using ZurfurGui.Base;

namespace ZurfurGui.Controls;

/// <summary>
/// A row inside a <see cref="ListBox{Item}"/>.  It hosts one item control and draws the hover,
/// pressed, and selected background.  The list box creates these.
/// </summary>
public sealed partial class ListBoxRow
{
    /// <summary>
    /// Initializes a list box row control.
    /// </summary>
    public ListBoxRow()
    {
        InitializeControl();
    }

    /// <summary>
    /// True if this row is selected.  The theme variable condition <c>ListBoxRow.isSelected</c>
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
