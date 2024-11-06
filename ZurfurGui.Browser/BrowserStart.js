

console.log("Running startup...");


globalThis.ZurfurGui = {};


globalThis.ZurfurGui.fillText = function(context, text, x, y) { context.fillText(text, x, y); }
globalThis.ZurfurGui.getBrowserWindow = function() { return globalThis.window; }
globalThis.ZurfurGui.getBoundingClientRect = function(canvas) { return canvas.getBoundingClientRect(); }
globalThis.ZurfurGui.fillRect = function(context, x, y, width, height) { context.fillRect(x, y, width, height); }
globalThis.ZurfurGui.strokeRect = function(context, x, y, width, height) { context.strokeRect(x, y, width, height); }
globalThis.ZurfurGui.getContext = function(canvas, contextId) {
    var c = canvas.getContext(contextId);
    c.save();
    return c;
}
globalThis.ZurfurGui.measureText = function (context, text) { return context.measureText(text); }
globalThis.ZurfurGui.clipRect = function (context, x, y, width, height) {
    context.restore();
    context.save();
    context.beginPath();
    context.rect(x, y, width, height);
    context.clip();
}


// Stores canvas size (pixels) in canvas.devicePixelWidth and canvas.devicePixelHeight whenever size changes
// https://web.dev/articles/device-pixel-content-box
globalThis.ZurfurGui.observeCanvasDevicePixelSize = function(canvas) {
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





