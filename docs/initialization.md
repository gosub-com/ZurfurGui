# Initialization Procedure

This document describes startup and initialization for applications using ZurfurGui, including
Windows and browser targets.

## Overview

ZurfurGui is a library distributed as a DLL. Applications start the platform, initialize the
library, and call generated registration code before creating or manipulating controls.

The source generator emits a `ZurfurMain.InitializeControls()` method for each project containing
ZUI or theme files. It registers control types, concrete data-controller factories, closed generic
control usages, property keys, and themes. Each project must declare a `static partial class
ZurfurMain` (the generator reports `ZUI005` otherwise). See [Code generation](code-generation.md)
for what is generated and how files are discovered.

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
     - runs static constructors for property registration, including discovered closed generic
       controls;
     - registers ordinary controls for creation from ZUI controller names;
     - registers concrete data types to controller factories;
     - registers closed generic controller usages discovered in ZUI files; and
     - registers themes.
   - If using third-party libraries or plugins, call their `ZurfurMain.InitializeControls()` methods
     at this stage too.

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

## Generated data-controller registration

For each non-generic data-bearing control, generated initialization contains a registration
equivalent to:

```csharp
Loader.RegisterDataController(
    typeof(ComboBoxItemTextData),
    itemData =>
    {
        var controller = new ComboBoxItemText();
        controller.DataContext = (ComboBoxItemTextData)itemData;
        return controller;
    });
```

The concrete data type is the registry key. The lambda receives the actual data object as `object`,
creates the matching controller, assigns that same object to `DataContext`, and returns the
controller.
Controls such as `ComboBox<Item>` call `Loader.CreateDataController(itemData)` when they need to
render an item.

## Loader and control initialization

### Per-control initialization

Generated `InitializeControl()` creates the view, loads the control's own ZUI definition, then
caches named child controls and creates the default `DataContext` when the control has data
bindings:

1. `View = new(this)` attaches the controller to its view.
2. `Loader.Load(this, _zuiJsonContent)` builds the control's template and applies its JSON
   properties.
3. Generated fields for named controls are assigned using `View.FindByName(...)`.
4. `DataContext = CreateDefaultDataContext()` creates the data object.

The default data context initializes ordinary values from declared defaults or type defaults. A
binding to a named child control uses that child's already-created `DataContext`; forwarded scalar
bindings initialize from the target child property to preserve template values. Other non-null data
values are initialized with a new instance of their declared type when no explicit or implicit
default applies. Template child controllers finish initialization before the parent data context is
created, so explicitly declared references can use their child contexts.

`Loader.Load` calls `ApplyDataProperties` before generated `InitializeControl()` assigns the
current control's default `DataContext`. See [Future improvements](future-improvements.md) for the
known ordering gap.

### Loader registration and lookup

`Loader` keeps separate registries for controls and concrete data-item controllers:

- `RegisterControl` maps a controller name to a `Controllable` type and factory. ZUI controller
  lookup tries the fully qualified name, the containing control's namespace, its `$use` namespaces,
  and then `ZurfurGui.Controls`. A missing `$controller` resolves to `Panel`. Closed generic
  usages are registered under the `ZurfurGui.Controls.` namespace. Re-registering
  type throws.
- `RegisterDataController` maps a concrete data type to a factory. Lookup uses the exact runtime
  type; duplicate data-type registration throws. The factory receives and assigns the same data
  instance to the controller's `DataContext`.

`Loader.Init` calls `ZurfurMain.MainApp()`, which must perform generated and required third-party
registration before controls are created. A generated controller then calls `Loader.Load`; child
controllers are resolved through the control registry. Data-item controller lookup occurs later,
when a data-driven control such as `ComboBox` creates an item controller.

For how the generator discovers `.zui` and `.zth` files and emits the registration code, see
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
        +--> Loader.Load(control, control's own ZUI JSON)
                |
                +--> merge template properties and apply template layout to View
                |
                +--> LoadTemplateContent(template content)
                |       default: create children and add their Views to View
                |       custom:  add or organize control-owned children
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
        +--> LoadParentContent(parent content)
                default: create children and add their Views to ContentHost
                custom:  handle parent content specially
```

`Loader.Load` invokes `Controllable.LoadTemplateContent` for the control's own ZUI content. Its
default implementation creates each child and adds that child's view to the control's `View`.
`Loader.CreateControl` first constructs and initializes the child; it then applies the parent's
instance properties and layout and invokes `Controllable.LoadParentContent`.

`ContentHost` determines where parent-supplied content is placed, not the call order or content
ownership. It is `View` by default. `LoadTemplateContent` handles the control-owned structure, while
`LoadParentContent` handles content supplied by the parent.

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

## Notes and best practices

- Call every required `InitializeControls()` method before creating or manipulating GUI elements.
- Call a library's generated initialization only once unless the library explicitly supports
  repeated registration.
- Keep generated `.g.cs` files out of manual edits; change ZUI, code-behind, or generator code
  instead.
- A ZUI file must be included as an `AdditionalFiles` input for the generator to see it (see
  [Code generation](code-generation.md#file-discovery)).
- A ComboBox item data class must implement `IComboBoxItem`, and its concrete data/controller
  registration must be generated before the item is added to a ComboBox.
- `Loader.Init` invokes the application's `ZurfurMain.MainApp()` before it invokes the platform
  callback. Do not assume the callback is where generated registration occurs unless the
  application's startup path is designed that way.
- The application is responsible for ordering third-party initialization and ensuring that all
  controls used by the application are registered before they are created.

For more details, see `WinStart`, `BrowserStart`, `Loader`, and the generated
`ZurfurMain.InitializeControls()` method.
