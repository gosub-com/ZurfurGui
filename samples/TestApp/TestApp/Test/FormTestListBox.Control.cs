using ZurfurGui.Base;
using ZurfurGui.Controls;
using TestApp.Test.Controls;

namespace TestApp.Test;

/// <summary>
/// Manual test form for ListBox.  The buttons act on all four list boxes.
/// </summary>
public partial class FormTestListBox
{
    int _nextNumber;

    public FormTestListBox()
    {
        InitializeControl();

        for (int i = 0; i < 15; i++)
        {
            _listNone.DataContext.Items.Add(NewTextItem());
            _listSingle.DataContext.Items.Add(NewTextItem());
            _listMultiple.DataContext.Items.Add(NewTextItem());
            _listBadge.DataContext.Items.Add(NewBadgeItem());
        }

        _listNone.SelectionChanged += (s, e) => ShowSelection(_listNone, _labelNone);
        _listSingle.SelectionChanged += (s, e) => ShowSelection(_listSingle, _labelSingle);
        _listMultiple.SelectionChanged += (s, e) => ShowSelection(_listMultiple, _labelMultiple);
        _listBadge.SelectionChanged += (s, e) => ShowSelection(_listBadge, _labelBadge);

        buttonAddStart.Click += ButtonAddStart_Click;
        buttonAddEnd.Click += ButtonAddEnd_Click;
        buttonDeleteSelected.Click += ButtonDeleteSelected_Click;
    }

    // Adds a new item at the start of every list box.
    void ButtonAddStart_Click(object? sender, EventArgs e)
    {
        _listNone.DataContext.Items.Insert(0, NewTextItem());
        _listSingle.DataContext.Items.Insert(0, NewTextItem());
        _listMultiple.DataContext.Items.Insert(0, NewTextItem());
        _listBadge.DataContext.Items.Insert(0, NewBadgeItem());
    }

    // Adds a new item at the end of every list box.
    void ButtonAddEnd_Click(object? sender, EventArgs e)
    {
        _listNone.DataContext.Items.Add(NewTextItem());
        _listSingle.DataContext.Items.Add(NewTextItem());
        _listMultiple.DataContext.Items.Add(NewTextItem());
        _listBadge.DataContext.Items.Add(NewBadgeItem());
    }

    // Deletes the selected items from every list box.
    void ButtonDeleteSelected_Click(object? sender, EventArgs e)
    {
        DeleteSelected(_listNone);
        DeleteSelected(_listSingle);
        DeleteSelected(_listMultiple);
        DeleteSelected(_listBadge);
    }

    // Removes the selected items, highest index first so the lower indices stay valid.
    static void DeleteSelected<T>(ListBox<T> listBox) where T : IListBoxItem
    {
        foreach (var index in listBox.SelectedIndices.Reverse().ToList())
            listBox.DataContext.Items.RemoveAt(index);
    }

    // Shows the selected indices in the label below the list box.
    static void ShowSelection<T>(ListBox<T> listBox, TextView label) where T : IListBoxItem
    {
        label.DataContext.Text = new($"Selected: {string.Join(", ", listBox.SelectedIndices)}");
    }

    // Creates a numbered text item.
    ListBoxItemTextData NewTextItem()
    {
        return new ListBoxItemTextData() { Text = new TextLines($"Item {_nextNumber++}") };
    }

    // Creates a numbered item with a badge.
    ListBoxItemBadgeData NewBadgeItem()
    {
        var number = _nextNumber++;
        return new ListBoxItemBadgeData()
        {
            Badge = new TextLines($"{number % 10}"),
            Text = new TextLines($"Badge item {number}")
        };
    }
}
