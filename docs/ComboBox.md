# ComboBox

`ComboBox<Item>` is a generic single-selection dropdown control. The type parameter `Item` must satisfy the
`ComboBoxItem` constraint, which means its data interface must extend `IComboBoxItemData`. The built-in
`ComboBoxItemText` item type covers the common case of a text-only list; custom item types can be added without
touching the combo box itself.

## Files

| File | Purpose |
|------|---------|
| `src/ZurfurGui/Controls/ComboBox.zui.json` | Generic view definition and `.data` contract |
| `src/ZurfurGui/Controls/ComboBox.Control.cs` | Hand-written code-behind: dropdown lifecycle, item creation, selection sync |
| `src/ZurfurGui/Controls/ComboBoxItem.zui.json` | Constraint definition — the minimum data shape every item must provide |
| `src/ZurfurGui/Controls/ComboBoxItemText.zui.json` | Built-in text item renderer; fully generated, no hand-written code-behind |

## Using ComboBox in a view

Because `ComboBox` is generic, you always use a concrete closed form in `.zui.json`. Use
`ComboBox<ComboBoxItemText>` for the common text-only case:

```jsonc
{
    ".name": "_themeComboBox",
    ".controller": "ComboBox<ComboBoxItemText>",
    ".align": { "horizontal": "left" }
}
```

The generator translates this to the C# field type `ComboBox<IComboBoxItemTextData>` in the parent
controller's generated code. You refer to it in code-behind simply as `_themeComboBox`.

## Populating and using ComboBox from code

`_themeComboBox.DataContext.Items` is an `ObservableCollection<IComboBoxItemData>`. Add item data objects —
not item controls — to this collection. Use `ComboBoxItemTextData` (the generated data class) directly:

```csharp
foreach (var label in new[] { "Zurfur Light", "Zurfur Dark", "Cherry Light", "Cherry Dark" })
{
    _themeComboBox.DataContext.Items.Add(new ComboBoxItemTextData() { Text = [label] });
}
_themeComboBox.DataContext.SelectedIndex = 0;
```

The combo box creates the visual item controllers at dropdown-open time from the data objects in `Items`.
Never add a controller (e.g. `new ComboBoxItemText()`) to the collection — only data objects belong there.

### Reacting to selection changes

Subscribe to `DataContext.PropertyChanged` and check for `"SelectedIndex"`:

```csharp
_themeComboBox.DataContext.PropertyChanged += (s, e) =>
{
    if (e.PropertyName != "SelectedIndex")
        return;
    var idx = _themeComboBox.DataContext.SelectedIndex;
    // idx is int? -- null means no selection
};
```

`SelectedIndex` is `null` when nothing is selected. Set it programmatically to change the displayed item:

```csharp
_themeComboBox.DataContext.SelectedIndex = 2;
```

### DataContext type

The generated `DataContext` property on a `ComboBox<IComboBoxItemTextData>` field is typed as
`IComboBoxData<IComboBoxItemTextData>`:

```csharp
public interface IComboBoxData<Item> : INotifyPropertyChanged
    where Item : IComboBoxItemData
{
    ObservableCollection<IComboBoxItemData> Items { get; set; }
    int? SelectedIndex { get; set; }
}
```

Note that `Items` is typed as `ObservableCollection<IComboBoxItemData>` (the constraint interface), not the
concrete item type. This keeps the collection compatible with any item type that satisfies the constraint.

## How to add a new kind of combo box item

The generic design means you can add a new item renderer without modifying `ComboBox` at all. Here is the
full process, using `ComboBoxItemBadge` (a real item type in `samples/TestApp`) as the worked example. It
shows a red-outlined badge label on the left and a descriptive text label vertically centred to its right.

**1. Define the item view** (see `ComboBoxItemBadge.zui.json`):

```jsonc
// Combo box item that shows a badge label and a text label side by side.
// The badge is outlined in red; the text is vertically centred to the right of it.
// Use ".implements": "ComboBoxItem" and reference via ComboBox<ComboBoxItemBadge>.
{
    ".controller": "ComboBoxItemBadge",
    ".namespace": "TestApp.Test.Controls",
    ".implements": "ComboBoxItem",
    ".padding": "${spacing.horizontal.small | spacing.vertical.extra-small}",
    ".backgroundColor": "${color.interactive.item.background}",
    ".data": {
        // Short label shown inside the red-outlined badge pill.
        "badge": {
            "type": "TextLines",
            "bind": "_badge.text"
        },
        // Main descriptive text shown to the right of the badge.
        "text": {
            "type": "TextLines",
            "bind": "_text.text"
        }
    },
    ".layout": "Row",
    ".content": [
        {
            // Badge pill: red outline box around the badge text.
            ".borderWidth": "${stroke.width.medium}",
            ".borderColor": "${color.status.danger.stroke}",
            ".borderRadius": "${radius.corner.small}",
            ".padding": "${spacing.vertical.extra-small | spacing.horizontal.small}",
            ".align": "vertical:center",
            ".content": [
                {
                    ".name": "_badge",
                    ".controller": "TextView",
                    "TextView.color": "${color.status.danger.stroke}"
                }
            ]
        },
        {
            ".name": "_text",
            ".controller": "TextView",
            "TextView.color": "${color.text.primary}",
            ".padding": "${spacing.left.small | spacing.zero}",
            ".align": "vertical:center"
        }
    ]
}

```

Key points:
- `.implements": "ComboBoxItem"` causes the generator to emit `IComboBoxItemBadgeData : IComboBoxItemData`,
  satisfying the `where Item : IComboBoxItemData` constraint.
- `isEnabled` and `tag` from `ComboBoxItem` are **automatically inherited** — do not redeclare them.
  The generator emits ZUI006 and refuses to build if you do.
- `.layout": "Row"` is set on the item root itself (not on a wrapper child) to lay the badge and text
  side by side. Use `.align": { "vertical": "center" }` on each child to centre them in the row.

**2. Use the new item in a view** (`FormTestComboBox.zui.json`):

```jsonc
{
    ".name": "_badgeCombo",
    ".controller": "ComboBox<ComboBoxItemBadge>",
    ".align": { "horizontal": "left" }
}
```

**3. Populate from code** (`FormTestComboBox.Control.cs`):

```csharp
var items = new (string Badge, string Text)[]
{
    ("A", "Pick 1"), ("B", "Pick 2"), ("C", "Pick 3"), ("D", "Pick 4"),
};
foreach (var (badge, text) in items)
    _badgeCombo.DataContext.Items.Add(new ComboBoxItemBadgeData { Badge = new(badge), Text = new(text) });
_badgeCombo.DataContext.SelectedIndex = 0;
```

**4. Show the form** in `ZurfurMain.cs`:

```csharp
app.ShowWindow(new FormTestComboBox(), "ComboBox Test",
    location: new PointProp(650, 120),
    sizeRequst: new SizeProp(280, 200));
```

### Gotchas

**Do not use collection-expression syntax for `TextLines`.**
`TextLines` has a `[CollectionBuilder]` attribute, but `TextLinesBuilder.Create` is `internal` so the
`["value"]` collection expression will not compile from outside the `ZurfurGui` assembly. Use the
string constructor instead:

```csharp
// ? CS9187 — TextLinesBuilder.Create is internal
Badge = ["A"]

// ? correct
Badge = new TextLines("A")   // explicit
Badge = new("A")             // or target-typed new
```

**The namespace of `.implements` target does not have to match your namespace.**
The generator searches the entire compilation (source and referenced assemblies) for `I{Implements}Data`
by name. You do not need to redeclare or mirror the namespace — just use the short constraint name
(e.g. `"ComboBoxItem"`) and the generator resolves it.

**Cross-assembly `.implements` does not support collection properties.**
If the constraint control (e.g. `ComboBoxItem`) is compiled into a referenced DLL rather than the same
project, the generator synthesizes inherited bindings from Roslyn metadata. This works for scalar types
(`bool`, `string`, `TextLines`, nullable references, etc.) but not for `ObservableCollection<>` properties.
If you hit ZUI007, declare that property explicitly in your own `.data` section.

**Redeclaring inherited properties is a hard error (ZUI006).**
The generator emits ZUI006 and stops generating the control if you copy `isEnabled` or `tag` from the
constraint into your `.data`. Remove the duplicates — they are emitted automatically.

### Conventions for item renderers

- Declare only the properties your item adds beyond `isEnabled` and `tag`.
- Keep item data simple (text, icon path, enum values). Avoid business logic in item data classes.
- Do not add pointer-event handlers inside the item control. The combo box attaches `PointerClick`
  handlers to item views at dropdown-open time.
- Style every selector in every theme and dark-mode variant so the item looks correct everywhere.
- Name all style selectors after the controller class (`ComboBoxItemBadge`, `ComboBoxItemBadge.Text`,
  etc.) to avoid collisions with other item types.

## How it works

### Generic constraint and registration

`ComboBox` is declared in `ComboBox.zui.json` as:

```jsonc
{ ".controller": "ComboBox<Item> where Item : ComboBoxItem" }
```

The generator parses the type parameter (`Item`) and constraint (`ComboBoxItem`), then emits:

```csharp
public sealed partial class ComboBox<Item> : Controllable
    where Item : IComboBoxItemData { ... }
```

The constraint `ComboBoxItem` names another `.zui.json`-defined control. The generator maps that name to its
generated data interface (`IComboBoxItemData`) rather than the controller class, so the constraint lives
entirely in the data layer. A controller interface `IComboBoxItem` is also generated and used as the type
argument to `Loader.CreateDataController<IComboBoxItem>(itemData)` at runtime.

Because C# static fields on a generic class are per closed type, generated `PropertyKey` fields for
`ComboBox` live in a separate companion non-generic static class (`ComboBox`) rather than on `ComboBox<Item>`
itself. Hand-written keys such as `ScrimColor` also use `typeof(ComboBox<>)` as the owner type for the same
reason. At startup, `ZurfurMain.g.cs` calls `RuntimeHelpers.RunClassConstructor` for both the open generic
type and each registered closed form so all property keys are registered before style sheets load.

### Data contract

The generated data contract is:

```csharp
public interface IComboBoxData<Item> : INotifyPropertyChanged
    where Item : IComboBoxItemData
{
    ObservableCollection<IComboBoxItemData> Items { get; set; }
    int? SelectedIndex { get; set; }
}
```

`Items` is typed as `ObservableCollection<IComboBoxItemData>` (the constraint interface) even though the
combo box is closed on a specific `Item`. This allows the collection to hold any conforming item data object
without making the collection itself generic.

Because `Items` is a collection binding, the generator does **not** auto-sync it to the view. The
hand-written `ComboBox.Control.cs` owns all collection observation and item-view lifecycle.

### Selected item display

The `_selectedItem` child is a plain `Panel`. When `SelectedIndex` changes (via `PropertyChanged` or on
initial sync), `SyncSelectedItem()` clears `_selectedItem`'s children and, if the index is valid, calls
`CreateItemController(itemData)` to instantiate a fresh item controller and adds its view as the sole child.
The selected-item display is therefore always a live item controller rendered inline, using the same item
renderer as the dropdown rows.

### Dropdown lifecycle

When the combo box is clicked, `OpenDropdown(AppWindow)` runs:

1. **Dismiss overlay** — a full-screen `Panel` is added as a floating panel. Its `BackgroundColor` is
   set to `View.GetStyle(ScrimColor)` so the tint comes from the active theme. The alpha must be greater
   than 16 (`DrawHelper.ALPHA_HIT_THRESHOLD`) so the overlay registers pointer hits; a fully transparent
   overlay will not close the dropdown when clicked. Clicking the overlay calls `CloseDropdown()`. Because
   `AppWindow.ShowFloatingPanel` always sets alignment to `Left/Top`, the overlay's `Stretch/Stretch`
   alignment must be applied **after** the call.

2. **Popup panel** — a second floating `Panel` with `LayoutColumn` is positioned at the logical coordinates
   just below the combo box. Each item in `DataContext.Items` gets an item controller created via
   `CreateItemController(itemData)`. Clicking an item sets `DataContext.SelectedIndex`, calls
   `SyncSelectedItem()`, and calls `CloseDropdown()`.

`CloseDropdown()` removes both floating views via `View.RemoveFromParent()`. `OnDetach()` also calls
`CloseDropdown()` so the dropdown is never orphaned when the control is removed from the tree.

### Coordinate system

`View.Origin` is in **device pixels**; `View.Size` and `View.Offset` are in **logical pixels**. When
positioning the popup, divide `Origin` by `View.Scale` before adding `Size.Height`:

```csharp
var logicalX = origin.X / scale;
var logicalY = origin.Y / scale + size.Height;
```

### Item controller creation

`CreateItemController(IComboBoxItemData itemData)` calls:

```csharp
Loader.CreateDataController<IComboBoxItem>(itemData)
```

The runtime looks up the factory by the runtime type of `itemData` (e.g. `ComboBoxItemTextData`) in a
dictionary built during control registration. It instantiates the matching controller
(e.g. `ComboBoxItemText`) and sets its `DataContext`. The combo box never references the concrete item type
directly, so any registered item type works transparently.
