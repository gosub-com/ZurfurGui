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
- Selection was owned by the container (`SelectedIndex`). It is now a per-item `IsSelected`.

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
- **Selection is a per-item `IsSelected` flag in D, for both `ListBox` and `TreeView`.** The flag is
  declared as a plain `$data` field in the item's `.zui` and required by the item's marker
  interface. The controls never store selection themselves. They watch `PropertyChanged` on the
  items and react. See the selection model below.
- **One marker interface per container** (`IComboBoxItem`, `IListBoxItem`, `ITreeNode`), kept
  separate (see below).
- **App-level control replacement (checkboxes, etc.) is out of scope.** Plain controls such as
  `CheckBox` do not use the item machinery. The by-name `$controller` registration is enough if this is
  revisited, so nothing is decided now.
- **Tree owns layout rows, and the views are nested rather than flattened.** The tree wraps each
  node renderer in a row: a header (indent by depth, expander, then the node content) above a
  child stack that exists only while the node is expanded. The item renderer only draws content.
  Selection and expansion come from hooks on the data, not from the views, so collapsed branches
  need no views. Keyboard navigation walks the visible nodes in the data. Virtualization would
  need a flattened list later, which is out of scope.
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
  - Costs accepted: the node author must declare the flag, the same data shown in two trees shares
  selection, and the control must repair invalid flag combinations.
  - *Other libraries.* WPF keeps `IsSelected` and `IsExpanded` on the control, but apps nearly
  always bind them to view model properties so state can be saved, restored, and set from code.
  WinForms keeps selection on the control (`SelectedNode`). Qt, Avalonia, and WinUI keep it in a
  view-owned selection model. Control-owned selection is the purer design for a generic library,
  because selection belongs to the (view, node) pair and the control owns the mode policy.
  - *Consistency with MDV.* D is view-shaped data generated from what the view declares, so
  `isSelected` and `isExpanded` in `$data` are legitimate. A bool does not reference a control,
  so the rule that data never references controls holds. The concern that UI state leaks into
  the domain model applies only if D is reused as M, which is optional. An app that needs a
  separate M keeps its own model and maps it to and from a custom D (a partial class of the
  generated data), so UI flags never enter M.
  - *Why not control-owned (a `HashSet` with reference equality plus a row `isSelected`).* It is
  the more generic choice, but it adds the set, cleanup on removal (including whole subtrees),
  a `tree.IsSelected(node)` query instead of `node.IsSelected`, and no way to set selection from
  `.zui` or JSON. Simplicity wins. If sharing one data set across several trees becomes a real
  need, add a tree-owned set as an alternative mode later.
  - *ListBox follows the same model (changed from indices).* The first plan kept `SelectedIndex`
  and `SelectedIndices` in the list box. The generator cannot declare an integer collection in
  `$data`, so the built version kept them in the controller. That meant no initial selection
  from `.zui`, no `PropertyChanged` signal, and index remapping on every insert and remove. A
  per-item flag removes all of that and LINQ replaces the index API. The cost is that item types
  must declare `isSelected` and the same item objects cannot be selected independently in two
  lists.
- **Selection model.** D is the single source of truth for `IsSelected` and `IsExpanded`.
  - Setting or clearing selection, and expanding or collapsing a node, is done by writing the flag
    in D (code, `.zui`, or JSON). There is no `Select(node)`, `ClearSelection()`, or
    `SelectNearest()`. The control writes the same flags when the user clicks, so there is one
    path. Apps that want neighbor selection after a removal do it in `SelectionChanged`.
  - The control hooks `PropertyChanged` on every item, including items in collapsed branches and
    items with no view, so a flag change anywhere in the data reaches the control's
    `SelectionChanged`. The control keeps no selected set of its own, only a private cached
    reference to the selected node (`Single` mode) so it can clear the old flag in O(1).
  - Queries are plain LINQ over the app's own data, such as `list.Items.Where(i => i.IsSelected)`.
    The tree adds `SelectedNode` (cached) and a `SelectedNodes` convenience walk, because a tree
    needs a recursive walk.
  - Hooks follow the data, not the views. Adding a subtree hooks every node in it (a walk), and
    any node already flagged `IsSelected` is treated as a new selection (mode rules below, then
    `SelectionChanged`). Removing a subtree unhooks every node in it (a walk) and, if one was
    selected, clears the cached node and raises `SelectionChanged`. Removed nodes keep their
    flags, so the control never writes into data the app has removed (a remove followed by an
    insert elsewhere keeps the flag).
  - `Reset` gives no list of old items, so the control keeps a private snapshot of the nodes it
    hooked for each child collection and unhooks from that.
  - Mode rules. `None`: a flag set to true is cleared again. `Single`: setting a flag to true
    clears the cached old node's flag (last writer wins). When several flags are found at once
    (attach, or an inserted subtree), an existing selection wins, otherwise the first in tree
    order wins, and the rest are cleared. Repairs run under a re-entrancy guard and raise one
    `SelectionChanged`.
  - Selection never expands or scrolls. An app that wants a selected node visible sets
    `IsExpanded` on its ancestors itself.
- **Rationale for `IsSelected` in node data.** Alternatives considered:
  - *Tree owns a set of node references.* Keeps D pure, but selection cannot be set from JSON or
    `.zui`, is not observable through D, needs a reference set, and removing a node requires
    walking its subtree to drop selected descendants. The app must ask the tree whether a node is
    selected.
  - *Hybrid (tree owns reference, flag is view-only).* Duplicates state for no benefit.
  - *Index path.* Shifts under edits like list box indices, and is harder to maintain.
  - *Flag in D (chosen).* Selection is plain serializable data, can be set from `.zui` or JSON,
    and needs no cleanup on removal because a removed node takes its flag with it. It is consistent
    makes multi-select and Shift ranges simple (the flags are just data), and
    lets the app walk its own data (`if (node.IsSelected)`). WPF developers commonly add
    `IsSelected` and `IsExpanded` to tree view models for the same reason.
  - *Other libraries.* WPF keeps `IsSelected` and `IsExpanded` on the control, but apps nearly
    always bind them to view model properties so state can be saved, restored, and set from code.
    WinForms keeps selection on the control (`SelectedNode`). Qt, Avalonia, and WinUI keep it in a
    view-owned selection model. Control-owned selection is the purer design for a generic library,
    because selection belongs to the (view, node) pair and the control owns the mode policy.
  - *Consistency with MDV.* D is view-shaped data generated from what the view declares, so
    `isSelected` and `isExpanded` in `$data` are legitimate. A bool does not reference a control,
    so the rule that data never references controls holds. The concern that UI state leaks into
    the domain model applies only if D is reused as M, which is optional. An app that needs a
    separate M keeps its own model and maps it to and from a custom D (a partial class of the
    generated data), so UI flags never enter M.
  - *Why not control-owned (a `HashSet` with reference equality plus a row `isSelected`).* It is
    the more generic choice, but it adds the set, cleanup on removal (including whole subtrees),
    a `tree.IsSelected(node)` query instead of `node.IsSelected`, and no way to set selection from
    `.zui` or JSON. Simplicity wins. If sharing one data set across several trees becomes a real
    need, add a tree-owned set as an alternative mode later.
  - Costs accepted: the tree and list box use different models, the same data shown in two trees
    shares selection, the node author must declare the flag, and the tree must repair invalid
    flag combinations.
  - *Why `ListBox` keeps indices:* list items have no identity and an index is a simple handle, and
    the tree's problem (no stable index) does not exist there. Revisit if initial selection from
    `.zui` is wanted for lists. Then add `IsSelected` to `IListBoxItem` and keep the index API as a
    computed view.
- **Selection mode.** `SelectionMode` is `None`, `Single`, or `Multiple`, a field in the control's D.
  `ListBox` version 1 implements all three and `TreeView` version 1 implements only `None` and
  `Single`. In `None` mode clicks do nothing and flags set to true are cleared. In `Multiple` mode
  the list box keeps a private anchor (the last item clicked) for Shift ranges.
- **Removing the selected item de-selects it.** The item is unhooked and `SelectionChanged` is
  raised. No neighbor is selected, and nothing shifts because there are no indices to remap.
- **`SelectionChanged` fires when the set of selected items changes.** It is a plain event on the
  control with no arguments (the app reads D). It fires on user input, when app code flips a flag,
  when a flagged item is added, and when a selected item is removed or the list is reset. A repair
  that flips several flags raises it once.
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

## ListBox implementation details

This section records how the list box port went, before the move to per-item `IsSelected`
(section 3). Index, `Select`, and `SelectNearest` details below are historical. The behavior is
documented in
[list-box.md](list-box.md).

### Overview

The list box was built close to the plan, modeled on `ComboBox<Item>`. New files in
`src/ZurfurGui/Controls/ListBox`: `ListBox.zui`, `ListBox.Control.cs`, `ListBoxRow.zui`,
`ListBoxRow.Control.cs`, `ListBoxItem.cs`, and `ListBoxItemText.zui`. Each theme got a
`color.listbox.item.background` variable. The test form is `FormTestListBox` in the test app, with a
custom `ListBoxItemBadge` item. All of it builds. It was not run, so manual testing is still
needed.

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

- Primitive collection support in the generator is not needed, because selection (list box and
  tree) is a bool flag in item data.
- Extract the collection synchronizer and row building from `ListBox` when the tree needs them.
  The index remapping goes away with the rework below.


## 4. Plan: TreeView

### What the tree adds beyond the list box

| Area | ListBox | TreeView |
|------|---------|----------|
| Item shape | Flat `Items` | Each node has `Children` of its own type, plus `IsExpanded` |
| Marker interface | `IListBoxItem`, requires `IsSelected` | `ITreeNode<TSelf>`, requires `Children`, `IsExpanded`, and `IsSelected` |
| Generated data | `Items` typed to the item | Node data class with `Children` typed to itself, declared by the author in `$data` (`[]MyNode`) |
| Rows | One row per item | One row per visible node: a header (indent by depth, expander, content) above a child stack while expanded |
| Row source | The items collection | Nested views built from the data, with children created on expand and destroyed on collapse |
| Collection changes | Hook `Items` and every item | Hook `Items` and every node's `Children`, collapsed or not |
| State | `IsSelected` per item in D, `SelectionMode` in D | `IsSelected` and `IsExpanded` per node in D. The control has a cached `SelectedNode` and a computed `SelectedNodes`, nothing else stored |
| Keyboard | Up, Down, Home, End | Also Left (collapse or go to parent), Right (expand or go to child) |
| Click (gesture) selects, expander click (gesture) toggles, optional double-click toggles |
| Leaf handling | None | Leaf is `Children.Count == 0`, expander hidden (width kept) |
| Tree drawing | None | The tree draws the expander and the connecting lines itself |

Not added in the tree (to keep it simple): mixed node types, templates, virtualization (it would
need a flattened list), drag and drop, multi-select (version 1 supports `None` and `Single`), and
lazy-loading children, and named external controls for the expander (deferred, see below).

### Drawing the expander and lines

The tree draws the expander and the lines itself. They are not external controls. The tree owns
the row layout, and only the tree knows the depth and sibling information the lines need. A
property naming an external control would be a second templating mechanism, which this plan
rejects. Named external expander controls are deferred. If they are needed later, the existing
by-name `$controller` registration can be used, so nothing is built for it now.

- **Row header layout.** `[indent area][expander cell][node content]`. The indent area is
  `depth * IndentWidth` wide. The expander cell has a fixed width, so content lines up whether or
  not a node is a leaf.
- **Expander.** A small view the tree creates in each row header. It draws a box with `+` (collapsed)
  or `-` (expanded) using the existing drawing primitives. It is hidden for leaves but still takes
  A click gesture on it (see gestures and scrolling) toggles `IsExpanded` and does not change
    selection.
  `IsExpanded` through the same `PropertyChanged` hook the tree already uses.
- **Lines.** Controlled by `ShowLines`. Each row draws a horizontal stub to its expander or
  content, and a vertical line at its own level that runs to the next sibling only if one exists
  (half height for the last child). For each ancestor level, the row draws a pass-through vertical
  line only if that ancestor has later siblings. The tree builds the rows, so each row is given
  its depth and an `isLast` flag for itself and each ancestor. Rows touch with no gaps, so the
  vertical lines join up across the nested rows. When a sibling is added or removed after the
  last one, the affected rows redraw their lines.
- **Styled properties on `TreeView` (version 1):**
  - `ShowLines` (bool). Draw connecting lines.
  - `ShowExpander` (bool). Draw the expander boxes.
  - `IndentWidth`. Width of each depth level.
  - `LineWidth`. Line thickness.
  - `ExpanderStyle`. Box with `+` and `-`, or a chevron.

  These are `styled` properties whose defaults come from `ZurfurBase.zth`. The line color is a
  styled property of the gutter. Lines are always solid. Add more styling later only if needed.
- **Pointer and keyboard.** Expander click toggles. Content click selects. Right expands, or
  moves to the first child if already expanded. Left collapses, or moves to the parent if
  already collapsed or a leaf.

### Gestures and scrolling

- **Clicks use the existing `ClickRecognizer`** (`View.AddGestureRecognizer`), as `Button`,
  `ComboBox`, and `ListBox` do, as-is. The tree does not know or care whether it is inside another
  scroll viewer. The expander has its own recognizer, so an expander click does not also select.
- **The tree hosts a `ScrollViewer`.** `TreeView.zui` declares a `ScrollViewer` as its content,
  and the row stack is the scroll viewer's content. The tree does no scrolling itself. Expose only
  what is needed in v1 (such as `scrollMode`).

### Steps

1. Review the list box
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
5. Add `src/ZurfurGui/Controls/TreeView/TreeNode.cs` with `ITreeNode<TSelf>` (it is not empty, unlike the other
   item interfaces) and its XML comments.
6. Finish `src/ZurfurGui/Controls/TreeView/TreeNodeText.zui`, the built-in node renderer. Declare
   `$implements: "ITreeNode<TreeNodeTextData>"` and `$data` fields `children` (`[]TreeNodeText`),
   `isExpanded`, `isSelected` (all `bind: "data"`), `text`, and an optional `icon` (`TextLines`).
7. Add `src/ZurfurGui/Controls/TreeView/TreeView.zui`: `$controller: "TreeView<Item> where Item :
   ITreeNode<Item>"`, with `Items` (`[]Item`, root nodes) and `SelectionMode` (`None` or
   `Single`). Selection and expansion are the `IsSelected` and `IsExpanded` flags in node data,
   and D is the only place they are set (see the selection model). The control exposes a cached
   `SelectedNode`, a computed `SelectedNodes`, and a `SelectionChanged` event. There is no
   `Select`, `ClearSelection`, or `SelectNearest`. A selected node inside a collapsed parent stays
   selected and hidden (decided).
8. Add `src/ZurfurGui/Controls/TreeView/TreeView.Control.cs`:
   - Hooks follow the data. Keep one small private hook object per node: the node, its depth, a
   snapshot of its children, and its handlers. Hooking a node subscribes to its
   `PropertyChanged` (`IsExpanded`, `IsSelected`) and its `Children` collection changes, then
   hooks its children. Unhooking walks the snapshot, so `Reset` is handled.
   - Use the shared collection synchronizer once per child collection. Add hooks the new subtree
   and applies the mode rules to any flagged nodes. Remove unhooks the subtree and raises
   `SelectionChanged` if a selected node left. Replace is remove plus add. Move reorders the
   child views only.
   - Build views from the data. A node row is a header (indent by depth, lines, expander hidden
   for leaves, node content) above a child stack. The tree draws the lines and expander (see
   drawing the expander and lines), using `ShowLines`, `LineColor`, `LineWidth`,
   `IndentWidth`, and `ExpanderStyle`.
   becomes true and destroyed when it becomes false. Collapsed branches have hooks but no views.
   - Create node controllers through the same single item-creation method used by the list box.
   - Clicks use `ClickRecognizer` on each row's content and expander. The tree's content is a
   `ScrollViewer` holding the row stack.
   - A click writes `IsSelected`
   expander click writes `IsExpanded`. Nothing else changes state. Keyboard navigation walks
   the visible nodes in the data, per the table above.
   - Mode repairs and the cached `SelectedNode` run under a re-entrancy guard, so flag writes made
   during a repair do not trigger further repairs. `SelectionChanged` is raised once after the
   guard is released.
   - Attach and detach. `OnAttach` walks the data, hooks every node, resolves the initial
   selection under the mode rules (without raising `SelectionChanged`), and builds the views.
   `OnDetach` unhooks every node and drops the views and snapshots, so the tree does not leak
   through data that outlives it (Priority 3 in future-improvements.md). Changes made while
   detached are picked up by the next attach walk. This mirrors the list box.
9. Add `FormTreeViewTest` to the test app:
   - A tree with a few levels of text nodes, some expanded in data.
   - Buttons to add a child, remove the selected nodes, expand all, and collapse all.
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
- **Changing `SelectionMode` at runtime.** Proposal: to `Single` keeps only the cached
  `SelectedNode`, and to `None` clears all flags. Each raises `SelectionChanged` only if the
  selected nodes changed.
- **Re-entrancy.** Resolved: repairs run under a guard (see the selection model). A
  `SelectionChanged` handler that writes flags runs after the guard is released, so its writes are
  handled normally and may raise a nested `SelectionChanged`.
- **Move and replace in children.** Replace is remove plus add (a selected node removed raises
  `SelectionChanged`). Move needs no selection work because flags travel with nodes, only the
  child views are reordered.
- **Node identity.** A node object must appear once, in one tree, and the data must not contain
  cycles. A cycle makes the hook walk endless, and a duplicate node would be hooked twice. These
  are not detected in version 1 and should be stated in `docs/tree-view.md`.
- **Shared data.** The same node objects shown in two trees share selection and expansion, and
  each tree hooks them independently. Accepted.
- **Hook cost.** Every node is hooked, including collapsed ones, so cost and memory grow with the
  whole data size rather than the visible size. Acceptable for version 1 and the reason lazy
  loading is out of scope. An app that lazy-loads can still use `IsExpanded` to trigger loading.
- **Removed nodes keep `IsSelected`.** Decided, so the control never writes into data the app has
  removed. If the removed node is inserted again in a `Single` tree that already has a selection,
  the existing selection wins and the inserted flag is cleared.
- **Generator impact.** Adding a node type needs no generator change. The one library-side change
  is loosening the `where` regex in step 2, and step 3 checks the self-referencing collection. The
  `ListBox` rework needs no generator change.