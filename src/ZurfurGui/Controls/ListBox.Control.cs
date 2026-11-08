using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using ZurfurGui.Base;
using ZurfurGui.Input;

namespace ZurfurGui.Controls;

/// <summary>
/// How a <see cref="ListBox{Item}"/> lets the user select items.
/// </summary>
public enum SelectionMode
{
    /// <summary>Clicks do nothing and nothing can be selected.</summary>
    None,

    /// <summary>At most one item is selected.</summary>
    Single,

    /// <summary>
    /// Any number of items are selected.  Click selects one item, Ctrl+click toggles an item, and
    /// Shift+click selects the range from the last item clicked without Shift.
    /// </summary>
    Multiple
}

/// <summary>
/// A scrolling list of items.  The item controller is chosen by the concrete item data type, for
/// example <c>ListBox&lt;ListBoxItemText&gt;</c>.  The selection is owned by the list box, items
/// know nothing about it.
/// </summary>
public sealed partial class ListBox<Item> : Controllable
    where Item : IListBoxItem
{
    // Selected item indices, always sorted ascending with no duplicates
    readonly List<int> _selected = new();

    // One row per item, in item order
    readonly List<ListBoxRow> _rowControls = new();

    // The collection we are subscribed to, or null if not subscribed
    ObservableCollection<Item>? _hookedItems;

    // The last item clicked without Shift, used as the start of a Shift+click range
    int? _anchor;

    /// <summary>
    /// Raised when the set of selected items changes.  It is raised for user input, for selection
    /// changes made through code, and when a selected item is removed.  It is not raised when an
    /// index merely shifts because items were added or removed elsewhere in the list.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Raised after every click on an item, with the index of the item, even when the click did not
    /// change the selection or the selection mode is <see cref="SelectionMode.None"/>.  A combo box
    /// drop down uses it to close when the already selected item is clicked.
    /// </summary>
    public event EventHandler<int>? ItemClicked;

    /// <summary>
    /// The selected item indices in ascending order.  Empty if nothing is selected.
    /// </summary>
    public IReadOnlyList<int> SelectedIndices => _selected;

    /// <summary>
    /// A convenience for single selection.  It is the selected index when exactly one item is
    /// selected, and null when nothing is selected or when more than one item is selected.
    /// </summary>
    public int? SelectedIndex => _selected.Count == 1 ? _selected[0] : null;

    /// <summary>
    /// Creates the list box.
    /// </summary>
    public ListBox()
    {
        InitializeControl();
        DataContext.PropertyChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Called when attached to the visual tree.  Starts listening to the items collection.
    /// </summary>
    public void OnAttach()
    {
        HookItems();
        RebuildRows();
    }

    /// <summary>
    /// Called when removed from the visual tree.  Stops listening to the items collection.
    /// </summary>
    public void OnDetach()
    {
        UnhookItems();
    }

    /// <summary>
    /// Replaces the selection with the given indices.  Invalid indices are ignored.  In
    /// <see cref="SelectionMode.Single"/> only the first index is kept, and in
    /// <see cref="SelectionMode.None"/> nothing is selected.
    /// </summary>
    public void SetSelectedIndices(IEnumerable<int> indices)
    {
        var count = DataContext.Items.Count;
        var list = indices.Where(i => i >= 0 && i < count).Distinct().OrderBy(i => i).ToList();
        var mode = DataContext.SelectionMode;
        if (mode == SelectionMode.None)
            list.Clear();
        else if (mode == SelectionMode.Single && list.Count > 1)
            list.RemoveRange(1, list.Count - 1);
        _anchor = list.Count > 0 ? list[^1] : null;
        ApplySelection(list);
    }

    /// <summary>
    /// Selects only the item at the given index.
    /// </summary>
    public void Select(int index)
    {
        SetSelectedIndices(new[] { index });
    }

    /// <summary>
    /// Deselects everything.
    /// </summary>
    public void ClearSelection()
    {
        SetSelectedIndices(Array.Empty<int>());
    }

    /// <summary>
    /// Selects only the item at the given index, clamped to the last item, or clears the selection
    /// if the list is empty.  An app can call this from <see cref="SelectionChanged"/> to select
    /// the neighbor of a removed item.
    /// </summary>
    public void SelectNearest(int index)
    {
        var count = DataContext.Items.Count;
        if (count == 0)
            ClearSelection();
        else
            Select(Math.Clamp(index, 0, count - 1));
    }

    // Handles changes to the data.  Items replaced or reset rebuilds the rows, and a new selection
    // mode may need to trim the current selection.
    void OnDataContextChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Items" || string.IsNullOrEmpty(e.PropertyName))
        {
            if (_hookedItems != null)
                HookItems();
            var hadSelection = ClearSelectionQuiet();
            RebuildRows();
            if (hadSelection)
                RaiseSelectionChanged();
        }
        if (e.PropertyName == "SelectionMode" || string.IsNullOrEmpty(e.PropertyName))
            SetSelectedIndices(_selected.ToList());
    }

    // Subscribes to the current items collection, replacing any earlier subscription.
    void HookItems()
    {
        UnhookItems();
        _hookedItems = DataContext.Items;
        _hookedItems.CollectionChanged += OnItemsChanged;
    }

    // Removes the subscription to the items collection.
    void UnhookItems()
    {
        if (_hookedItems != null)
            _hookedItems.CollectionChanged -= OnItemsChanged;
        _hookedItems = null;
    }

    // Keeps the selection pointing at the same items, then rebuilds the rows.  The event is
    // raised last so a handler sees a consistent list.
    void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var selectionChanged = AdjustSelection(e);
        RebuildRows();
        if (selectionChanged)
            RaiseSelectionChanged();
    }

    // Updates the selected indices for a change to the items.  Returns true if the set of selected
    // items changed (a selected item was removed or replaced, or the list was reset).
    bool AdjustSelection(NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                var start = e.NewStartingIndex;
                var count = e.NewItems?.Count ?? 0;
                return RemapSelection(i => i >= start ? i + count : i);
            }
            case NotifyCollectionChangedAction.Remove:
            {
                var start = e.OldStartingIndex;
                var count = e.OldItems?.Count ?? 0;
                return RemapSelection(i => i < start ? i : i < start + count ? null : i - count);
            }
            case NotifyCollectionChangedAction.Replace:
            {
                var start = e.NewStartingIndex;
                var count = e.NewItems?.Count ?? 0;
                return _selected.Any(i => i >= start && i < start + count);
            }
            case NotifyCollectionChangedAction.Move:
            {
                var from = e.OldStartingIndex;
                var to = e.NewStartingIndex;
                RemapSelection(i =>
                    i == from ? to
                    : from < to && i > from && i <= to ? i - 1
                    : from > to && i >= to && i < from ? i + 1
                    : i);
                return false;
            }
            default:
                return ClearSelectionQuiet();
        }
    }

    // Maps every selected index and the anchor to a new index, where the map returns null to remove
    // an index.  Returns true if any selected index was removed.
    bool RemapSelection(Func<int, int?> map)
    {
        var mapped = new List<int>();
        var removed = false;
        foreach (var index in _selected)
        {
            if (map(index) is int newIndex)
                mapped.Add(newIndex);
            else
                removed = true;
        }
        mapped.Sort();

        if (_anchor is int oldAnchor && map(oldAnchor) is int newAnchor)
            _anchor = newAnchor;
        else if (_anchor is int removedAnchor)
            _anchor = mapped.Count == 0 ? null : mapped.MinBy(i => Math.Abs(i - removedAnchor));

        _selected.Clear();
        _selected.AddRange(mapped);
        return removed;
    }

    // Clears the selection without raising the event.  Returns true if anything was selected.
    bool ClearSelectionQuiet()
    {
        var hadSelection = _selected.Count > 0;
        _selected.Clear();
        _anchor = null;
        return hadSelection;
    }

    // Destroys and recreates all rows from the items.  This is the simple full-rebuild strategy.
    void RebuildRows()
    {
        var items = DataContext.Items;
        _selected.RemoveAll(i => i >= items.Count);

        _rows.View.ClearChildren();
        _rowControls.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            var index = i;
            var row = new ListBoxRow();
            row.View.AddGestureRecognizer(new ClickRecognizer((s, e) => OnRowClick(index, e)));
            row.View.AddChild(CreateItemController(items[i]).View);
            row.IsSelected = _selected.BinarySearch(i) >= 0;
            _rows.View.AddChild(row.View);
            _rowControls.Add(row);
        }
    }

    // The single place where an item data object becomes an item controller.
    Controllable CreateItemController(Item itemData)
    {
        return global::ZurfurGui.Loader.CreateDataController(itemData);
    }

    // Handles a click on a row: updates the selection, then raises ItemClicked.
    void OnRowClick(int index, PointerEvent e)
    {
        SelectFromClick(index, e);
        ItemClicked?.Invoke(this, index);
    }

    // Selects according to the selection mode and the Ctrl and Shift keys held during the click.
    void SelectFromClick(int index, PointerEvent e)
    {
        var mode = DataContext.SelectionMode;
        if (mode == SelectionMode.None)
            return;

        var modifiers = e.Contact.Modifiers;
        var isShift = (modifiers & PointerModifiers.Shift) != 0;
        var isControl = (modifiers & PointerModifiers.Control) != 0;

        if (mode == SelectionMode.Multiple && isShift && _anchor is int anchor)
        {
            var first = Math.Min(anchor, index);
            var last = Math.Max(anchor, index);
            ApplySelection(Enumerable.Range(first, last - first + 1).ToList());
        }
        else if (mode == SelectionMode.Multiple && isControl)
        {
            var list = _selected.ToList();
            if (!list.Remove(index))
            {
                list.Add(index);
                list.Sort();
            }
            _anchor = index;
            ApplySelection(list);
        }
        else
        {
            _anchor = index;
            ApplySelection(new List<int> { index });
        }
    }

    // Makes the given sorted list the selection, updates the row highlights, and raises the
    // event if the selection is different.
    void ApplySelection(List<int> selection)
    {
        if (_selected.SequenceEqual(selection))
            return;
        _selected.Clear();
        _selected.AddRange(selection);
        UpdateRowHighlights();
        RaiseSelectionChanged();
    }

    // Sets the selected state of every row from the selected indices.
    void UpdateRowHighlights()
    {
        for (int i = 0; i < _rowControls.Count; i++)
            _rowControls[i].IsSelected = _selected.BinarySearch(i) >= 0;
    }

    // Raises the SelectionChanged event.
    void RaiseSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
