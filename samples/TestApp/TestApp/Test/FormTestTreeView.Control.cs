using System.Collections.ObjectModel;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Controls.TreeView;
using TestApp.Test.Controls;

namespace TestApp.Test;

/// <summary>
/// Manual test form for TreeView.  The buttons act on both trees.
/// </summary>
public partial class FormTestTreeView
{
    int _nextNumber;

    /// <summary>
    /// Creates the form and fills both trees with sample nodes.
    /// </summary>
    public FormTestTreeView()
    {
        InitializeControl();

        for (int i = 0; i < 3; i++)
        {
            var text = NewTextNode();
            text.IsExpanded = i == 0;
            for (int j = 0; j < 3; j++)
            {
                var child = NewTextNode();
                child.Children.Add(NewTextNode());
                text.Children.Add(child);
            }
            _treeText.DataContext.Items.Add(text);

            var badge = NewBadgeNode();
            badge.IsExpanded = i == 0;
            for (int j = 0; j < 3; j++)
                badge.Children.Add(NewBadgeNode());
            _treeBadge.DataContext.Items.Add(badge);
        }

        _treeText.SelectionChanged += (s, e) =>
            ShowSelection(_treeText.SelectedNodes.Select(n => n.Text), _labelText);
        _treeBadge.SelectionChanged += (s, e) =>
            ShowSelection(_treeBadge.SelectedNodes.Select(n => n.Text), _labelBadge);

        buttonAddChild.Click += ButtonAddChild_Click;
        buttonRemove.Click += ButtonRemove_Click;
        buttonExpandAll.Click += (s, e) => SetExpanded(true);
        buttonCollapseAll.Click += (s, e) => SetExpanded(false);
    }

    // Adds a child to the first selected node of each tree, or a root node if nothing is selected.
    void ButtonAddChild_Click(object? sender, EventArgs e)
    {
        var text = _treeText.SelectedNodes.FirstOrDefault();
        if (text != null)
        {
            text.Children.Add(NewTextNode());
            text.IsExpanded = true;
        }
        else
            _treeText.DataContext.Items.Add(NewTextNode());

        var badge = _treeBadge.SelectedNodes.FirstOrDefault();
        if (badge != null)
        {
            badge.Children.Add(NewBadgeNode());
            badge.IsExpanded = true;
        }
        else
            _treeBadge.DataContext.Items.Add(NewBadgeNode());
    }

    // Removes the selected nodes of each tree.
    void ButtonRemove_Click(object? sender, EventArgs e)
    {
        foreach (var node in _treeText.SelectedNodes.ToList())
            Remove(_treeText.DataContext.Items, node);
        foreach (var node in _treeBadge.SelectedNodes.ToList())
            Remove(_treeBadge.DataContext.Items, node);
    }

    // Removes the node from the collection or from the children of whichever node holds it.
    static void Remove<T>(ObservableCollection<T> items, T? node) where T : class, ITreeNode<T>
    {
        if (node == null || items.Remove(node))
            return;
        foreach (var item in items)
            Remove(item.Children, node);
    }

    // Expands or collapses every node in both trees.
    void SetExpanded(bool expanded)
    {
        SetExpanded(_treeText.DataContext.Items, expanded);
        SetExpanded(_treeBadge.DataContext.Items, expanded);
    }

    // Expands or collapses the nodes and all of their descendants.
    static void SetExpanded<T>(IEnumerable<T> nodes, bool expanded) where T : ITreeNode<T>
    {
        foreach (var node in nodes)
        {
            node.IsExpanded = expanded;
            SetExpanded(node.Children, expanded);
        }
    }

    // Shows the first line of text of each selected node in the label below the tree.
    static void ShowSelection(IEnumerable<TextLines?> selected, TextView label)
    {
        var names = selected.Select(t => t is { Count: > 0 } lines ? lines[0] : "");
        label.DataContext.Text = new($"Selected: {string.Join(", ", names)}");
    }

    // Creates a numbered text node.
    TreeNodeTextData NewTextNode()
    {
        return new TreeNodeTextData() { Text = new TextLines($"Node {_nextNumber++}") };
    }

    // Creates a numbered node with a badge.
    TreeNodeBadgeData NewBadgeNode()
    {
        var number = _nextNumber++;
        return new TreeNodeBadgeData()
        {
            Badge = new TextLines($"{number % 10}"),
            Text = new TextLines($"Badge node {number}")
        };
    }
}
