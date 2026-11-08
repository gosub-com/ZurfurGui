# ListBox

`ListBox<Item>` is a scrolling, vertical list of items with optional selection. The item controller
is chosen by the item's data type, for example `ListBox<ListBoxItemText>`. Items know nothing about
selection; the list box owns it.

## Using it in a ZUI file

```json
{
	$name: "_names",
	$controller: "ListBox<ListBoxItemText>",
	$sizeRequest: "width:160,height:300",
	selectionMode: "multiple",
	items: [
		{ text: [ "One" ] },
		{ text: [ "Two" ] }
	]
}
```

- `items` is a list of the item's data class (`ListBoxItemTextData` here).
- `selectionMode` is `none`, `single` (the default), or `multiple`.
- Give the list box a height, otherwise it is as tall as its content.
- Use `SelectionChanged` in code-behind to react to the user:

```csharp
_names.SelectionChanged += (s, e) => Show(_names.SelectedIndices);
```

### Selection API

- `SelectedIndices`: the selected indices in ascending order.
- `SelectedIndex`: the index if exactly one item is selected, otherwise `null`.
- `SelectionChanged`: raised when the set of selected items changes (user input, code, or removal
  of a selected item). It is not raised when an index only shifts because of an add or remove
  elsewhere.
- `Select(index)`, `SetSelectedIndices(indices)`, `ClearSelection()`: change the selection from code.
- `SelectNearest(index)`: selects `index` clamped to the last item, or clears if the list is empty.
  Call it from `SelectionChanged` to select the neighbor of a removed item.

User input: click selects one item. In `multiple` mode Ctrl+click toggles an item and Shift+click
selects the range from the last item clicked without Shift. There is no keyboard support yet.

## Adding and removing items at runtime

`DataContext.Items` is an `ObservableCollection`. Change it and the list box updates:

```csharp
_names.DataContext.Items.Add(new ListBoxItemTextData() { Text = new TextLines("Three") });
_names.DataContext.Items.Insert(0, new ListBoxItemTextData() { Text = new TextLines("Zero") });
_names.DataContext.Items.RemoveAt(2);
```

Selection follows the items. Inserting at or before a selected item shifts its index up, and
removing before it shifts it down. Removing a selected item deselects it (no neighbor is selected
automatically) and raises `SelectionChanged`. Clearing the collection deselects everything.
Assigning a new `Items` collection also clears the selection. To delete the selected items, remove
from the highest index down:

```csharp
foreach (var index in _names.SelectedIndices.Reverse().ToList())
	_names.DataContext.Items.RemoveAt(index);
```

## Creating your own list box items

An item is a normal ZUI view that declares `$implements: "IListBoxItem"`. Its generated data class
then implements `IListBoxItem`, and `ListBox<MyItem>` works. Do not set a background on the item,
because the row behind it draws the hover and selected colors. See
`samples/TestApp/TestApp/Controls/ListBoxItemBadge.zui`.

```json
{
	$controller: "ListBoxItemBadge",
	$namespace: "TestApp.Test.Controls",
	$implements: "IListBoxItem",
	$data: {
		text: { type: "TextLines", bind: "data:_text.text" }
	},
	$content: [ { $name: "_text", $controller: "TextView" } ]
}
```

Use it with `$controller: "ListBox<ListBoxItemBadge>"`, and add `$use` for the item's namespace in
the form's ZUI file when it is not the same namespace.

## How it works

Files, all in `src/ZurfurGui/Controls` unless noted:

| File | Purpose |
|---|---|
| `ListBox.zui` | Generic view. `$data` has `items` (`[]Item`) and `selectionMode`. Content is a vertical `ScrollViewer` holding a `Column` panel named `_rows`. |
| `ListBox.Control.cs` | Behavior: rows, selection, click handling, collection changes. Also declares the `SelectionMode` enum. |
| `ListBoxRow.zui`, `ListBoxRow.Control.cs` | One row per item. Hosts the item view, draws the background, and has the `IsSelected` property. |
| `ListBoxItem.cs` | The empty `IListBoxItem` marker interface. |
| `ListBoxItemText.zui` | Built-in text item with a `text` field. |
| `Styles/*.zth` | Theme variable `color.listbox.item.background`. |

Key points for modifying it:

- **Selection is not in the generated data.** The generator turns `[]Type` into
  `ObservableCollection<TypeData>`, so a collection of `int` is not possible. The selection lives
  in the controller as a private sorted `List<int> _selected`, plus `_anchor` for Shift ranges.
  `SelectedIndex` is derived from it.
- **Full rebuild.** `RebuildRows` clears `_rows` and creates one `ListBoxRow` per item, each with a
  `ClickRecognizer` that captures its index. Every item collection change rebuilds, so the captured
  indices stay correct. Diffing and virtualization are future work.
- **Item creation.** `CreateItemController` is the only place that calls
  `Loader.CreateDataController`, so a future item factory can replace it.
- **Collection changes.** `OnItemsChanged` first calls `AdjustSelection`, which remaps `_selected` and
  `_anchor` for add, remove, replace, move, and reset. It returns true only when selected items
  were removed or replaced, which is when `SelectionChanged` is raised, after the rebuild.
- **Subscriptions.** The `Items` collection is subscribed to in `OnAttach` and unsubscribed in
  `OnDetach`. `OnAttach` also rebuilds, which covers items added while detached. The
  `DataContext.PropertyChanged` subscription is made once in the constructor, as `ComboBox` does.
  It handles `Items` replaced and `SelectionMode` changed. Replacing `DataContext` itself is not
  supported.
- **Colors.** The selected, pressed, and hover colors come from the theme variable
  `color.listbox.item.background`. It uses the condition `ListBoxRow.isSelected`, which is the
  `isSelected` property declared in `ListBoxRow.zui` with `bind: "property"` and flags
  `style,render`. Setting `IsSelected` invalidates the row's style cache. Selected uses the same
  color as pressed. Each theme also has the generic `color.interactive.item.background` that
  `ComboBox` items use.
- **Hit testing.** The row has `$hitTest: "Always"` so a transparent row still receives hover and
  clicks.
- **Selection mode.** `SetSelectedIndices` applies the mode: `none` clears and `single` keeps only the
  first index. Changing `selectionMode` re-applies it.

The test form is `samples/TestApp/TestApp/Test/FormTestListBox.zui`. It has one list per selection
mode, one with a custom item, and buttons to add at the start, add at the end, and delete the
selected items.

Not implemented yet: keyboard input, focus, virtualization, fixed row heights, and diffing.
The test form also has no check box that calls `SelectNearest` from `SelectionChanged`, because
`CheckBox` has no click event yet. The method itself is implemented.
A `.zui` file cannot set the initial selection, because the selection is not part of the generated
data (see the first key point above). Set it in code with `Select` or `SetSelectedIndices`.
