

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

globalThis.ZurfurGui.fillText = function(context, text, x, y) { context.fillText(text, x, y); }
globalThis.ZurfurGui.fillRect = function (context, x, y, width, height, radius) {
    if (radius > 0) {
        context.beginPath();
        context.roundRect(x, y, width, height, radius);
        context.fill();
    } else {
        context.fillRect(x, y, width, height);
    }
}
globalThis.ZurfurGui.strokeRect = function (context, x, y, width, height, radius) {
    if (radius > 0) {
        context.beginPath();
        context.roundRect(x, y, width, height, radius);
        context.stroke();
    } else {
        context.strokeRect(x, y, width, height);
    }
}

globalThis.ZurfurGui.measureText = function (context, text) { return context.measureText(text); }

globalThis.ZurfurGui.clipRect = function (context, x, y, width, height) {
    context.save();
    context.beginPath();
    context.rect(x, y, width, height);
    context.clip();
}

globalThis.ZurfurGui.unClip = function (context) {
    context.restore();
}



