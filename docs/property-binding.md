# Property Binding

ZurfurGUI uses a property system that connects concrete view data, controls, and styles. A ZUI `$data` declaration tells
the source generator which values belong in the generated data class and how changes should reach the view or style
system.

## The `$data` section

Controls declare their data properties in the `$data` section of a `.zui.json5` file. Legacy `.zui.json` files remain
supported. The source generator emits a concrete `<ViewName>Data` class that implements `INotifyPropertyChanged`, a
strongly typed `DataContext` property on the controller, and `PropertyKey` fields where the binding category requires
them.

There are no generated `I<ViewName>Data` contracts. If application code needs to extend a generated data class, add a
matching `<ViewName>.Data.cs` partial class in the same namespace. A handwritten partial can also implement an application
interface such as `IComboBoxItem`.

Each `$data` entry specifies `type`, `bind`, and optional `default` and `flags` values:

| Field | Required | Description | Example |
|---|---|---|---|
| `type` | Yes | The property type. Prefix with `?` for nullable types and `[]` for collections. | `Color`, `?string`, `[]Item` |
| `bind` | Yes | Controls whether the value is data-only, style-aware, style-only, or forwarded to a child. | `data`, `styledData`, `styledOnly`, `_child.text` |
| `default` | No | A C# expression used by the generated data constructor. | `100`, `Orientation.horizontal`, `new Color(128, 128, 128)` |
| `flags` | No | Comma-separated `ViewFlags` used when the property changes. | `draw`, `measure`, `styleThis`, `styleDown` |

For example:

```json5
$data: {
    value: {
        type: "double",
        bind: "data"
    },
    orientation: {
        type: "Orientation",
        bind: "styledData",
        default: "Orientation.horizontal"
    },
    thumbColor: {
        type: "Color",
        bind: "styledOnly",
        default: "new Color(128, 128, 128)",
        flags: "draw"
    }
}
```

The generator normalizes simple aliases such as `Int`, `Bool`, `Double`, and `String` to their C# equivalents. Property
names are camelCase in ZUI and become PascalCase C# properties.

## Binding categories

### 1. Data

- **Syntax:** `bind: "data"`
- **Generated output:** A property in the concrete data class and no `PropertyKey`.
- **Runtime behavior:** The generated controller stores the value in `DataContext` but does not push it to the view.
  Code-behind observes `INotifyPropertyChanged` directly.
- **Use for:** Application state such as `SelectedIndex`, `IsChecked`, or a collection of items.

### 2. Styled data

- **Syntax:** `bind: "styledData"`
- **Generated output:** A property in the concrete data class and a `PropertyKey<T>` on the controller. For a generic
  controller, the key is emitted on its non-generic companion class.
- **Runtime behavior:** The generated controller calls `View.SetProperty` when the data value changes. A nullable value
  calls `View.RemoveProperty` when it is `null`, allowing the active style or theme to supply a fallback.
- **Use for:** Visual values that application data may override, such as text color or orientation.

### 3. Style-only

- **Syntax:** `bind: "styledOnly"`
- **Generated output:** A `PropertyKey<T>` only. No property is emitted in the concrete data class or `DataContext`.
- **Runtime behavior:** The value is controlled by styles or imperative `View.SetProperty` calls, not by data changes.
- **Use for:** Visual configuration that should not be application data, such as scrollbar thickness or a control's
  default visual color.

### 4. Forwarded

- **Syntax:** Any other non-empty binding containing a dot, normally `bind: "_childName.propertyName"`.
- **Generated output:** A property in the concrete parent data class. No separate `PropertyKey` is emitted for the
  forwarded parent property.
- **Runtime behavior:** The generated controller assigns the parent value to the named child property on change.
  The first path component must be a named child control.
- **Use for:** Composite controls that expose a child property through their own data context, such as a CheckBox
  forwarding `text` to its internal TextView.

### 5. Attached

- **Syntax:** `bind: "attached"` is parsed as a distinct category.
- **Generated output:** Attached properties are normally defined directly in C# with `PropertyKey` rather than exposed
  through a control's data context.
- **Use for:** Parent-managed metadata such as layout values. The global Panel properties (`$align`, `$margin`,
  `$backgroundColor`, and `$borderWidth`) are examples of attached-style properties in ZUI.

## Collections

A collection uses `[]Type` and must use `bind: "data"`:

```json5
$data: {
    items: {
        type: "[]Item",
        bind: "data"
    }
}
```

Rules:

- Collections cannot be nullable; `?[]Type` is invalid.
- Collection bindings cannot use `styledData`, `styledOnly`, `attached`, or a forwarded path.
- A regular `[]Type` becomes `ObservableCollection<TypeData>` in generated data code.
- A generic `[]Item` becomes `ObservableCollection<Item>` so a closed `ComboBox<Item>` retains its concrete item type.
- The generated data constructor creates a live, non-null `ObservableCollection<T>`.
- The generated controller does not synchronize collection contents with the view.
  Handwritten code-behind must react to collection changes and refresh the relevant visual controls.

## Runtime behavior and initialization

When a generated controller assigns its `DataContext`, it subscribes to `PropertyChanged` and synchronizes applicable
styled properties to the view. During `Loader.Load`:

1. The embedded ZUI JSON is deserialized and the control tree is built.
2. Child controls are initialized before their parent data properties are applied.
3. Each generated controller creates its default concrete data object.
4. `Loader.ApplyDataProperties` recursively processes children, then applies JSON data properties to the parent.
5. JSON property names are converted from camelCase to PascalCase and assigned through generated `SetDataProperty` code.

Unknown data properties, invalid types, and null values for non-nullable properties produce errors.
Direct JSON data values should match the generated concrete property types.

## Generated data classes and code-behind

Generated files are placed under the build-generated source directory and must not be edited manually.
Change the ZUI schema or generator instead. Add behavior or marker interfaces in a matching partial file:

```csharp
namespace MyApp.Controls;

public sealed partial class ComboBoxItemBadgeData : global::ZurfurGui.Controls.IComboBoxItem
{
}
```

The corresponding ZUI file should declare the control's own `$data` properties. It should not use `$implements` or rely
on inherited generated data contracts.

