# Initialization Procedure

This document describes startup and initialization for applications using ZurfurGui, including Windows and browser
targets.

## Overview

ZurfurGui is a library distributed as a DLL. Applications start the platform, initialize the library, and call
generated registration code before creating or manipulating controls.

The source generator emits a `ZurfurMain.InitializeControls()` method for each project containing ZUI or theme files.
It registers control types, concrete data-controller factories, closed generic control usages, property keys, and
themes.

## Startup Sequence

1. **Application entry point**
   - The application starts in its own `Program.Main` or equivalent entry point.

2. **Platform startup**
   - The application calls the platform-specific entry point:
     - **Windows:** `WinStart.Start(userCallback)`
     - **Browser:** `BrowserStart.Start(userCallback)`
   - The callback is a delegate, typically a method or lambda, that runs application initialization.

3. **Library initialization**
   - `WinStart.Start` or `BrowserStart.Start` calls `Loader.Init(...)`.
   - `Loader.Init` initializes built-in layouts, calls the application's `ZurfurMain.MainApp()`, creates an
     `AppWindow`, and finally invokes the platform callback with that window.

4. **Generated registration**
   - `ZurfurMain.MainApp()` must call the generated `InitializeControls()` method before creating controls.
   - The generated method:
     - runs static constructors for property registration, including discovered closed generic controls;
     - registers ordinary controls for creation from ZUI controller names;
     - registers concrete data types to controller factories;
     - registers closed generic controller usages discovered in ZUI files; and
     - registers themes.
   - If using third-party libraries or plugins, call their `ZurfurMain.InitializeControls()` methods at this stage too.

5. **Application logic**
   - After all required initialization methods return, application code can safely create windows and controls,
     populate concrete data objects, and set the main application window.

## Example startup code

Platform-specific `Program.cs` code should call the appropriate platform entry point with the application callback:

```csharp
[STAThread]
static void Main()
{
    WinStart.Start(ZurfurMain.MainApp);
}
```

The application's `ZurfurMain` class must be partial and call generated registration before application logic:

```csharp
public static partial class ZurfurMain
{
    public static void MainApp()
    {
        InitializeControls();

        // Register controls and themes supplied by third-party libraries.
        // ThirdPartyDll.ZurfurMain.InitializeControls();

        app.SetMainAppWindow(new MyMainForm());
    }
}
```

For browser targets, use `BrowserStart.Start(ZurfurMain.MainApp)` in the platform entry point instead.

## Generated data-controller registration

For each non-generic data-bearing control, generated initialization contains a registration equivalent to:

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

The concrete data type is the registry key. The lambda receives the actual data object as `object`, creates the matching
controller, assigns that same object to `DataContext`, and returns the controller. Controls such as `ComboBox<Item>` call
`Loader.CreateDataController(itemData)` when they need to render an item.

## Notes and best practices

- Call every required `InitializeControls()` method before creating or manipulating GUI elements.
- Call a library's generated initialization only once unless the library explicitly supports repeated registration.
- Keep the generated `.g.cs` files out of manual edits; change ZUI, code-behind, or generator code instead.
- A ZUI file must be included as an `AdditionalFiles` input for the generator to see it.
- A ComboBox item data class must implement `IComboBoxItem`, and its concrete data/controller registration must be
  generated before the item is added to a ComboBox.
- `Loader.Init` invokes the application's `ZurfurMain.MainApp()` before it invokes the platform callback. Do not assume
  the callback is where generated registration occurs unless the application's startup path is designed that way.
- The application is responsible for ordering third-party initialization and ensuring that all controls used by the
  application are registered before they are created.

For more details, see `WinStart`, `BrowserStart`, `Loader`, and the generated `ZurfurMain.InitializeControls()` method.
