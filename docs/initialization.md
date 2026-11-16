# Initialization Procedure

This document describes startup and initialization for applications using ZurfurGui, including
Windows and browser targets.

## Overview


The source generator emits a `ZurfurMain.InitializeControls()` method for each project containing
ZUI or theme files. It registers control types, concrete data-controller factories, closed generic
control usages, property keys, and themes. Each project must declare a `static partial class
ZurfurMain` (the generator reports `ZUI005` otherwise). See [Code generation](code-generation.md)
for details.

## Startup Sequence

1. **Application entry point**
   - The application starts in its own `Program.Main` or equivalent entry point.

2. **Platform startup**
   - The application calls the platform-specific entry point:
     - **Windows:** `WinStart.Start(userCallback)`
     - **Browser:** `BrowserStart.Start(canvasId, userCallback)`
   - The callback receives the created `AppWindow`. `Loader.Init` calls
     `ZurfurMain.MainApp()` before invoking this callback.

3. **Library initialization**
   - `WinStart.Start` or `BrowserStart.Start` calls `Loader.Init(...)`.
   - `Loader.Init` initializes built-in layouts, calls the application's `ZurfurMain.MainApp()`,
     creates an `AppWindow`, and finally invokes the platform callback with that window.

4. **Generated registration**
   - `ZurfurMain.MainApp()` must call the generated `InitializeControls()` method before creating
     controls.
   - The generated method:
     - runs static constructors for property registration, including open generic controls;
     - registers ordinary controls for creation from ZUI controller names;
     - registers open generic controls (e.g. `ComboBox<>`) under their bare class name, so closed
       usages can be resolved later by name;
     - registers concrete data types to controller factories;
     - registers closed generic controller usages discovered in ZUI files, resolving the base
       generic control and item controller by bare name at runtime (see
       [Loader registration and lookup](#loader-registration-and-lookup)); and
     - registers themes.
   - If using third-party libraries or plugins, call their `ZurfurMain.InitializeControls()` methods
     at this stage too, before any of your own closed generic usages of their controls are
     registered or loaded.

5. **Application logic**
   - After all required initialization methods return, the callback can create windows and controls,
     populate concrete data objects, and set the main application window.

## Example startup code

`Loader.Init` invokes `ZurfurMain.MainApp()` for library and generated registration. The platform
callback is separate and receives the `AppWindow` used to set up the main application control.
For Windows:

```csharp
[STAThread]
static void Main()
{
    WinStart.Start(appWindow =>
    {
        appWindow.SetMainappWindow(new MyMainForm());
    });
}
```

The application's `ZurfurMain` class must be partial and call generated registration before
application logic:

```csharp
public static partial class ZurfurMain
{
    public static void MainApp()
    {
        InitializeControls();

        // Register controls and themes supplied by third-party libraries.
        // ThirdPartyDll.ZurfurMain.InitializeControls();
    }
}
```

For browser targets, pass the canvas id and an `AppWindow` callback:

```csharp
BrowserStart.Start("main", appWindow =>
{
    appWindow.SetMainappWindow(new MyMainForm());
});
```

## Loader and control initialization

### Loader registration and lookup

`Loader` keeps separate registries for controls and concrete data-item controllers:

- `RegisterControl` maps a controller name to a `Controllable` type and factory. ZUI controller
  lookup tries the fully qualified name, the containing control's namespace, its `$use` namespaces,
  and then `ZurfurGui.Controls`. A missing `$controller` resolves to `Panel`. Re-registering the
  same controller name with a different type throws.
  - Closed generic usages found in ZUI files are registered under their usage string. The generator
    resolves and emits those types directly; see [Code generation](code-generation.md).
- `RegisterDataController` maps a concrete data type to a factory. Lookup uses the exact runtime
  type; duplicate data-type registration throws. The factory receives and assigns the same data
  instance to the controller's `DataContext`.

Generated controllers load through the control registry. Data-item controller lookup occurs when a
data-driven control such as `ComboBox` creates an item controller. For generated registrations, see
[Code generation](code-generation.md).

## Control content loading

Controls load two kinds of content: template content is the control's own ZUI-defined visual
structure, while parent content is supplied by the containing control through the child control's
`$content`.

The loader and generated initialization call in this order:

```text
Control constructor (generated or hand-written)
        |
        v
Generated InitializeControl()
        |
        +--> View = new(this)
        |
        +--> ZuiRuntime.Load(control, control's own ZUI JSON)
                |
                +--> merge template properties and apply template layout to View
                |
                +--> add template content: create children and add their Views to View
                |
                +--> ApplyDataProperties recursively, children before parent
        |
        +--> cache named child controls
        |
        +--> create the control's default DataContext, if it has data bindings

Parent creates a child from its $content
        |
        v
Loader.CreateControl(child properties, context)
        |
        +--> resolve and invoke the child controller factory
        |       child constructor completes its own InitializeControl sequence
        |
        +--> merge parent-supplied instance properties onto the child's View
        |
        +--> apply parent-supplied layout to the child's ContentHost
        |
        +--> add parent content: create children and add their Views to ContentHost
```

`ContentHost` determines where parent-supplied content is placed, not the call order or content
ownership. It is `View` by default. Template content is control-owned structure, while parent content
is supplied by the parent.

For `DataContext` defaults and binding behavior, see [Property Binding](property-binding.md).

`ScrollViewer` uses the default template-loading behavior: its own ZUI content defines the clipped
viewport, content window, and overlay scrollbars. Its `ContentHost` is `_contentWindow`, so parent
content is routed separately from the scrollbars. `Window` uses the same pattern: its template
defines the title bar, clipped `_windowContent`, and resize handle, and its `ContentHost` routes
parent content to `_windowContent`. Its `title` data property forwards to the title `TextView`'s
`text` property.

This explicit distinction avoids inferring the loading phase from the current child count and
supports controls with templates or named content hosts without loader-specific phase detection.
For measurement, arrangement, sizing, and content-extent behavior, see
[ScrollViewer](scroll-viewer.md).
