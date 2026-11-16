# TreeView

## Overview

`TreeView<Item>` shows a scrolling tree of nodes. Each node has text (and optionally an icon or other
content), can have child nodes, and can be expanded, collapsed, and selected. The node controller is
chosen by the concrete node data type, for example `TreeView<TreeNodeText>`.

The tree draws the connecting lines and the expander (a box with `+` and `-`, or a chevron) itself.
Selection and expansion are plain flags in the node data, so the app can read and change them
without calling the tree.

The files are in `src/ZurfurGui/Controls/TreeView`, in the `ZurfurGui.Controls.TreeView` namespace.
The test form is `FormTestTreeView` in the test app.

| File | Purpose |
|------|---------|
| `TreeView.zui`, `TreeView.Control.cs` | The tree control. |
| `TreeNode.cs` | The `ITreeNode<TSelf>` data contract every node must satisfy. |
| `TreeNodeText.zui` | The built-in node: an optional icon and text. |
| `TreeNodeView.zui` | Internal. One node: a header row above a stack of child nodes. |
| `TreeNodeRow.zui` | Internal. The header, which draws the hover and selected background. |
| `TreeGutter.zui`, `TreeGutter.Control.cs` | Internal. Draws the lines and the expander. |

## Using the tree

### Building the data

Add root nodes to `DataContext.Items` and child nodes to each node's `Children`:

```csharp
var node = new TreeNodeTextData() { Text = new TextLines("Parent"), IsExpanded = true };
node.Children.Add(new TreeNodeTextData() { Text = new TextLines("Child") });
tree.DataContext.Items.Add(node);
```

The collections are `ObservableCollection`, so the tree updates when nodes are added, removed,
replaced, moved, or cleared at any depth.

### Declaring the tree in a `.zui` file

Include `ZurfurGui.Controls.TreeView` and `ZurfurGui.Controls.ListBox` in the form's `$use` list.
The tree namespace provides the control and node contract; the list-box namespace provides the
shared `SelectionMode` enum.

```
{ $controller: "TreeView<TreeNodeText>", selectionMode: "single", showLines: false }
```

`selectionMode` is data. The other settings are styles, so a theme can change them for every tree,
and a value in the `.zui` file or in code applies to that one tree only. The defaults are theme
variables in `ZurfurBase.zth`.

| Setting | Theme variable | Default | Meaning |
|---------|----------------|---------|---------|
| `selectionMode` | (data) | single | `none`, `single`, or `multiple`. |
| `showLines` | `treeview.show-lines` | true | Draw the lines connecting nodes. |
| `showExpander` | `treeview.show-expander` | true | Draw the expander of nodes that have children. |
| `indentWidth` | `treeview.indent-width` | 16 | Width of each depth level. |
| `lineWidth` | `treeview.line-width` | 1 | Thickness of the lines and the expander. |
| `expanderStyle` | `treeview.expander-style` | box | `box` (plus and minus) or `chevron`. |

The lines are always solid. The line color is the theme variable `color.treeview.line`. The row
background is `color.treeview.row.background`, which uses the condition `TreeNodeRow.isSelected`.

### Selecting and expanding

- Click a row to select its node. Click the expander to expand or collapse it. Clicking the
  expander does not change the selection.
- Clicking the sole selected node clears the selection in either selection mode. Otherwise, a row
  click selects that node.
- In `multiple` mode, Ctrl+click and touch click toggle a node, and Shift+click selects the visible
  nodes from the last node clicked without Shift to the clicked node, replacing the selection. Shift
  wins over Ctrl. If the start node is gone or hidden, the selected visible node nearest to the click
  is the start. Nodes inside collapsed branches are skipped by the range. A plain click or a Shift
  range also deselects selected nodes that are hidden in collapsed branches. In `single` and `none`
  mode the modifier keys are ignored.
- To change the state from code, write the flag: `node.IsSelected = true` or
  `node.IsExpanded = false`. There is no `Select` or `Expand` method. The tree writes the same
  flags when the user clicks.
- `SelectedNodes` walks the data and returns every node whose flag is set, in tree order,
  including nodes in collapsed branches. It is lazy, so flags may be changed while enumerating, and
  it works while the tree is detached.
- `ClearSelection()` deselects every node and `SelectAll()` selects every node (only in `multiple`
  mode, while attached). Each raises `SelectionChanged` once.
- `SelectionChanged` is raised when the selected set changes. It has no arguments, so read the
  data. It is raised for user input, for flag changes made by app code, when a flagged node is
  added, and when a selected node is removed. A click, `ClearSelection`, and `SelectAll` raise it
  once however many flags they write, but flags written by app code raise it once per flag.
- Selecting a node does not expand its ancestors or scroll to it. An app that wants the selected
  node to be visible sets `IsExpanded` on the ancestors itself.
- Collapsing a parent of the selected node leaves the selection on the hidden node.
- Removing the selected node de-selects it. No neighbor is selected. An app that wants one can
  select it in `SelectionChanged`.

### Creating your own tree nodes

Copy this `.zui` and change the names and the content. The test app has a complete example in
`Controls/TreeNodeBadge.zui`.

```
{
    $controller: "MyNode",
    $namespace: "MyApp",
    $use: ["ZurfurGui.Controls.TreeView"],
    $implements: "ITreeNode<MyNodeData>",
    $data: {
        children: { type: "[]MyNode", bind: "data" },
        isExpanded: { type: "Bool", bind: "data" },
        isSelected: { type: "Bool", bind: "data" },
        text: { type: "TextLines", bind: "data:_text.text" }
    },
    $content: [
        { $name: "_text", $controller: "TextView" }
    ]
}
```

Rules:

- `$implements` is `ITreeNode<` plus the node's own data class name (the controller name plus
  `Data`), for example `ITreeNode<MyNodeData>`.
- `$data` must declare `children` as `[]` plus the node's own controller name, and `isExpanded` and
  `isSelected` as bools, all with `bind: "data"`.
- Add `$use` for the node's namespace in the form's `.zui` when it differs.
- Use it as `TreeView<MyNode>`.
- A missing member or a misspelled name shows up as an ordinary C# error that the interface is not
  implemented.
- Do not set a background on the node, because the row draws the hover and selected colors.
- A node object must appear once, in one tree, and the data must not contain cycles. These are not
  detected.

<!-- INTERNAL -->

## Implementation details

### The node contract

`ITreeNode<TSelf>` declares `Children`, `IsExpanded`, and `IsSelected`, with
`where TSelf : ITreeNode<TSelf>`. `TreeView<Item>` is declared
`where Item : ITreeNode<Item>`, so `node.Children` is already typed and needs no cast. The code
generator knows nothing about trees. The node author supplies the members with ordinary `$data`
fields and `$implements`, and the generated properties satisfy the interface. The only generator
change is that the `where` constraint in `$controller` may be a generic type.

### Views follow the data

Each node is a `TreeNodeView`: a `TreeNodeRow` header above a child stack. The header holds a
`TreeGutter` (lines and expander) followed by the node content, which is created by
`Loader.CreateDataController` from the concrete data type. The child stack is filled only while the
node is expanded. Expanding creates the child views, and collapsing destroys them, so a large
collapsed branch costs no views.

The tree keeps a small private state object per node: the node, its depth, whether it is the last
child, its list of child states, its view (if any), and its event handlers. This is the "hook".

### Hooks cover every node

The tree subscribes to `PropertyChanged` and `Children.CollectionChanged` of every node, including
nodes in collapsed branches that have no views. That way a flag change anywhere in the data reaches
the tree. Hooking is done in `OnAttach` and undone in `OnDetach`, so the tree does not keep
data alive after it leaves the window. Changes made while detached are picked up by the next attach.

When a child collection changes, the tree compares the new collection with its list of child states.
Nodes that left are unhooked (with everything below them), nodes that arrived are hooked, and the
child stack is rebuilt from the states, reusing the views that already exist. This handles add,
remove, replace, move, and reset with one code path, and because it compares against its own
snapshot it works for `Reset`, which does not say which items were removed.

### Selection and expansion

The flags in the node data are the single source of truth. The tree reacts to them in one place,
the `PropertyChanged` handler of each node, and it writes the same flags when the user clicks. A
click therefore takes exactly the same path as app code setting a flag.

- **Expanded.** When `IsExpanded` changes, the tree creates or destroys the child views of that
  node and redraws its expander.
- **Selected, `Multiple` mode.** A flag turning true or false only updates the selected set and the
  row highlight. Nothing else is cleared.
- **Selected, `Single` mode.** When a flag becomes true, the flags of the other selected nodes are
  cleared. Last writer wins.
- **Selected, `None` mode.** A flag set to true is cleared again.
- **Several flags at once.** On attach, when a subtree containing flagged nodes is added, or when
  the mode changes, `Multiple` keeps them all. `Single` keeps an existing selection, otherwise the
  first flagged node in tree order, and clears the others. `None` clears them all. Changing the
  mode also forgets the Shift anchor.
- **Re-entrancy.** The tree writes flags while repairing, which fires `PropertyChanged` again. A
  guard makes those writes ignored, so the repair code updates the selected set itself, and one
  `SelectionChanged` is raised after the repair.
- **Removal.** Removing a subtree unhooks every node in it, removes them from the selected set, and
  raises `SelectionChanged` if any of them was selected. The removed nodes keep their flags, so the
  tree never writes into data the app has removed.

The selected set is a private cache of the flags (a hash set using reference equality, only tracked
while attached). It lets a click clear the old selection without walking every node. The flags stay
the source of truth, and `SelectedNodes` is a walk over the app's data, so it never depends on the
cache. The Shift anchor is a private reference to the last node clicked without Shift.

### Why `IsSelected` is in the node data

In WPF, `TreeViewItem` has `IsSelected` and `IsExpanded` properties on the control, and a tree view
model usually copies them into its own properties through bindings so that state can be saved,
restored, and set from code. WinForms keeps selection on the control (`SelectedNode`). Qt, Avalonia,
and WinUI keep it in a selection model owned by the view. While control-owned selection is the purer
design for a generic library, this library puts the flags in the node data (`D`) instead, for these
reasons:

- **It is already what apps end up doing.** WPF apps nearly always bind the flags to their view
  model, so the data is the real owner anyway.
- **Selection is plain data.** It can be set in a `.zui` file or deserialized from JSON, it is
  observable through `PropertyChanged`, and it is saved and restored with the data.
- **No cleanup.** A removed node takes its flag with it. A control-owned set would be the source of
  truth, would need removal of whole subtrees from it, and would need a query such as
  `tree.IsSelected(node)`. The private cache here is rebuilt from the flags on attach.
- **The app can walk its own data.** `if (node.IsSelected)` works, and multi-select and Shift ranges
  are simple operations on flags.
- **It fits the model-data-view split.** `D` is view-shaped data generated from what the view
  declares, so `isSelected` and `isExpanded` are legitimate there. A bool does not reference a
  control, so the rule that data never references controls still holds. The worry that UI state
  leaks into the domain model applies only if `D` is reused as the model `M`, which is optional. An
  app that needs a separate model keeps its own and maps it to a custom `D`, so the UI flags never
  enter `M`.

The costs are accepted: the node author must declare the three members (the compiler tells them if
one is missing), the same node objects shown in two trees share selection and expansion, and the
tree must repair invalid combinations such as two selected nodes in `Single` mode. If sharing one
data set across several trees becomes a real need, a tree-owned set can be added later as an
alternative mode.

### Drawing

The tree draws the lines and the expander itself instead of using external controls, because only
the tree knows the depth and sibling information they need. Each gutter is told its depth and an
`isLast` flag for the node and each ancestor. It draws a vertical pass-through line for each ancestor
that has later siblings, and for its own level a line that stops halfway on the last child, plus a
horizontal stub to the content. Rows touch with no gaps, so the vertical lines join across the
nested rows. The tree's columns take their spacing from the theme variable `treeview.row-spacing`
(default 0), and the node controls provide the vertical breathing room with their own padding,
as `TreeNodeText` does. A custom node control should do the same. If a theme sets the variable
above 0, the lines no longer join. The gutter is not hit-testable, so clicks go to the row, and the tree asks the gutter
whether the click was over the expander.

The tree reads the style settings (`showLines`, `showExpander`, `indentWidth`, `lineWidth`, and
`expanderStyle`) from its own styled properties and gives every gutter one shared, immutable style
object. The gutters do not look up styles themselves, so a setting made on one tree applies to all
of that tree's gutters. The style system flags only the view that owns a changed style, which is
the tree and not its gutters, so the tree implements `Layoutable` and checks for a style change at
the start of its measure. If the style changed, it gives the gutters the new style, and they
redraw and resize in the same frame. The style properties use the measure flag for this reason.

### Limits

- Every node is hooked, so cost and memory grow with the size of all the data, not just the visible
  part. Lazy loading is out of scope, but an app can use `IsExpanded` to trigger its own loading.
- There is no virtualization, because the views are nested rather than flattened.
- There is no keyboard navigation, because the library has no keyboard focus yet.
- Collection changes rebuild the affected child stack rather than doing a minimal diff.

## Future

### Ideas

- Keyboard navigation (Up, Down, Home, End, Left, Right) once the library has keyboard focus. It
  would walk the visible nodes in the data, using the same enumeration as Shift ranges.
- Diffing child collection changes instead of rebuilding the stack, shared with the list box.
- An icon image control, when one exists.
- Virtualization, which would need a flattened list of visible nodes.