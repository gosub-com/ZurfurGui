# ComboBox

## Overview

`ComboBox<Item>` is a generic single-selection dropdown control. In ZUI, `Item` is written as the item
controller name, but generated C# closes the generic controller with that control's concrete data class.
The item data class contains the data needed by its renderer; selection state belongs to the combo box
rather than individual items.

The C# namespace for these types is `ZurfurGui.Controls.ComboBox`. A view that uses the control
should include that namespace in its `$use` list.

## Using ComboBox in a view

Because `ComboBox` is generic, use the item controller name in a concrete closed form in `.zui`:

```
{
    $name: "_themeComboBox",
    $controller: "ComboBox<ComboBoxItemText>",
    $align: { horizontal: "left" }
}
```

The item data class must implement `IComboBoxItem`. An item view normally declares this with
`$implements: "IComboBoxItem"` in its ZUI definition.

## Populating and using ComboBox from code

Add data objects, not item controllers, to the collection:

```csharp
foreach (var label in new[] { "Zurfur Light", "Zurfur Dark", "Cherry Light", "Cherry Dark" })
{
    _themeComboBox.DataContext.Items.Add(new ComboBoxItemTextData { Text = new TextLines(label) });
}
_themeComboBox.DataContext.SelectedIndex = 0;
```

`Items` is a live `ObservableCollection<T>`.

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

`SelectedIndex` is `null` when nothing is selected. Set it programmatically to change the displayed
item:

```csharp
_themeComboBox.DataContext.SelectedIndex = 2;
```

## Adding a ComboBox item renderer

A new item renderer declares its own view-shaped data. If the item is used with `ComboBox`, its ZUI
definition declares `$implements: "IComboBoxItem"`.

For example, `ComboBoxItemBadge.zui` declares `Badge` and `Text` data:

```
{
    $controller: "ComboBoxItemBadge",
    $namespace: "TestApp.Test.Controls",
    $use: ["ZurfurGui.Controls.ComboBox"],
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

```
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

`IsSelected` is not an item property. Selection is owned by `ComboBoxData<TItem>.SelectedIndex`. Items
also do not receive common `IsEnabled` or `Tag` properties automatically; add such properties to the
item's own data shape only when the application requires them.

<!-- INTERNAL -->

## Implementation Section

### Files

| File | Purpose |
|------|---------|
| `src/ZurfurGui/Controls/ComboBox/ComboBox.zui` | Generic view definition and data shape |
| `src/ZurfurGui/Controls/ComboBox/ComboBox.Control.cs` | Dropdown lifecycle, item creation, and selection synchronization |
| `src/ZurfurGui/Controls/ComboBox/ComboBoxItem.cs` | Minimal handwritten marker constraint |
| `src/ZurfurGui/Controls/ComboBox/ComboBoxItemText.zui` | Built-in text item renderer, data shape, and marker declaration |

### Generated types and item registration

The generator translates `ComboBox<ComboBoxItemText>` to the C# controller type
`ComboBox<ComboBoxItemTextData>`. Its generated data context is
`ComboBoxData<ComboBoxItemTextData>`, with an `ObservableCollection<ComboBoxItemTextData>` for `Items`.
The generator emits `IComboBoxItem` on the generated data class and registers it during
`ZurfurMain.InitializeControls()`.

The generated controller does not synchronize collection changes by itself. `ComboBox.Control.cs`
creates item controllers when it synchronizes the selected item or opens the dropdown. Each item
controller receives the same data object through its `DataContext`.

Include each custom item ZUI file as an `AdditionalFiles` input so initialization can register its
concrete data type. Without that registration, creating an item controller fails. See
[Initialization](initialization.md).
