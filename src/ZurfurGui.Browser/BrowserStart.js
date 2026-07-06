

console.log("Running startup...");


globalThis.ZurfurGui = {};

globalThis.ZurfurGui.getBrowserWindow = function () { return globalThis.window; }

globalThis.ZurfurGui.getBoundingClientRect = function (canvas) { return canvas.getBoundingClientRect(); }

globalThis.ZurfurGui.getContext = function (canvas, contextId) {

    // NOTE: {alpha: false } makes canvas text rendering clearer
    return canvas.getContext(contextId, { alpha: false});
}

globalThis.ZurfurGui.canvasHasFocus = function (canvas) {
    return globalThis.document.hasFocus() && globalThis.document.activeElement == canvas;
}

globalThis.ZurfurGui.observeCanvasInput = function (canvas, callBack) {
    for (const et of ["pointerenter", "pointermove", "pointerleave", "pointerdown", "pointerup",
        "pointercancel", "wheel", "keydown", "keyup", "keypress"]) {
        canvas.addEventListener(et, function (event) {

            // TBD: Probably need to do this on most events, but we want to allow
            //      CTRL-mouse wheel through so browser scales the window
            // event.preventDefault();

            callBack(event);
        });
    }
}

// Stores canvas size (pixels) in canvas.devicePixelWidth and canvas.devicePixelHeight whenever size changes
// https://web.dev/articles/device-pixel-content-box
globalThis.ZurfurGui.observeCanvasDevicePixelSize = function (canvas) {
    try {
        canvas.devicePixelWidth = -1;
        canvas.devicePixelHeight = -1;
        const observer = new ResizeObserver((entries) => {
            try {
                const entry = entries.find((entry) => entry.target === canvas);
                if (entry && entry.devicePixelContentBoxSize) {
                    canvas.devicePixelWidth = entry.devicePixelContentBoxSize[0].inlineSize;
                    canvas.devicePixelHeight = entry.devicePixelContentBoxSize[0].blockSize;
                }
            }
            catch (e) {
                // Not supported
            }
        });
        observer.observe(canvas, { box: ['device-pixel-content-box'] });
    }
    catch (e) {
        // Not supported
    }
}

globalThis.ZurfurGui.measureText = function (context, font, text) {
    context.font = font;
    return context.measureText(text);
}


globalThis.ZurfurGui.marshaledStrings = [];

globalThis.ZurfurGui.marshalString = function (str, index) {
    if (str === null) {
        globalThis.ZurfurGui.marshaledStrings[index | 0] = null;
    } else {
        globalThis.ZurfurGui.marshaledStrings[index | 0] = str;
    }
}

// drawBuffer with persistent state in closure
globalThis.ZurfurGui.drawBuffer = (function() {
    // Persistent font state (private to this closure, like C# BrowserContext instance fields)
    let _fontName = 'sans-serif';
    let _fontSize = 16;
    let _fontString = null;

    return function(context, buffer, length) {
        const marshaledStrings = globalThis.ZurfurGui.marshaledStrings;

        // Locals pinned to prevent garbage collection allocation churn
        let command = 0, paramCount = 0, commandHeader = 0.0, text = "", i = 0;
        let x = 0.0, y = 0.0, width = 0.0, height = 0.0, radius = 0.0;

        let pc = 0; // Program counter
        let pi = 0; // Parameter index
        while (pc < length) {

            // Get command
            commandHeader = buffer[pc];
            command = (commandHeader / 0x100000000) | 0; // Shift right 32 bits
            paramCount = commandHeader & 0xFFFFFFFF;

            // Get pi and advance pc
            pi = (pc + 1) | 0;
            pc += (paramCount + 1) | 0;
            switch (command) {
                case 1: // FillColor
                    context.fillStyle = marshaledStrings[buffer[pi] | 0];
                    break;
                case 2: // StrokeColor
                    context.strokeStyle = marshaledStrings[buffer[pi] | 0];
                    break;
                case 3: // LineWidth
                    context.lineWidth = buffer[pi];
                    break;
                case 4: // FontName
                    _fontName = marshaledStrings[buffer[pi] | 0];
                    _fontSize = buffer[pi + 1];
                    _fontString = null;
                    break;
                case 6: // FillRect
                    x = buffer[pi];
                    y = buffer[pi + 1];
                    width = buffer[pi + 2];
                    height = buffer[pi + 3];
                    radius = buffer[pi + 4];
                    if (radius > 0) {
                        context.beginPath();
                        context.roundRect(x, y, width, height, radius);
                        context.fill();
                    } else {
                        context.fillRect(x, y, width, height);
                    }
                    break;
                case 7: // StrokeRect
                    x = buffer[pi];
                    y = buffer[pi + 1];
                    width = buffer[pi + 2];
                    height = buffer[pi + 3];
                    radius = buffer[pi + 4];
                    if (radius > 0) {
                        context.beginPath();
                        context.roundRect(x, y, width, height, radius);
                        context.stroke();
                    } else {
                        context.strokeRect(x, y, width, height);
                    }
                    break;
                case 8: // FillText
                    if (_fontString === null) {
                        _fontString = _fontSize + "px " + _fontName;
                        context.font = _fontString;
                    }
                    text = marshaledStrings[buffer[pi] | 0];
                    x = buffer[pi + 1];
                    y = buffer[pi + 2];
                    context.fillText(text, x, y);
                    break;
                case 9: // StrokePolyLine
                    if (paramCount < 4) break;
                    context.beginPath();
                    context.moveTo(buffer[pi], buffer[pi + 1]);
                    for (i = 2; i < paramCount; i = (i + 2) | 0) {
                        context.lineTo(buffer[pi + i], buffer[pi + i + 1]);
                    }
                    context.stroke();
                    break;
                case 10: // FillPolygon
                    if (paramCount < 6) break;
                    context.beginPath();
                    context.moveTo(buffer[pi], buffer[pi + 1]);
                    for (i = 2; i < paramCount; i = (i + 2) | 0) {
                        context.lineTo(buffer[pi + i], buffer[pi + i + 1]);
                    }
                    context.closePath();
                    context.fill();
                    break;
                case 11: // PushClip
                    x = buffer[pi];
                    y = buffer[pi + 1];
                    width = buffer[pi + 2];
                    height = buffer[pi + 3];
                    context.save();
                    context.beginPath();
                    context.rect(x, y, width, height);
                    context.clip();
                    break;
                case 12: // PopClip
                    context.restore();
                    break;
            }
        }
    };
})();

