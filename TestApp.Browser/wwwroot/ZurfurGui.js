


//
// TBD:
//
// This file should be removed from here and moved to ZurfurGui.Browser/wwwroot.
// This works and publishes ok, but then I can't run from the debugger in Visual Studio
// because it can't find ZurfurGui.js
//

import { dotnet } from './_framework/dotnet.js'


export async function ZurfurGuiRun(canvasId) {
    console.log("Starting app...");

    // Run in browser
    const is_browser = typeof window != "undefined";
    if (!is_browser) {
        throw new Error(`Expected to be running in a browser`);
    }

    const dotnetRuntime = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .create();
    const config = dotnetRuntime.getConfig();
    //const exports = await dotnetRuntime.getAssemblyExports(config.mainAssemblyName);

    await dotnetRuntime.runMain(config.mainAssemblyName, [canvasId]);

    console.log(`App is running the main assembly '${config.mainAssemblyName}'`);
}

export function fillText(context, text, x, y) { context.fillText(text, x, y); }
export function getBrowserWindow() { return globalThis.window; }
export function getBoundingClientRect(canvas) { return canvas.getBoundingClientRect(); }
export function fillRect(context, x, y, width, height) { context.fillRect(x, y, width, height); }
export function strokeRect(context, x, y, width, height) { context.strokeRect(x, y, width, height); }
export function getContext(canvas, contextId) {
    var c = canvas.getContext(contextId);
    c.save();
    return c;
}
export function measureText(context, text) { return context.measureText(text); }
export function clipRect(context, x, y, width, height) {
    context.restore();
    context.save();
    context.beginPath();
    context.rect(x, y, width, height);
    context.clip();
}


// Stores canvas size (pixels) in canvas.devicePixelWidth and canvas.devicePixelHeight whenever size changes
// https://web.dev/articles/device-pixel-content-box
export function observeCanvasDevicePixelSize(canvas) {
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







