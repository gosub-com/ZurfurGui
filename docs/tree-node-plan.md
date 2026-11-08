# Tree View and List Box Plan

This document records the design conclusions for `ListBox` and `TreeView`, then lays out the plan to
build the list box first and the tree view second. Priority: **keep things simple**.

## 1. Design summary and conclusions

### Goals

- The immediate goal is a tree view. A list box comes first because it is the tree without hierarchy.
- Make good decisions for a generic GUI, but do not over-generalize. Past experience: very generic
  GUI systems are harder to use than WinForms, which was easy to make do everything.
- Simplicity (easy to understand and work with) wins over maximum flexibility.

### Current system

- `ComboBox<Item>` is generic. The item controller is named in `.zui`, and the generator closes the
  generic with the item's concrete data class (for example `ComboBoxItemTextData`).
- Each item renderer declares its own view-shaped data (icons are just data fields).
- At runtime `Loader.CreateDataController` maps the concrete data type to a controller factory. This is
  already a type-keyed template mechanism.
- Selection is owned by the container (`SelectedIndex`), not by items.

### Conclusions

- **The current system is powerful enough for the tree.** Icons and other customization are data fields
  in the node's data shape.
- **Keep generics, keep them simple.** Typing stops at the item data class. Users set D directly in
  code or deserialize it from JSON, so `ObservableCollection<ConcreteData>` is JSON and AOT friendly.
  Interface-typed collections would need polymorphic JSON handling, which we want to avoid.
- **Do not add a templating syntax** (such as `$itemTemplate`). It would add a second mechanism and pull
  toward runtime bindings, which MDV avoids.
- **One renderer per container.** Mixed node types are not needed. A tree node is one control with
  data for text, icon, and so on. Leaf versus non-leaf is derived from `Children.Count` (or a
  visibility flag in the node view). The runtime lookup by concrete data type already allows mixed
  types later, so do not make decisions that prevent it, but build nothing extra for it.
- **Selection policy differs by control.** `ListBox` items carry no `IsSelected` (the list box
  owns selection as indices). `TreeView` nodes carry `IsSelected` in their data (see below).
- **One marker interface per container** (`IComboBoxItem`, `IListBoxItem`, `ITreeNode`), kept
  separate (see below).
- **App-level control replacement (checkboxes, etc.) is out of scope.** Plain controls such as
  `CheckBox` do not use the item machinery. The by-name `$controller` registration is enough if this is
  revisited, so nothing is decided now.
- **Tree owns layout rows.** The tree wraps each node renderer in a row with indent and expander, and
  flattens visible nodes into a list. The item renderer only draws content.
- **`IsExpanded` lives in node data.** D is the state, so it is serializable and the app can set it.
  The tree listens to `PropertyChanged`.
- **Keep `IComboBoxItem`, `IListBoxItem`, and `ITreeNode<TSelf>` separate.** The list and combo
  marker interfaces are empty, so sharing saves nothing, and the item factory work in
  [future-improvements.md](future-improvements.md) (Priority 2) may shrink or remove them. Merge only
  if their members become identical. `ITreeNode<TSelf>` is not empty (see the next bullet).
- **`ITreeNode<TSelf>` is a self-typed generic interface, filled in by the node author (option 3).**
  It declares `ObservableCollection<TSelf> Children { get; }`, `bool IsExpanded { get; set; }`, and
  `bool IsSelected { get; set; }`, with `where TSelf : ITreeNode<TSelf>`. A node's `.zui` uses only
  the general mechanisms the generator already has: `$implements: "ITreeNode<MyNodeData>"` (passed
  through verbatim) and `$data` fields named `children` (`[]MyNode`), `isExpanded`, and `isSelected`.
  The generated properties satisfy the interface implicitly. `TreeView` is declared as
  `TreeView<Item> where Item : ITreeNode<Item>`. The generator knows nothing about trees.
- **Rationale for the generic pass-through.** Alternatives considered:
  - *Generator special case for `ITreeNode`* (it emits `Children`, `IsExpanded`, `IsSelected`, and an
    explicit `IReadOnlyList<ITreeNode> ChildNodes`). Keeps the interface a plain marker and the author
    writes less, but the generator then has tree-specific rules. Rejected: a general purpose library
    should not hard-code one control's members in the generator, and every future control with
    required members would want its own special case.
  - *Non-generic `ITreeNode` with `ChildNodes` written by the author.* Needs the author to add a
    partial-class member to each node. More boilerplate than option 3.
  - *Reflection or a delegate* to find children. Not AOT friendly, and every user must wire it.
  - *Generic pass-through (chosen).* The only generator change is to allow a generic type in the
    `where` constraint. Everything else is existing `$implements` and `$data` behavior. Errors (a
    missing member or a wrong name in `$implements`) surface as ordinary C# compile errors.
  - Costs accepted: the author writes the node's own name twice and declares three fields; the
    self-referential constraint is a little harder to read; and a tree of mixed node types would be
    harder later, because there is no non-generic common type. Mixed trees are out of scope.
- **Selection policy is per control.** `ListBox` keeps selection as indices in its D (`SelectedIndex`,
  like `ComboBox`). The app relates indices to items itself through `Items`. Items never know about
  selection, and there are no attached properties and no separate selection model class. `TreeView`
  keeps selection as an `IsSelected` flag in each node's data (see the next bullet). It shares only
  `SelectionMode` with the list box.
- **TreeView selection is `IsSelected` in node data (option B).** `ITreeNode` requires
  `bool IsSelected { get; set; }` and `bool IsExpanded { get; set; }`, declared as plain `$data`
  fields in the node's `.zui`. The tree listens to `PropertyChanged` on each node (already needed for
  `IsExpanded`). It caches the last selected node and exposes it as `SelectedNode` for convenience.
  In `Single` mode a click clears the old node's flag and sets the new one. `SelectionChanged` is
  raised when a flag flips, from user input or app code, and when a selected node is removed (the
  cached reference is cleared). `SelectedNodes` is a computed walk of the tree, not stored. The row
  binds to the flag through the style system, like `ListBoxRow.isSelected`. If the app sets flags
  that break the mode (two selected in `Single`), the tree tolerates or repairs it.
- **Rationale for `IsSelected` in node data.** Alternatives considered:
  - *Tree owns a set of node references.* Keeps D pure, but selection cannot be set from JSON or
    `.zui`, is not observable through D, needs a reference set, and removing a node requires
    walking its subtree to drop selected descendants. The app must ask the tree whether a node is
    selected.
  - *Hybrid (tree owns reference, flag is view-only).* Duplicates state for no benefit.
  - *Index path.* Shifts under edits like list box indices, and is harder to maintain.
  - *Flag in D (chosen).* Selection is plain serializable data, can be set from `.zui` or JSON,
    and needs no cleanup on removal because a removed node takes its flag with it. It is consistent
    with `IsExpanded`, makes multi-select and Shift ranges over the flattened list trivial, and
    lets the app walk its own data (`if (node.IsSelected)`). WPF developers commonly add
    `IsSelected` and `IsExpanded` to tree view models for the same reason.
  - Costs accepted: the tree and list box use different models, the same data shown in two trees
    shares selection, and the tree must repair invalid flag combinations.
  - *Why `ListBox` keeps indices:* list items have no identity and an index is a simple handle, and
    the tree's problem (no stable index) does not exist there. Revisit if initial selection from
    `.zui` is wanted for lists. Then add `IsSelected` to `IListBoxItem` and keep the index API as a
    computed view.
- **Selection mode.** `SelectionMode` is `None`, `Single`, or `Multiple`. `ListBox` version 1
  implements all three. `SelectedIndices` is in D for `Multiple`, with `SelectedIndex` as the
  focus/anchor item (the last item clicked). In `Single` mode `SelectedIndices` holds the same one
  item as `SelectedIndex`, or nothing. In `None` mode clicks and keys do nothing and both stay empty.
  `TreeView` version 1 implements only `None` and `Single`.
- **Removing the selected item de-selects it by default.** `SelectedIndex` becomes `null`. Removing
  an item before the selection shifts the index down, and inserting at or before it shifts it up.
  Reset sets it to `null` and clears `SelectedIndices`. For multi-select, the removed index leaves
  `SelectedIndices` and higher indices shift, with no neighbor auto-selected. The focus/anchor moves
  to the nearest remaining selected index, or `null`.
- **Auto-select next is easy but opt-in.** The control provides one function, for example
  `SelectNearest(int index)`, that selects the item at `index` (clamped to the last item, or `null`
  if empty). An app calls it from `SelectionChanged` when it wants neighbor selection. In `Multiple`
  mode it replaces the selection with that one item.
- **`SelectionChanged` fires only when the selected item changes.** It fires on user input, when the
  app selects a different item, and when the selected item is removed. It does not fire when an index
  merely shifts because of an insert or remove elsewhere. In `Multiple` mode it fires when the set of
  selected items changes. `PropertyChanged` on `SelectedIndex` or `SelectedIndices` is the raw data
  signal and can fire on a shift without a selection change.
- **Reset and rebuild first.** Both controls start with the full-rebuild strategy. Diffing is later
  work (Priority 1 in future-improvements.md). No stable item keys for now.
- **Separate the collection synchronizer from row creation.** Collection handling is a small piece
  that normalizes add, remove, replace, move, and reset. The tree reuses it once per child
  collection.
- **One item-creation method.** The controls call `Loader.CreateDataController` through a single
  method so Priority 2 can replace it with an item factory without touching the rest.
- **Detach cleanup is required.** Collection and item subscriptions must be removed on detach
  (Priority 3 in future-improvements.md).
- **Icons in version 1 are a `TextLines` data field.** There is no image control yet. It will be added
  later as a separate task and would add an image field to the node data.

## 2. Plan: ListBox (implemented and tested first)

Manual testing only. No automated test cases.

### Design

- `ListBox<Item>` is a generic list, modeled on `ComboBox<Item>`, with a selection mode.
- Data: `ListBoxData<TItem>` with `Items` (`ObservableCollection<TItem>`), `SelectedIndex`
  (`int?`, `null` means no selection), `SelectedIndices` (`ObservableCollection<int>`, kept sorted,
  used in `Multiple` mode), and `SelectionMode` (default `Single`). The app gets the selected item
  with `Items[SelectedIndex.Value]`, or the selected items by mapping `SelectedIndices`.
- The control listens to `PropertyChanged` on its D, so the app can set `SelectedIndex` and the
  highlight follows. Clicks and keys write `SelectedIndex` back to D, which is the single source of
  truth.
- Items must implement `IListBoxItem`, declared in the item view with `$implements: "IListBoxItem"`.
- Built-in item renderer `ListBoxItemText` with a `Text` data field.
- The list box shows all items in a vertical stack inside a scroll viewer, with a highlight on the
  selected row (every selected row in `Multiple` mode). Clicking a row sets `SelectedIndex`. Arrow
  keys move the selection.
- Multiple mode input, kept in one small helper: click selects only that row, Ctrl+click toggles a
  row, Shift+click selects the range from the anchor, Shift+arrow extends the range, and Ctrl+A
  selects all.
- No virtualization in the first version. Keep the row-hosting code isolated so the tree can reuse it.

### Steps

1. Study `ComboBox.zui`, `ComboBox.Control.cs`, `ComboBoxItem.cs`, `ComboBoxItemText.zui`, and the
   generator's generic handling. `IListBoxItem` stays separate from `IComboBoxItem`.
2. Add `src/ZurfurGui/Controls/ListBoxItem.cs` with the minimal marker interface.
3. Add `src/ZurfurGui/Controls/ListBoxItemText.zui` (text item renderer, data shape, and
   `$implements: "IListBoxItem"`).
(generic view and `$data` with `Items`,
   `SelectedIndex`, `SelectedIndices`, and `SelectionMode`), reusing the scroll viewer.
5. Add `src/ZurfurGui/Controls/ListBox.Control.cs`:
   - Create item controllers from each data object through one private method that calls
     `Loader.CreateDataController`.
   - Put collection handling in a small separate synchronizer (add, remove, replace, move, reset)
     that the tree can reuse. The list box responds with a full rebuild.
   - Unsubscribe from the collection and items on detach.
   - Selection policy: removing the selected item sets `SelectedIndex` to `null`. In `Multiple`
     mode the removed index leaves `SelectedIndices` and higher indices shift. Index shifts from
     add or remove elsewhere do not raise `SelectionChanged`.
   - Provide `SelectNearest(int index)` so an app can opt in to neighbor selection from
     `SelectionChanged`.
   - Handle pointer click to set `SelectedIndex`, and arrow, Home, and End keys to move it. Apply
	 the Ctrl, Shift, and Ctrl+A rules in `Multiple` mode. Do nothing in `None` mode.
   - Update the selected row highlight when `SelectedIndex` or `SelectedIndices` change.
   - Factor row hosting (item to row view, highlight, hit test) into a small reusable helper for the
	 tree.
6. Confirm `ListBox.zui` and `ListBoxItemText.zui` are included as `AdditionalFiles` so the generator
   registers them.
7. Add `FormListBoxTest` to `samples/TestApp/TestApp/Test/` (a `.zui` plus `.Control.cs`, following
   `FormTestComboBox`), and register it in the test app's form list:
   - Four list boxes side by side:
     - Three using the built-in `ListBoxItemText`, each populated with a few dozen items, with
       `SelectionMode` set to `None`, `Single`, and `Multiple` respectively.
     - One using a custom item (such as `ListBoxItemBadge`, badge plus text) defined in the test app,
       not in the main library, similar to `ComboBoxItemBadge`, with `Multiple` selection.
   - Each list box has a label showing its `SelectedIndex` (and `SelectedIndices` where used).
   - Buttons to add an item, remove the selected item(s), and set the selection programmatically.
     The buttons act on the list box chosen by the user, or each list box has its own buttons.
   - A check of `SelectNearest` called from `SelectionChanged` on one list box (for example the
     `Single` one), toggled by a check box.
8. Build, then manually test in the Windows and Browser hosts: click selection, keyboard selection,
   Ctrl and Shift selection in the `Multiple` boxes, no selection in the `None` box, add and remove
   while selected (including items before the selection), empty list, scrolling with many items.
9. Add `docs/list-box.md` modeled on `docs/combo-box.md`, and link it from the documentation map in
   `docs/architecture.md`.

## Implemented list box details

This section records how the list box port went. The behavior is documented in
[list-box.md](list-box.md).

### Overview

The list box was built close to the plan, modeled on `ComboBox<Item>`. New files in
`src/ZurfurGui/Controls`: `ListBox.zui`, `ListBox.Control.cs`, `ListBoxRow.zui`,
`ListBoxRow.Control.cs`, `ListBoxItem.cs`, and `ListBoxItemText.zui`. Each theme got a
`color.listbox.item.background` variable. The test form is `FormTestListBox` in the test app, with a
custom `ListBoxItemBadge` item. All of it builds. It was not run, so manual testing is still
needed.

### Differences from the plan

- **Selection is controller state, not data.** The plan put `SelectedIndex` and `SelectedIndices`
  in D. The generator turns `[]int` into `ObservableCollection<intData>`, so an integer collection
  cannot be declared in `$data`. Rather than change the generator, the selection is a private
  sorted list in the controller. `SelectedIndices` is public and read-only, and `SelectedIndex` is
  derived (the index when exactly one item is selected, otherwise `null`). Consequences:
  `PropertyChanged` is not a selection signal (use `SelectionChanged`), and a `.zui` file cannot set
  the initial selection. If the tree needs selection in D, the generator needs primitive
  collection support first.
- **`SelectedIndex` has no setter.** Code selects with `Select`, `SetSelectedIndices`,
  `ClearSelection`, and `SelectNearest`.
- **No separate collection synchronizer.** The plan wanted a reusable synchronizer and a reusable
  row-hosting helper. Since the tree is not being built now, both live inside `ListBox` as small
  private methods (`AdjustSelection`, `RemapSelection`, `RebuildRows`). Extract them when the tree
  needs them.
- **No keyboard or focus.** There is no focus concept yet, so the arrow, Home, End, and Ctrl+A
  rules were dropped. Only pointer input exists.
- **Hover and selected colors.** The plan did not cover them. See the first problem below.
- **Test form.** There is no `SelectNearest` check box because `CheckBox` has no click event. The
  add and delete buttons act on all four list boxes instead of a chosen one.

### Problems and how they were solved

- **Selected highlight needs a theme condition.** Theme variables only know `isPointerOver` and
  `isPressed`, plus bool properties named `<Controller>.<name>`. A row is a plain panel, so there
  was nothing to attach `isSelected` to. The fix is a tiny `ListBoxRow` control with a
  `bind: "property"` bool (the same pattern `ScrollBar.isExpanded` uses), so themes can say
  `ListBoxRow.isSelected ? ...`. Setting it calls `InvalidateStyleCacheInternal`, as `ScrollBar`
  does. The row also draws the hover color, so items must not set their own background (unlike
  `ComboBoxItemText`).
- **Transparent rows were not hit.** A fully transparent background fails the hit test. The row
  uses `$hitTest: "Always"`, as `ScrollViewer` does.
- **Items are attached late.** `OnAttach` only runs once the tree reaches an `AppWindow`, and it
  can run again after a detach. The collection is subscribed in `OnAttach`, unsubscribed in
  `OnDetach`, and `OnAttach` rebuilds the rows to cover changes made while detached. The cost is a
  redundant detach message for old rows on a re-attach, which is harmless here.
- **Selection must follow collection changes.** Add, remove, replace, move, and reset each map the
  selected indices (and the Shift-click anchor) through one remap function. `SelectionChanged` is
  raised only when a selected item was removed or replaced, or on reset, and it is raised after the
  rows are rebuilt so handlers such as `SelectNearest` see a consistent list.
- **Tool and encoding noise.** The editor tool wrote tabs into new `.zui` files, and a script edit of
  the themes added a BOM. Both were converted back to spaces and no BOM. A first edit to
  `ListBox.zui` also dropped a property name and broke the JSON, which the build reported
  immediately.
- **Enum in `$data`.** `SelectionMode` is declared as an enum in `$data` with a default. The generator
  handled it, and JSON strings such as `"multiple"` work because the loader uses a camel-case string
  enum converter.

### Carry-over for the tree

- Reuse `SelectionMode` and the `ListBoxRow` idea (a row control with a theme-visible bool).
- Primitive collection support in the generator is not needed, because tree selection is a bool
  flag in node data.
- Extract the collection remapping and row building from `ListBox` when the tree needs them.

## 3. Plan: TreeView

### What the tree adds beyond the list box

| Area | ListBox | TreeView |
|------|---------|----------|
| Item shape | Flat `Items` | Each node has `Children` of its own type, plus `IsExpanded` |
| Marker interface | `IListBoxItem` | `ITreeNode<TSelf>`, requires `Children`, `IsExpanded`, and `IsSelected` |
| Generated data | `Items` typed to the item | Node data class with `Children` typed to itself, declared by the author in `$data` (`[]MyNode`) |
| Rows | One row per item | One row per visible node: indent by depth, expander, then content |
| Row source | The items collection | A flattened list of visible nodes, rebuilt on change |
| Collection changes | Watch `Items` | Watch `Items` and every expanded node's `Children` |
| State | `SelectedIndex` in the controller | Per-node `IsSelected` and `IsExpanded` in node data. The control has computed `SelectedNode` (cached) and `SelectedNodes` (tree walk), nothing stored in the tree's own D |
| Keyboard | Up, Down, Home, End | Also Left (collapse or go to parent), Right (expand or go to child) |
| Pointer | Click selects | Click selects, expander click toggles, optional double-click toggles |
| Leaf handling | None | Leaf is `Children.Count == 0`, expander hidden |

Not added in the tree (to keep it simple): mixed node types, templates, virtualization, drag and drop,
multi-select (version 1 supports `None` and `Single`), and lazy-loading children.
virtualization is possible later because rows come from a flattened list.

### Steps

1. Review the list box implementation and the row-hosting helper. Fix any friction found while
   testing the list box before building on it.
2. Loosen the generator's `$controller` regex so the `where` constraint may be a generic type. In
   `src/ZurfurGuiGen/ZuiInput.cs`, `s_genericControllerRegex` is
   `^(\w+)<(\w+)>\s+where\s+\w+\s*:\s*(\w+)$`, and the constraint group `(\w+)` rejects
   `ITreeNode<Item>`. Change that group to accept any text (for example `(.+)`). The constraint is
   already used verbatim in `ZuiEmitData.cs` and `ZuiEmitController.cs` (via the identity function
   `GetConstraintType`), so nothing else changes. No tree-specific generator code is added. The
   generator's `GetBindingDataType` already ties the type parameter to the literal name `Item`, which
   `TreeView<Item>` keeps.
3. Prototype before building the tree: add the `ITreeNode<TSelf>` interface and a first
   `TreeNodeText.zui`, then build. This confirms three things that were read from the generator but not
   built: the regex change works, `type: "[]TreeNodeText"` inside `TreeNodeText.zui` generates
   `ObservableCollection<TreeNodeTextData>` (a self-reference), and the generated properties satisfy
   the interface. If the self-reference fails, fix that in the generator (it must allow a collection of
   the file's own controller) and note it here.
4. Update `docs/code-generation.md` to say the `where` constraint may be a generic type, with the
   `TreeView<Item> where Item : ITreeNode<Item>` example, and that `$implements` is passed through
   unchanged.
5. Add `src/ZurfurGui/Controls/TreeNode.cs` with `ITreeNode<TSelf>` (it is not empty, unlike the other
   item interfaces) and its XML comments.
6. Finish `src/ZurfurGui/Controls/TreeNodeText.zui`, the built-in node renderer. Declare
   `$implements: "ITreeNode<TreeNodeTextData>"` and `$data` fields `children` (`[]TreeNodeText`),
   `isExpanded`, `isSelected` (all `bind: "data"`), `text`, and an optional `icon` (`TextLines`).
7. Add `src/ZurfurGui/Controls/TreeView.zui`: `$controller: "TreeView<Item> where Item :
   ITreeNode<Item>"`, with `Items` (`[]Item`, root nodes) and `SelectionMode` (`None` or `Single`).
   Selection is the `IsSelected` flag in node data. The control exposes a cached `SelectedNode`, a
   computed `SelectedNodes`, a `SelectionChanged` event, and `Select(node)` and `ClearSelection()`.
   Removing the selected node clears the cached reference and raises `SelectionChanged`, and the
   `SelectNearest` opt-in applies. A selected node inside a collapsed parent stays selected and
   hidden (decided), and the tree does not move the selection. To select a node from code, the tree
   expands its ancestors using a private node-to-parent map built while flattening. In `Single` mode
   with several flags set, keep the first selected node in tree order, as `ListBox` does.
8. Add `src/ZurfurGui/Controls/TreeView.Control.cs`:
   - Flatten the visible nodes (roots, then children of expanded nodes) into a list of rows.
   - Use the shared collection synchronizer once per child collection, with reset and rebuild.
   - Subscribe to `PropertyChanged` (`IsExpanded`, `IsSelected`) on nodes as they enter or leave the
	 flattened list, and unsubscribe everything on detach.
   - Create node controllers through the same single item-creation method used by the list box.
   - Build each row as indent by depth, then the expander (hidden for leaves), then the node content.
   - Handle click, expander click, and keyboard navigation from the table above.
   - Selection lives in node data, so rebuilds preserve it automatically. In `Single` mode a click
	 clears the old node's flag and sets the new one, and invalid flag combinations are repaired.
9. Add `FormTreeViewTest` to the test app:
   - A tree with a few levels of text nodes, some expanded in data.
   - Buttons to add a child, remove the selected node, expand all, and collapse all.
   - A label showing the selected node.
   - A custom node renderer with an icon, written the way a user would (see the custom nodes
     section in step 12).
10. Build and manually test in Windows and Browser: expand and collapse by click and keyboard,
   add and remove nodes at several depths, remove the selected node and its ancestors, empty tree,
   deep nesting, and collapsing a parent of the selected node.
11. Add `docs/tree-view.md` modeled on `docs/combo-box.md`, and link it from `docs/architecture.md`.
12. In `docs/tree-view.md`, include a "Creating your own tree nodes" section. It must show a
    complete copy-paste `.zui` and state the rules the author must follow:
    - `$implements` is `ITreeNode<` plus the node's own data class name (the controller name plus
      `Data`), for example `ITreeNode<TreeNodeBadgeData>`.
    - `$data` must declare `children` as `[]` plus the node's own controller name, and `isExpanded`
      and `isSelected` as bools, all with `bind: "data"`.
    - Add `$use` for the node's namespace in the form's `.zui` when it differs.
    - Use it as `TreeView<TreeNodeBadge>`.
    - A missing member or a misspelled name shows up as a C# error that the interface is not
      implemented. Do not set a background on the node, because the row draws hover and selected
      colors.

### Open questions

- **How does `TreeView` read children?** Resolved: `TreeView<Item> where Item : ITreeNode<Item>`, so
  `Item.Children` is typed and needs no cast. Change notification uses the `INotifyCollectionChanged`
  that `ObservableCollection<Item>` implements. Remaining check is the prototype in step 3.
- **Keyboard input.** Confirm that controls can receive focus and key events on both hosts. If not,
  keyboard navigation moves to a later step, and click selection is the first milestone.
- **Selected row highlight.** Confirm how a row view sets a background color for the highlight, and
  which theme color to use.
- **Scrolling.** Confirm the existing scroll viewer can host a tall stack that changes size when
  rows are rebuilt.
- **Test form registration.** Confirm where the test app lists its forms (for `FormTreeViewTest`).
- **Collapsing a parent that contains the selected node.** Decided: the selection stays on the
  hidden node and is not moved. `SelectedNode` and its flag are unchanged.
- Does the icon use an image control that exists today, or does the tree need one first?
- **Which selection property is authoritative.** The `IsSelected` flags in node data. `SelectedNode`
  is the cached last-selected node. If the app sets a flag that breaks the mode (two selected in
  tree order,
    clearing the others' flags.
- **Changing `SelectionMode` at runtime.** Proposal: to `Single` keeps only the cached
  `SelectedNode`, and to `None` clears all flags. Each raises `SelectionChanged` only if the
  selected nodes changed.
- **`SelectionChanged` shape.** Decide whether it is a plain event on the control with no arguments
  (the app reads D) or carries old and new selections. Proposal: no arguments, to stay simple.
- **Re-entrancy.** `SelectNearest` is called from inside `SelectionChanged`, which changes the
  selection again. Proposal: the control raises the nested event normally, and guards its own
  `PropertyChanged` handler against feedback loops.
- **Move and replace in children.** Proposal: replace of a selected node is a remove of that node
  (it raises `SelectionChanged`), and move needs no selection work because flags travel with nodes.
