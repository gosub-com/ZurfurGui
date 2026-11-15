using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ZurfurGui.Controls.TreeView;

/// <summary>
/// Data contract for a node shown in a <see cref="TreeView{Item}"/>.  A node control declares it in
/// its ZUI file with <c>$implements: "ITreeNode&lt;MyNodeData&gt;"</c> and the <c>$data</c> fields
/// <c>children</c> (<c>[]MyNode</c>), <c>isExpanded</c>, and <c>isSelected</c>.  The tree reads
/// and writes these flags, so the data is the single source of truth for selection and expansion.
/// A node object must appear once, in one tree, and the data must not contain cycles.
/// </summary>
public interface ITreeNode<TSelf> : INotifyPropertyChanged
    where TSelf : ITreeNode<TSelf>
{
    /// <summary>
    /// The child nodes of this node.  Empty for a leaf.
    /// </summary>
    ObservableCollection<TSelf> Children { get; }

    /// <summary>
    /// True when the children of this node are shown.
    /// </summary>
    bool IsExpanded { get; set; }

    /// <summary>
    /// True when this node is selected.
    /// </summary>
    bool IsSelected { get; set; }
}
