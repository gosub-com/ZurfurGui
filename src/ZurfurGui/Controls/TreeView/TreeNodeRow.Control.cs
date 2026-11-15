using ZurfurGui.Base;

namespace ZurfurGui.Controls.TreeView;

/// <summary>
/// The header of a tree node.  It hosts the gutter and the node content, and draws the hover,
/// pressed, and selected background.  The <see cref="TreeView{Item}"/> creates these.
/// </summary>
internal sealed partial class TreeNodeRow
{
    /// <summary>
    /// Initializes a tree node row control.
    /// </summary>
    public TreeNodeRow()
    {
        InitializeControl();
    }

    /// <summary>
    /// True if this row is selected.  The theme variable condition <c>TreeNodeRow.isSelected</c>
    /// reads this to choose the background color.
    /// </summary>
    public bool IsSelected
    {
        get => Properties.IsSelected;
        set
        {
            if (Properties.IsSelected == value)
                return;
            var properties = Properties;
            properties.IsSelected = value;
            View.InvalidateStyleCacheInternal();
        }
    }
}
