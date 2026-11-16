using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Controls.ListBox;
using ZurfurGui.Input;
using ZurfurGui.Layout;
using ZurfurGui.Render;

namespace ZurfurGui.Controls.TreeView;

/// <summary>
/// A scrolling tree of nodes.  The node controller is chosen by the concrete node data type, for
/// example <c>TreeView&lt;TreeNodeText&gt;</c>.  Selection and expansion are the
/// <see cref="ITreeNode{TSelf}.IsSelected"/> and <see cref="ITreeNode{TSelf}.IsExpanded"/> flags in
/// the node data.  The tree hooks every node (including collapsed ones) and reacts when a flag
/// changes, and it writes the same flags when the user clicks.  Views exist only for the visible
/// nodes.  Each node is a header row above a stack holding its child nodes while expanded.
/// </summary>
public sealed partial class TreeView<Item> : Controllable, Layoutable
    where Item : ITreeNode<Item>
{
    // Hook and view state for one node, or for the root (whose Parent is null and Depth is -1).
    sealed class NodeState
    {
        public Item Node = default!;
        public NodeState? Parent;
        public int Depth;
        public bool IsLast = true;
        public ObservableCollection<Item> Collection = null!;
        public List<NodeState> Children = new();
        public TreeNodeView? View;
        public PropertyChangedEventHandler? PropertyHandler;
        public NotifyCollectionChangedEventHandler? CollectionHandler;
    }

    // Every hooked node, by reference
    readonly Dictionary<object, NodeState> _states = new(ReferenceEqualityComparer.Instance);

    // The root state while attached, otherwise null
    NodeState? _root;

    // The nodes whose IsSelected flag is set, by reference.  This is a cache of the flags, kept so
    // a click can clear the old selection without walking the data.  Only tracked while attached.
    readonly HashSet<object> _selectedNodes = new(ReferenceEqualityComparer.Instance);

    // The node where a Shift+click range starts: the last node clicked without Shift
    object? _anchor;

    // True while the tree is writing selection flags, so those writes are not treated as input
    bool _repairing;

    // Set when the selected set changes, until SelectionChanged is raised for it
    bool _selectionDirty;

    // The drawing style shared by every gutter.  It is read from the styled properties of the tree
    // (see RefreshGutterStyle) and replaced only when a value changes.
    TreeGutterStyle _gutterStyle = new(16, true, true, 1, TreeExpanderStyle.Box);

    /// <summary>
    /// Raised when the set of selected nodes changes: by user input, when app code changes a flag,
    /// when a flagged node is added, and when the selected node is removed.  Read the node data
    /// to find out what is selected.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Walks the data and returns the nodes with <see cref="ITreeNode{TSelf}.IsSelected"/> set, in
    /// tree order, including nodes in collapsed branches.  It is lazy, so it is safe to change the
    /// flags while enumerating.  It works whether or not the tree is attached.
    /// </summary>
    public IEnumerable<Item> SelectedNodes => FindSelected(DataContext.Items);

    /// <summary>
    /// Deselects every node, raising <see cref="SelectionChanged"/> once.
    /// </summary>
    public void ClearSelection()
    {
        if (_root != null)
        {
            ApplySelection([]);
            FlushSelectionChanged();
        }
        else
        {
            foreach (var node in FindSelected(DataContext.Items).ToList())
                node.IsSelected = false;
        }
    }

    /// <summary>
    /// Selects every node, including nodes in collapsed branches, raising
    /// <see cref="SelectionChanged"/> once.  Does nothing unless the selection mode is
    /// <see cref="SelectionMode.Multiple"/> and the tree is attached.
    /// </summary>
    public void SelectAll()
    {
        if (_root == null || DataContext.SelectionMode != SelectionMode.Multiple)
            return;
        ApplySelection(AllNodes().ToList());
        FlushSelectionChanged();
    }

    /// <summary>
    /// Creates the tree view.
    /// </summary>
    public TreeView()
    {
        InitializeControl();
        View.Layout = this;
        DataContext.PropertyChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Called when attached to the visual tree.  Hooks every node and builds the views.
    /// </summary>
    public void OnAttach()
    {
        AttachData();
    }

    /// <summary>
    /// Called when removed from the visual tree.  Unhooks every node and drops the views.
    /// </summary>
    public void OnDetach()
    {
        DetachData();
    }

    // Handles changes to the data: new items or a new selection mode.
    void OnDataContextChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_root == null)
            return;
        var name = e.PropertyName;
        var all = string.IsNullOrEmpty(name);
        if (all || name == "Items")
        {
            var oldSelected = _selectedNodes.ToList();
            DetachData();
            AttachData();
            if (!_selectedNodes.SetEquals(oldSelected))
                RaiseSelectionChanged();
        }
        else if (name == "SelectionMode")
        {
            _anchor = null;
            ResolveFlagged(AllNodes().Where(s => s.Node.IsSelected).ToList());
            FlushSelectionChanged();
        }
    }

    // Hooks the data, applies the selection mode quietly, and builds the views.
    void AttachData()
    {
        var root = new NodeState { Depth = -1, Collection = DataContext.Items };
        root.CollectionHandler = (s, e) => OnCollectionChanged(root, e);
        root.Collection.CollectionChanged += root.CollectionHandler;
        HookChildren(root);
        _root = root;
        _gutterStyle = ReadGutterStyle();
        ResolveFlagged(AllNodes().Where(s => s.Node.IsSelected).ToList());
        _selectionDirty = false;
        BuildChildViews(root);
    }

    // Unhooks the data and removes the views.
    void DetachData()
    {
        _rows.View.ClearChildren();
        if (_root != null)
        {
            _root.Collection.CollectionChanged -= _root.CollectionHandler;
            foreach (var child in _root.Children)
                UnhookState(child);
            _root.Children.Clear();
        }
        _root = null;
        _selectedNodes.Clear();
        _anchor = null;
        _selectionDirty = false;
    }

    // Creates and hooks a state for every child of the parent state.
    void HookChildren(NodeState parent)
    {
        parent.Children.Clear();
        foreach (var item in parent.Collection)
        {
            var child = CreateState(item, parent);
            parent.Children.Add(child);
            HookState(child);
        }
        SetIsLast(parent);
    }

    // Creates the state for a node.
    NodeState CreateState(Item node, NodeState parent)
    {
        var state = new NodeState
        {
            Node = node,
            Parent = parent,
            Depth = parent.Depth + 1,
            Collection = node.Children
        };
        state.PropertyHandler = (s, e) => OnNodePropertyChanged(state, e);
        state.CollectionHandler = (s, e) => OnCollectionChanged(state, e);
        return state;
    }

    // Subscribes to a node and every node below it.
    void HookState(NodeState state)
    {
        _states[state.Node!] = state;
        state.Node.PropertyChanged += state.PropertyHandler;
        state.Collection.CollectionChanged += state.CollectionHandler;
        HookChildren(state);
    }

    // Unsubscribes from a node and every node below it.
    void UnhookState(NodeState state)
    {
        foreach (var child in state.Children)
            UnhookState(child);
        state.Node.PropertyChanged -= state.PropertyHandler;
        state.Collection.CollectionChanged -= state.CollectionHandler;
        _states.Remove(state.Node!);
        state.View = null;
        if (_selectedNodes.Remove(state.Node!))
            _selectionDirty = true;
        if (ReferenceEquals(_anchor, state.Node))
            _anchor = null;
    }

    // Updates the IsLast flag of every child of the state.
    static void SetIsLast(NodeState state)
    {
        for (int i = 0; i < state.Children.Count; i++)
            state.Children[i].IsLast = i == state.Children.Count - 1;
    }

    // Handles a flag change on a node.
    void OnNodePropertyChanged(NodeState state, PropertyChangedEventArgs e)
    {
        var all = string.IsNullOrEmpty(e.PropertyName);
        if (all || e.PropertyName == "IsSelected")
            OnSelectedFlagChanged(state);
        if (all || e.PropertyName == "IsExpanded")
            OnExpandedFlagChanged(state);
    }

    // Applies the selection mode rules to a changed IsSelected flag.
    void OnSelectedFlagChanged(NodeState state)
    {
        if (_repairing)
            return;
        var node = state.Node;
        var mode = DataContext.SelectionMode;
        if (node.IsSelected && mode == SelectionMode.None)
        {
            Repair(() => node.IsSelected = false);
            UpdateHighlight(state);
            return;
        }
        if (node.IsSelected && mode == SelectionMode.Single)
            ApplySelection([state]);
        else
            SyncSelected(state);
        FlushSelectionChanged();
    }

    // Shows or hides the children of a node when its IsExpanded flag changes.
    void OnExpandedFlagChanged(NodeState state)
    {
        if (state.View == null)
            return;
        var stack = state.View.childStack;
        if (state.Node.IsExpanded)
        {
            if (stack.View.Children.Count == 0)
                BuildChildViews(state);
        }
        else
        {
            stack.View.ClearChildren();
            foreach (var child in state.Children)
                ClearViews(child);
        }
        UpdateGutter(state);
    }

    // Synchronizes the states and the views with a change to a child collection.
    void OnCollectionChanged(NodeState parent, NotifyCollectionChangedEventArgs e)
    {
        var oldStates = new Dictionary<object, NodeState>(ReferenceEqualityComparer.Instance);
        foreach (var child in parent.Children)
            oldStates[child.Node!] = child;

        var newStates = new List<NodeState>();
        var added = new List<NodeState>();
        foreach (var item in parent.Collection)
        {
            if (oldStates.Remove(item!, out var existing))
            {
                newStates.Add(existing);
                continue;
            }
            var child = CreateState(item, parent);
            newStates.Add(child);
            added.Add(child);
        }

        foreach (var removed in oldStates.Values)
            UnhookState(removed);
        parent.Children = newStates;
        SetIsLast(parent);
        foreach (var child in added)
            HookState(child);

        var flagged = new List<NodeState>();
        foreach (var child in added)
        {
            if (child.Node.IsSelected)
                flagged.Add(child);
            flagged.AddRange(Walk(child).Where(s => s.Node.IsSelected));
        }
        if (flagged.Count > 0)
            ResolveFlagged(flagged);

        RebuildStack(parent);
        FlushSelectionChanged();
    }

    // Re-creates the child views of a state whose children changed, reusing existing views.
    void RebuildStack(NodeState parent)
    {
        if (parent.Parent != null && parent.View == null)
            return;
        if (parent.Parent != null && !parent.Node.IsExpanded)
        {
            UpdateGutter(parent);
            return;
        }
        StackOf(parent).View.ClearChildren();
        BuildChildViews(parent);
        if (parent.Parent != null)
            UpdateGutter(parent);
    }

    // Creates (or reuses) the views for the children of a state and adds them to its stack.
    void BuildChildViews(NodeState parent)
    {
        var stack = StackOf(parent);
        foreach (var child in parent.Children)
        {
            if (child.View == null)
                CreateNodeView(child);
            stack.View.AddChild(child.View!.View);
        }
        RefreshGutters(parent);
    }

    // Creates the views of a node, and of its children if it is expanded.
    void CreateNodeView(NodeState state)
    {
        var view = new TreeNodeView();
        state.View = view;
        var header = view.header;
        header.View.AddChild(CreateItemController(state.Node).View);
        header.View.AddGestureRecognizer(new ClickRecognizer((s, e) => OnRowClick(state, e)));
        header.IsSelected = state.Node.IsSelected;
        if (state.Node.IsExpanded)
            BuildChildViews(state);
    }

    // The single place where a node data object becomes a node controller.
    Controllable CreateItemController(Item itemData)
    {
        return global::ZurfurGui.Loader.CreateDataController(itemData);
    }

    // Forgets the views of a node and everything below it.
    void ClearViews(NodeState state)
    {
        state.View = null;
        foreach (var child in state.Children)
            ClearViews(child);
    }

    // The panel holding the child views of a state.
    Panel StackOf(NodeState state)
    {
        return state.Parent == null ? _rows : state.View!.childStack;
    }

    // Handles a click on a row: the expander toggles the node, anywhere else selects it.
    void OnRowClick(NodeState state, PointerEvent e)
    {
        if (state.View == null)
            return;
        if (state.View.header.gutter.IsExpanderHit(e.Contact.DevicePosition))
            state.Node.IsExpanded = !state.Node.IsExpanded;
        else
            SelectFromClick(state, e);
    }

    // Selects according to the selection mode and the Ctrl and Shift keys held during the click.
    void SelectFromClick(NodeState state, PointerEvent e)
    {
        var mode = DataContext.SelectionMode;
        if (mode == SelectionMode.None)
            return;

        var modifiers = e.Contact.Modifiers;
        var isShift = (modifiers & PointerModifiers.Shift) != 0;
        var isControl = (modifiers & PointerModifiers.Control) != 0
            || e.Contact.Device == PointerDeviceKind.Touch;

        if (_selectedNodes.Count == 1 && _selectedNodes.Contains(state.Node!))
        {
            _anchor = null;
            ApplySelection(Array.Empty<NodeState>());
            FlushSelectionChanged();
            return;
        }

        if (mode == SelectionMode.Multiple && isShift && SelectRange(state))
            return;

        _anchor = state.Node;
        if (mode == SelectionMode.Multiple && isControl)
        {
            state.Node.IsSelected = !state.Node.IsSelected;
            return;
        }
        ApplySelection([state]);
        FlushSelectionChanged();
    }

    // Selects the visible nodes from the anchor to the clicked node, replacing the selection.  The
    // anchor does not move.  If the anchor is gone or hidden, the nearest selected visible node is
    // used instead.  Returns false if there is nothing to start from.
    bool SelectRange(NodeState clicked)
    {
        var visible = Walk(_root!, true).ToList();
        var clickedIndex = visible.IndexOf(clicked);
        if (clickedIndex < 0)
            return false;

        var anchorIndex = -1;
        if (_anchor != null && _states.TryGetValue(_anchor, out var anchorState))
            anchorIndex = visible.IndexOf(anchorState);
        if (anchorIndex < 0)
            anchorIndex = FindNearestSelected(visible, clickedIndex);
        if (anchorIndex < 0)
            return false;

        _anchor = visible[anchorIndex].Node;
        var first = Math.Min(anchorIndex, clickedIndex);
        var last = Math.Max(anchorIndex, clickedIndex);
        ApplySelection(visible.GetRange(first, last - first + 1));
        FlushSelectionChanged();
        return true;
    }

    // The index of the selected node closest to the start index, or -1 if none is selected.
    int FindNearestSelected(List<NodeState> nodes, int start)
    {
        for (int distance = 0; distance < nodes.Count; distance++)
        {
            var before = start - distance;
            if (before >= 0 && _selectedNodes.Contains(nodes[before].Node!))
                return before;
            var after = start + distance;
            if (after < nodes.Count && _selectedNodes.Contains(nodes[after].Node!))
                return after;
        }
        return -1;
    }

    // Makes the flagged nodes follow the selection mode.  Multiple keeps them all.  In Single mode
    // an existing selection wins, otherwise the first flagged node wins.  None keeps nothing.  The
    // caller raises SelectionChanged.
    void ResolveFlagged(List<NodeState> flagged)
    {
        var mode = DataContext.SelectionMode;
        if (mode == SelectionMode.Multiple)
        {
            foreach (var state in flagged)
                SyncSelected(state);
            return;
        }

        NodeState? keep = null;
        if (mode == SelectionMode.Single)
        {
            if (_selectedNodes.Count > 0)
                keep = AllNodes().FirstOrDefault(s => _selectedNodes.Contains(s.Node!));
            keep ??= flagged.FirstOrDefault();
        }

        Repair(() =>
        {
            foreach (var state in flagged)
                if (state != keep)
                    state.Node.IsSelected = false;
        });
        foreach (var state in flagged)
            UpdateHighlight(state);
        ApplySelection(keep == null ? Array.Empty<NodeState>() : new[] { keep });
    }

    // Makes the wanted nodes the selection by writing the flags.  The caller raises
    // SelectionChanged.
    void ApplySelection(IReadOnlyCollection<NodeState> wanted)
    {
        var keep = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var state in wanted)
            keep.Add(state.Node!);

        Repair(() =>
        {
            foreach (var node in _selectedNodes.ToList())
            {
                if (keep.Contains(node) || !_states.TryGetValue(node, out var old))
                    continue;
                old.Node.IsSelected = false;
                SyncSelected(old);
            }
            foreach (var state in wanted)
            {
                state.Node.IsSelected = true;
                SyncSelected(state);
            }
        });
    }

    // Makes the selected set and the highlight follow the flag of a node.
    void SyncSelected(NodeState state)
    {
        var node = state.Node!;
        var changed = state.Node.IsSelected ? _selectedNodes.Add(node) : _selectedNodes.Remove(node);
        if (changed)
            _selectionDirty = true;
        UpdateHighlight(state);
    }

    // Raises SelectionChanged if the selected set changed since the last time.
    void FlushSelectionChanged()
    {
        if (!_selectionDirty)
            return;
        _selectionDirty = false;
        RaiseSelectionChanged();
    }

    // Runs an action that writes selection flags without those writes being treated as input.
    void Repair(Action action)
    {
        _repairing = true;
        try
        {
            action();
        }
        finally
        {
            _repairing = false;
        }
    }

    // Sets the highlight of a row from the flag in the data.
    static void UpdateHighlight(NodeState state)
    {
        if (state.View != null)
            state.View.header.IsSelected = state.Node.IsSelected;
    }

    // Updates the gutter of every visible node below the state.
    void RefreshGutters(NodeState state)
    {
        foreach (var child in state.Children)
        {
            if (child.View == null)
                continue;
            UpdateGutter(child);
            RefreshGutters(child);
        }
    }

    // Tells the gutter of a node what to draw.
    void UpdateGutter(NodeState state)
    {
        if (state.View == null)
            return;
        var isLast = new bool[state.Depth + 1];
        var s = state;
        for (int i = state.Depth; i >= 0 && s != null; i--, s = s.Parent)
            isLast[i] = s.IsLast;

        state.View.header.gutter.SetState(
            isLast, state.Node.Children.Count > 0, state.Node.IsExpanded, _gutterStyle);
    }

    // Reads the current drawing style from the styled properties of the tree.
    TreeGutterStyle ReadGutterStyle()
    {
        var style = Properties;
        return new TreeGutterStyle(style.IndentWidth, style.ShowLines, style.ShowExpander,
            style.LineWidth, style.ExpanderStyle);
    }

    // Passes a changed drawing style on to every gutter.  Does nothing if the style is the same.
    void RefreshGutterStyle()
    {
        var style = ReadGutterStyle();
        if (style == _gutterStyle)
            return;
        _gutterStyle = style;
        if (_root != null)
            RefreshGutters(_root);
    }

    // Measures the tree like a plain panel, but first checks whether the drawing style changed.
    // (The layout type name comes from the generated TypeName.)
    //
    // This is a workaround for how style changes are detected.  The drawing settings (lines,
    // expander, indent width) are styled properties of the tree, but the gutters that draw them are
    // deep in the view tree.  When a theme or style changes, the style system refreshes its cached
    // values and flags only the view that owns the property, which is this tree, and never calls
    // any code.  The gutters hold a shared copy of the style, so nothing would redraw or resize
    // them.  The properties have the measure flag, so a change flags this view and brings us here
    // before the children are measured.  Passing the new style to the gutters flags them for
    // redraw and gives them their new width, and they are then measured in this same frame.
    // The alternative is for each gutter to own every styled property, which would stop the tree
    // from applying a local setting such as expanderStyle to all of its gutters.
    Size Layoutable.MeasureView(View view, MeasureContext measure, Size available)
    {
        RefreshGutterStyle();
        return view.MeasurePanel(measure, available);
    }

    // Arranges the children like a plain panel.  The tree only needs the measure hook above.
    void Layoutable.ArrangeViews(View view, MeasureContext measure)
    {
        view.ArrangePanel(measure);
    }

    // Every hooked node in tree order.
    IEnumerable<NodeState> AllNodes()
    {
        return _root == null ? [] : Walk(_root);
    }

    // The nodes below a state in tree order, not including the state.  If visibleOnly is set,
    // the children of collapsed nodes are skipped.
    static IEnumerable<NodeState> Walk(NodeState state, bool visibleOnly = false)
    {
        foreach (var child in state.Children)
        {
            yield return child;
            if (visibleOnly && !child.Node.IsExpanded)
                continue;
            foreach (var descendant in Walk(child, visibleOnly))
                yield return descendant;
        }
    }

    // The selected nodes in a collection and below it, in tree order.
    static IEnumerable<Item> FindSelected(IEnumerable<Item> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.IsSelected)
                yield return node;
            foreach (var descendant in FindSelected(node.Children))
                yield return descendant;
        }
    }

    // Raises the SelectionChanged event.
    void RaiseSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
