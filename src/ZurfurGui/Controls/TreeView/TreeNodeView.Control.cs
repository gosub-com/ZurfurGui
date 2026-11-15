using ZurfurGui.Base;

namespace ZurfurGui.Controls.TreeView;

/// <summary>
/// One node of a <see cref="TreeView{Item}"/>: a header row above a stack that holds the child
/// nodes while the node is expanded.  The tree view creates these.
/// </summary>
internal sealed partial class TreeNodeView
{
    /// <summary>
    /// Initializes a tree node view.
    /// </summary>
    public TreeNodeView()
    {
        InitializeControl();
    }
}
