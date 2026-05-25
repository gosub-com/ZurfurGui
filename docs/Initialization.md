# Initialization Procedure

This document describes the startup and initialization sequence for applications using the ZurfurGui library, including
both Windows and browser targets.

## Overview
ZurfurGui is a library distributed as a DLL. Applications (such as TestApp) are responsible for starting the library
and initializing controls, styles, and themes before running any application logic.

## Startup Sequence

1. **Application Entry Point**
   - The user's application starts in its own `Program.Main` (or equivalent entry point).

2. **Platform Startup**
   - The application calls the platform-specific entry point:
     - **Windows:** `WinStart.Start(userCallback)`
     - **Browser:** `BrowserStart.Start(userCallback)`
   - The `userCallback` is a delegate (typically a lambda or method) that will run user initialization code.

3. **Library Initialization**
   - `WinStart.Start` or `BrowserStart.Start` calls `Loader.Init(userCallback)`.
   - `Loader.Init` performs core ZurfurGui library setup, then invokes the `userCallback`.

4. **User Initialization (MainApp)**
   - The user's callback (e.g., `Zurfur.MainApp`) is now running. At this point:
     - The ZurfurGui library is initialized, but controls, styles, and themes are not yet registered.
     - The callback must call the code generated `ZurfurMain.InitializeControls()` to register all controls, styles,
       and themes for the main app.
     - If using third-party libraries or plugins, their `ZurfurMain.InitializeControls()` methods should also be called
       at this stage.

5. **Application Logic**
   - After all `InitializeControls()` calls, the user can proceed to run application logic (e.g., creating windows,
     setting the main window, etc.).

## Example Startup Code

Platform specific code in `Program.cs` should call `WinStart.Start` or `BrowserStart.Start` with the main app callback:

```csharp
[STAThread]
static void Main()
{
    WinStart.Start(ZurfurMain.MainApp);
}

```

The `ZurfurMain` class must be a partial class that calls `InitializeControls()` before running application logic:

```csharp
public static partial class ZurfurMain
{
    public static void MainApp()
    {
        // Register controls, styles, and themes for this app
        InitializeControls();

        // Register controls for any third-party libraries
        // ThirdPartyDll.ZurfurMain.InitializeControls();

        // Now safe to run application logic
        app.SetMainAppWindow(new MyMainForm());
    }
}
```

## Notes
- The generated `ZurfurMain.InitializeControls()` method must be called before any application logic that creates or
  manipulates GUI elements.
- If using third-party libraries that provide their own controls or styles, their `InitializeControls()` methods must
  also be called before proceeding.
- The ZurfurGui library cannot automatically detect when all `InitializeControls()` methods have been called; it is the
  responsibility of the application to ensure the correct order.

## Best Practices
- Always call all required `InitializeControls()` methods before running any application logic.
- If you add new third-party libraries, update your startup code to call their initialization methods as well.
- Follow this sequence for both Windows and browser targets.

---

For more details, see the documentation for `WinStart`, `BrowserStart`, and `Loader` in the ZurfurGui library.
