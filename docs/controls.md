# Controls

This guide gives a short introduction to the controls provided by ZurfurGui. Controls are declared in ZUI JSON5
files and loaded into generated C# controller classes. A view with named controls gets fields for those controls in
its generated controller, and application behavior can be added in the matching `.Control.cs` partial class.

For the generated data model, property binding categories, and initialization details, see
[Architecture](architecture.md) and [Property Binding](property-binding.md).

## Application setup

Each application provides a partial `ZurfurMain` class with a `MainApp` method. Call `InitializeControls` before
creating controls. Use `SetMainappWindow` to set the main application content, and `ShowWindow` to display an
additional window.

```csharp
public static void MainApp(AppWindow app)
{
    InitializeControls();

    app.SetMainappWindow(new MainView());
    app.ShowWindow(new SettingsView(), "Settings");
}
```

The main view and the window content are ordinary generated controls. For example, a simple main view can be
written as follows:

```json5
{
    $controller: "MainView",
    $namespace: "MyApp",
    $backgroundColor: "#202020",
    $content: [
        {
            $controller: "TextView",
            text: "Welcome to ZurfurGui"
        }
    ]
}
```

`SetMainappWindow` places the control in the application's main window. `ShowWindow` wraps a control in a window
with optional title, position, size, and alignment settings. See [Initialization](initialization.md) for more about
application startup.

## ZUI controls

### Panel

`Panel` is a general-purpose container. Use its `$content` section to contain other controls and use `$layout` to
arrange those controls. Common layouts include `Row`, `Column`, and `Dock`.

```json5
{
    $controller: "Panel",
    $layout: "Row",
    $content: [
        { $controller: "TextView", text: "Name:" },
        { $controller: "Button", text: "Edit" }
    ]
}
```

### TextView

`TextView` displays text. Its `text` property can be a string or an array of lines. Use styles and layout properties
to control its appearance and position.

```json5
{
    $controller: "TextView",
    text: "Hello"
}
```

### Button

`Button` displays a clickable button. Set its `text` property in ZUI. Give it a `$name` when application code needs
to access it from the generated controller.

```json5
{
    $name: "buttonSave",
    $controller: "Button",
    text: "Save"
}
```

Subscribe to the button's `Click` event in the view's `.Control.cs` partial class. Use the public `Click` event
instead of low-level pointer events:

```csharp
public partial class MainView
{
    public MainView()
    {
        InitializeControl();
        buttonSave.Click += ButtonSave_Click;
    }

    private void ButtonSave_Click(object? sender, EventArgs e)
    {
        // Execute the save operation.
    }
}
```

### ScrollBar

`ScrollBar` displays and changes a numeric scroll value between its `minimum` and `maximum` values. Its `value`
property is data, so application code can read or change it through the generated `DataContext`.

Scrollbars are commonly managed by `ScrollViewer` rather than created directly. Direct scrollbar usage is useful when
an application needs to provide its own scrolling behavior.

Standalone scrollbars use their expanded appearance by default. Set `expandOnHover` to `true` to make the scrollbar
use its compact appearance until the pointer enters it:

```csharp
scrollBar.View.SetProperty(ScrollBar.ExpandOnHoverProperty, true);
```

When `expandOnHover` is `true`, the scrollbar expands while hovered, pressed, or dragged. When it is `false`, it
remains expanded regardless of the pointer position. Pressed and dragging behavior is independent of the property.

The `thumbThickness` property controls the expanded thumb thickness. `thumbRestingThickness` controls the compact
thumb thickness, and `thumbEdgeInset` controls the compact thumb's distance from the outer edge.

Themes can use the runtime `ScrollBar.isExpanded` condition to select colors for the effective visual state. This
condition reflects hover, press, drag, and `expandOnHover` behavior rather than only the physical pointer position.

### ScrollViewer

`ScrollViewer` displays content through a clipped viewport and provides horizontal and vertical scrolling. Put the
content to scroll in its `$content` section. The combined visible offset is available through `ScrollOffset`.

```csharp
var offset = scrollViewer.ScrollOffset;
scrollViewer.ScrollOffset = new Point(offset.X, offset.Y + 8);
```

For layout, scrollbar visibility, range behavior, and detailed usage, see [ScrollViewer](scroll-viewer.md).

### ComboBox

`ComboBox<Item>` displays a single-selection dropdown. Populate its generated `DataContext.Items` collection with
item data objects and set `DataContext.SelectedIndex` to select an item.

```csharp
_themeComboBox.DataContext.Items.Add(
    new ComboBoxItemTextData { Text = new TextLines("Light") });
_themeComboBox.DataContext.SelectedIndex = 0;
```

Selection changes are reported through `DataContext.PropertyChanged` for the `SelectedIndex` property. For generic item
requirements, item renderers, and detailed usage, see [ComboBox](combo-box.md).

### TextEdit

`TextEdit` has not been implemented yet.

## Generated controller access

A `$name` in a ZUI file creates a named control field on the generated controller. User code can use that field from
the matching `.Control.cs` partial class. Data declared in `$data` is available through the control's generated
`DataContext` and reports changes through `INotifyPropertyChanged`.

For example, a named ComboBox can be populated from its containing view's code-behind, while a named Button can be
handled through its `Click` event. This keeps control layout in ZUI and application behavior in C#.
