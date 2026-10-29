# Property Binding

ZurfurGUI uses a property system that connects concrete view data, controls, and styles. A ZUI `$data`
declaration tells the source generator which values belong in the generated data class and how changes
should reach the view or style system.

## The `$data` section

Controls declare their data properties in the `$data` section of a `.zui` file. The generator
`INotifyPropertyChanged`, a strongly typed `DataContext` property when data bindings exist, and
`PropertyKey`s for view-facing bindings. Controllers expose view-facing properties through a generated
`Properties` accessor, for example `control.Properties.ThumbColor`.

There are no generated `I<ViewName>Data` contracts. If application code needs to extend a generated
data class, add a matching `<ViewName>.Data.cs` partial class in the same namespace. A handwritten
partial can also implement an application interface such as `IComboBoxItem`.

Each `$data` entry specifies `type`, `bind`, and optional `default` and `flags` values:

| Field | Required | Description | Example |
|---|---|---|---|
| `type` | Yes | The property type. Prefix with `?` for nullable types and `[]` for collections. | `Color`, `?string`, `[]Item` |
| `bind` | Yes | Controls whether the value is data-only, style-aware, style-only, view-only, attached, or forwarded. | `data`, `styledData`, `styled`, `property`, `attachedProperty`, `attachedStyle` |
so write `"100"`. | `"100"`, `"Orientation.horizontal"`, `"new Color(128, 128, 128)"` |
| `flags` | No | Comma-separated `ViewFlags` used when the property changes. | `draw`, `measure`, `styleThis`, `styleDown` |

For example:

```json5
$data: {
    // Current application value; not mirrored to a view property.
    value: {
        type: "double",
        bind: "data"
    },
    // Initial orientation, with a style-aware view value.
    orientation: {
        type: "Orientation",
        bind: "styledData",
        default: "Orientation.horizontal"
    },
    // Themeable visual configuration that is not application data.
    thumbColor: {
        type: "Color",
        bind: "styled",
        default: "new Color(128, 128, 128)",
        flags: "draw"
    }
}
```

The generator normalizes simple aliases such as `Int`, `Bool`, `Double`, and `String` to their C#
equivalents. Property names are camelCase in ZUI and become PascalCase C# properties.

In a generic control, a type parameter can be used by `data` and forwarded bindings. It cannot be used by
a binding that emits a `PropertyKey` (`styledData`, `styled`, `property`, `attachedProperty`, or
`attachedStyle`), because generic controls emit those keys on a non-generic companion class. The
generated key cannot refer to the control's type parameter. This restriction applies to scalar and
nullable type-parameter values; `[]T` remains data-only under the collection rules below. A generic
control may still declare view-facing bindings whose types do not use `T`; the open generic owner key
for those bindings is valid on matching closed controller types.

Line comments immediately above a binding in `$data` are emitted as XML documentation on the generated
members for that binding. Comments are omitted where a binding does not generate a member; for example,
a `data` binding has no `Properties` member. A `styledData` comment can appear on both its generated
data property and its read-only `Properties` member.

## Binding categories

`View.GetStyle` is private. Use `Control.Properties` to read a property or style when the property is
not attached and the control is known at compile time. Use `View.GetProperty` when the property is
attached or the control is not known at compile time. `View.GetProperty` resolves styles for style
kinds (`styledData`, `styled`, `attachedStyle`) and reads the view property directly for `property`
and `attachedProperty`. The generated `Properties` getters call `View.GetProperty`, so the
"`GetStyle`" entries below mean the style-resolving read.

### 1. Data

- **Syntax:** `bind: "data"`
- **Generated output:** A property in the concrete data class; no `PropertyKey` or `Properties` member.
- **Runtime behavior:** The generated controller stores the value in `DataContext` but does not push it to the view.
  Read and write it through `DataContext`; code-behind observes `INotifyPropertyChanged` directly.
- **Use for:** Application state such as `SelectedIndex`, `IsChecked`, or a collection of items.

### 2. Styled data

- **Syntax:** `bind: "styledData"`
- **Generated output:** A property in the concrete data class and a `PropertyKey<T>` on the controller.
  For a generic controller, the key is emitted on its non-generic companion class. `Properties` exposes
  a read-only member for it.
- **Runtime behavior:** Always read the effective value through `Properties`, which uses `GetStyle`. The
  member has no setter; write through `DataContext`. The generated controller mirrors data-context
  changes to the view with `View.SetProperty`; a nullable value calls `View.RemoveProperty` when it is
  `null`, allowing the active style or theme to supply a fallback.
- **Use for:** Visual values that application data may override, such as text color or orientation.

### 3. Style-only

- **Syntax:** `bind: "styled"`
- **Generated output:** A `PropertyKey<T>` and no property in the concrete data class or `DataContext`.
- **Runtime behavior:** JSON values are applied directly to the view using the generated `PropertyKey`;
  the value is also controllable by styles or imperative `View.SetProperty` calls, not by data changes.
  It is not stored in `DataContext`. The generated `Properties` accessor reads the resolved value with
  `GetStyle` and writes through `View.SetProperty`.
- **Use for:** Visual configuration that should not be application data, such as scrollbar thickness or a control's
  default visual color.

### 4. View property

- **Syntax:** `bind: "property"`
- **Generated output:** A `PropertyKey<T>` and no property in the concrete data class or `DataContext`.
- **Runtime behavior:** JSON values are set on the view using the generated `PropertyKey`, like
  `styled`. Controls read the value with `View.GetProperty`, which does not resolve theme values. The
  generated `Properties` accessor uses `GetProperty` for reads and `View.SetProperty` for writes.
- **Use for:** A view property that is not style-aware and should be read directly from the view property collection.

### 5. Forwarded

- **Syntax:** `bind: "data:<target>"`, normally `bind: "data:_childName.propertyName"`.
- **Generated output:** A property in the concrete parent data class, but no `PropertyKey` or
  `Properties` member. Its runtime `ZuiPropertyType` is `Data`.
- **Runtime behavior:** Read and write the parent value through its `DataContext`. When the parent value
  changes, generated code assigns it to the named child's `DataContext`. The target can be a child
  `data` or `styledData` property, but not a child `styled` property, which has no `DataContext`
  property. A `styledData` child target mirrors the forwarded value to its own view property.
- **Use for:** Composite controls that expose a child property through their own data context, such as a CheckBox
  forwarding `text` to its internal TextView.

Forwarding is data-to-data, even when the child's target uses `styledData`: generated code copies the
explicit value between the parent and child `DataContext`s rather than reading the child's effective
styled value. This preserves `null` as “no data override,” allowing the child's style or theme fallback
to remain active. Use the child's `Properties` accessor when code needs that effective visual value, not
when forwarding the underlying data.

Without a colon, only `data`, `styledData`, `styled`, `property`, `attachedProperty`, and `attachedStyle`
are valid. With a colon, the prefix must be `data` and the target must be non-empty. The target path
must begin with the name of a child control and include a property (`_child.property`); a target
without a `.` is accepted but generates no forwarding code (see
[Future improvements](future-improvements.md)).
compiler when it compiles the generated forwarding assignment.

### 6. Attached property

- **Syntax:** `bind: "attachedProperty"`
- **Generated output:** A `PropertyKey<T>` and a `Properties` member, but no property in the concrete
  data class or `DataContext`. The generated metadata uses `ZuiPropertyType.AttachedProperty`.
- **Generated access:** The `Properties` member reads with `GetProperty` and writes with `View.SetProperty`, like
  `property`.
- **Runtime behavior:** The key may be used on a view even when its controller is not the key's owner.
  Reads and writes use that view's ordinary property collection; the value is stored on the view passed
  to the operation, with no routing to another control.
- **Use for:** A property that can be set on controls that do not own its key.

### 7. Attached style

- **Syntax:** `bind: "attachedStyle"`
- **Generated output:** A `PropertyKey<T>` and a `Properties` member, but no property in the concrete
  data class or `DataContext`. The generated metadata uses `ZuiPropertyType.AttachedStyle`.
- **Generated access:** The `Properties` member reads with `GetStyle` and writes with `View.SetProperty`, like
  `styled`.
- **Runtime behavior:** The key may be used on a view even when its controller is not the key's owner.
  Writes are stored on that view; reads use the ordinary `GetStyle` lookup against that view. There is
  no additional routing to another control.
- **Use for:** A style-aware property that can be used on controls that do not own its key.

## DataContext and Properties

`DataContext` and `Properties` serve different purposes. Use `DataContext` for application data and
forwarded values, including writes to `styledData`. Use `Properties` for view-facing properties and
**always use it to read `styledData`**: it returns the effective value after style and theme resolution.
`Properties` is generated around the control's `View`; data and forwarded bindings are not exposed there.

| Binding | `Properties` getter | `Properties` setter | Notes |
|---|---|---|---|
| `data` | — | — | Use `DataContext`; the value is not mirrored to a view property. |
| `data:<target>` (Forwarded) | — | — | Use `DataContext`; parent changes are forwarded to the child's `DataContext`. |
| `styledData` | `GetStyle` | — | Read through `Properties`; set through `DataContext`. A null write removes the local override so styles can apply. |
| `styled` | `GetStyle` | `SetProperty` | The getter resolves styles; the setter writes a local property override. |
| `property` | `GetProperty` | `SetProperty` | Reads the view property without resolving theme values. |
| `attachedStyle` | `GetStyle` | `SetProperty` | Uses the target view's style lookup; the key is allowed on non-owner views. |
| `attachedProperty` | `GetProperty` | `SetProperty` | Stores a value on the target view; the key is allowed on non-owner views. |

For nullable `styledData`, assigning `null` through `DataContext` updates the data value and removes the
local view property. A subsequent `Properties` read resolves the available style or theme value. Do not
read `styledData` from `DataContext` when you need the effective visual value. Nullable `styled` and
`property` accessors remove their local view property when assigned `null`. Data-only null assignments
remain data assignments and never call `RemoveProperty`.

## Property ownership

For non-attached keys, `View` checks that the key owner is the controller type, an assignable base or
interface, or an open generic definition matching the controller's generic type. Attached keys bypass
this owner check. Typed property access and the ID-based set/remove methods enforce the check;
`ContainsProperty` returns `false` when a non-attached key is not valid for the view. An attached key
is still stored on the specific `View` used for the operation; the owner bypass does not route the value
to another view.

For now, `Loader.ApplyDataProperties` retains its `ZuiPropertyType` switch. Data, styledData, and
forwarded values go through generated `SetDataProperty`, which validates the value and assigns through
the typed `DataContext` property. Styled, property, attachedStyle, and attachedProperty values are
applied by the loader using their `PropertyKeyId`; attached kinds use the target view's local property
path. Data and forwarded metadata have no view property key. A `TimeSpan` theme value uses the standard
JSON time format, for example `"00:00:00.5"` for half a second.

## Collections

A collection uses `[]Type` and must use `bind: "data"`:

```json5
$data: {
    // Items shown by the control.
    items: {
        type: "[]Item",
        bind: "data"
    }
}
```

Rules:

- Collections cannot be nullable; `?[]Type` is invalid.
- Collection bindings cannot use `styledData`, `styled`, `property`, `attachedProperty`, `attachedStyle`, or a
  forwarded path.
- A regular `[]Type` becomes `ObservableCollection<TypeData>` in generated data code.
- A generic `[]Item` becomes `ObservableCollection<Item>` so a closed `ComboBox<Item>` retains its
  concrete item type.
- The generated data constructor creates a live, non-null `ObservableCollection<T>`.
- The generated controller does not synchronize collection contents with the view.
  Handwritten code-behind must react to collection changes and refresh the relevant visual controls.

## Runtime behavior and initialization

When a generated controller assigns its `DataContext`, it subscribes to `PropertyChanged` and
synchronizes applicable styled-data properties to the view. During initialization:

1. `Loader.Load` deserializes the embedded ZUI JSON, builds the control tree, and applies data properties
   recursively, children before parents.
2. After `Loader.Load` returns, generated initialization resolves named child controls and creates the control's
   default `DataContext`.
3. Parent-supplied child properties are included in the parent loader's recursive pass after the child
   controller has initialized.
4. Data property names are converted from camelCase to PascalCase before `SetDataProperty` dispatch.

Unknown data properties, invalid types, and null values for non-nullable properties produce errors.
Direct JSON data values should match the generated concrete property types.

Additional runtime details:

- Every binding has generated `ZuiPropertyInfo`. `styledData`, `styled`, `property`,
  `attachedProperty`, and `attachedStyle` bindings also have a `PropertyKeyId`; data and forwarded
  bindings do not, because they are applied through `DataContext`.
- Generated `SetDataProperty` validates the value type and assigns it to the typed `DataContext`
  property (it does not go through `Properties`).
- A `data` binding that declares `flags` calls `View.SetFlags` when it changes, so the view is
  invalidated even though the value is not mirrored to a view property.
- A `PropertyChanged` notification with a null or empty property name synchronizes all styled-data
  values to the view.
- Property key fields are named after the binding with a `Property` suffix (for example,
  `LongClickDurationProperty`), except `$`-prefixed Panel properties such as `Panel.Name`.

## Generated data classes and code-behind

Generated files are placed under the build-generated source directory and must not be edited manually.
Change the ZUI schema or generator instead. Add behavior or marker interfaces in a matching partial
file:

```csharp
namespace MyApp.Controls;

public sealed partial class ComboBoxItemBadgeData : global::ZurfurGui.Controls.IComboBoxItem
{
}
```

The corresponding ZUI file should declare the control's own `$data` properties and should not rely on
inherited generated data contracts. A marker interface can be supplied either with this handwritten
partial or with `$implements` in the ZUI file (see [ComboBox](combo-box.md)), not both.

