# ComboBox

`ComboBox<Item>` is a generic single-selection dropdown control. In ZUI, `Item` is written as the item controller name,
but generated C# closes the generic controller with that control's concrete data class.
The item data class contains the data needed by its renderer; selection state belongs to the combo box rather than
individual items.

## Files

| File | Purpose |
|------|---------|
| `src/ZurfurGui/Controls/ComboBox.zui.json5` | Generic view definition and data shape |
| `src/ZurfurGui/Controls/ComboBox.Control.cs` | Dropdown lifecycle, item creation, and selection synchronization |
| `src/ZurfurGui/Controls/ComboBoxItem.cs` | Minimal handwritten marker constraint |
| `src/ZurfurGui/Controls/ComboBoxItemText.zui.json5` | Built-in text item renderer, data shape, and marker declaration |

## Using ComboBox in a view

Because `ComboBox` is generic, use the item controller name in a concrete closed form in `.zui.json5`:

```json5
{
    $name: "_themeComboBox",
    $controller: "ComboBox<ComboBoxItemText>",
    $align: { horizontal: "left" }
}
```

The generator translates this to the C# controller type `ComboBox<ComboBoxItemTextData>`. Its generated data context is
`ComboBoxData<ComboBoxItemTextData>`, with an `ObservableCollection<ComboBoxItemTextData>` for `Items`.

The item data class must implement `IComboBoxItem`. An item view normally declares this with
`$implements: "IComboBoxItem"`; the generator emits the interface on the generated data class. The generator emits
`ComboBoxItemTextData` and `ComboBoxItemText` registrations during `ZurfurMain.InitializeControls()`.

## Populating and using ComboBox from code

Add data objects, not item controllers, to the collection:

```csharp
foreach (var label in new[] { "Zurfur Light", "Zurfur Dark", "Cherry Light", "Cherry Dark" })
{
    _themeComboBox.DataContext.Items.Add(new ComboBoxItemTextData { Text = new TextLines(label) });
}
_themeComboBox.DataContext.SelectedIndex = 0;
```

`Items` is a live `ObservableCollection<T>`. The generated controller does not synchronize collection changes by itself;
`ComboBox.Control.cs` creates item controllers when it synchronizes the selected item or opens the dropdown. Each item
controller receives the same data object through its `DataContext`.

### Reacting to selection changes

Subscribe to `DataContext.PropertyChanged` and check for `SelectedIndex`:

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

## Adding a ComboBox item renderer

A new item renderer declares its own view-shaped data. It does not inherit a generated data contract. If the item is used
with `ComboBox`, its ZUI definition declares `$implements: "IComboBoxItem"` so the generated data class satisfies the
generic constraint.

For example, `ComboBoxItemBadge.zui.json5` declares `Badge` and `Text` data:

```json5
{
    $controller: "ComboBoxItemBadge",
    $namespace: "TestApp.Test.Controls",
    $implements: "IComboBoxItem",
    $data: {
        badge: {
            type: "TextLines",
            bind: "_badge.text"
        },
        text: {
            type: "TextLines",
            bind: "_text.text"
        }
    },
    $layout: "Row"
}
```

Use the item in a view with the controller name, not the generated data class name:

```json5
{
    $name: "_badgeCombo",
    $controller: "ComboBox<ComboBoxItemBadge>",
    $align: { horizontal: "left" }
}
```

Populate it with the concrete generated data class:

```csharp
_badgeCombo.DataContext.Items.Add(new ComboBoxItemBadgeData
{
    Badge = new("A"),
    Text = new("Pick 1")
});
```

`IsSelected` is not an item property. Selection is owned by `ComboBoxData<TItem>.SelectedIndex`. Items also do not
receive common `IsEnabled` or `Tag` properties automatically; add such properties to the item's own data shape only
when the application requires them.

## Runtime item creation

The source generator registers each non-generic data-bearing item controller with its concrete data class. For
example, `ComboBoxItemBadgeData` is registered with `ComboBoxItemBadge`.

`ComboBox<TItem>` looks up the factory by the runtime concrete data type and passes the existing data object to it. The
factory creates the controller and assigns that object to `DataContext`. Runtime lookup does not use an item interface;
`$implements` only makes the generated data class satisfy the compile-time generic constraint.

If a new item type is not registered, opening the dropdown or synchronizing the selected item fails because
`Loader.CreateDataController` cannot find a factory for its runtime data type. Ensure the item has a ZUI file with a
`$data` section, is included as an `AdditionalFiles` input, and declares `$implements: "IComboBoxItem"`.
