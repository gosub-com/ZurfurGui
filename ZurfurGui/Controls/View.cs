using System.Diagnostics;

namespace ZurfurGui.Controls;

public enum HorizontalAlignment
{
    Stretch,
    Left,
    Center,
    Right
}

public enum VerticalAlignment
{
    Stretch,
    Top,
    Center,
    Bottom
}

public class View
{


    /// <summary>
    /// Parent view
    /// </summary>
    public View? ParentView { get; private set; }
    internal void SetParentView(View? parent) { ParentView = parent; }

    /// <summary>
    /// All child views, including ones that don't have a control
    /// </summary>
    public readonly List<View> Views = new List<View>();
    
    /// <summary>
    /// The control, if there is one
    /// </summary>
    public Controllable? Control { get; private set; }

    /// <summary>
    /// Measured size of view (as calculated by the measure pass)
    /// </summary>
    public Size DesiredSize { get; private set; }

    /// <summary>
    /// Bounds of view within parent (as caluclated by the measure pass)
    /// </summary>
    public Rect Bounds { get; private set; }

    public View() { }

    public View(Controllable control)
    {
        Control = control;
    }
    public View(Controllable control, IEnumerable<View> children)
    {
        Control = control;
        Views = children.ToList();
    }

    public string Name => Control?.Name ?? "";
    public string Type => Control?.Type ?? "";

    public override string ToString()
    {
        return $"{(Control == null ? "View" : $"View:{Control}")}";
    }

    public static View BuildViewTree(Controllable control)
        => ViewHelper.BuildViewTree(control);


    /// <summary>
    /// View is visible.
    /// TBD: Make this into a stylable property.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Requested size of the view.  Nan means it will auto-size.
    /// TBD: Make this into a stylable property.
    /// </summary>
    public Size Size { get; set; } = new(double.NaN, double.NaN);

    /// <summary>
    /// Requested maximum size of the view.
    /// TBD: Make this into a stylable property.
    /// </summary>
    public Size MaxSize { get; set; } = new(double.PositiveInfinity, double.PositiveInfinity);

    /// <summary>
    /// Requested minimum size of the view
    /// TBD: Make this into a stylable property.
    /// </summary>
    public Size MinSize { get; set; } = new();

    /// <summary>
    /// Hoizontal alignment
    /// TBD: Make this into a stylable property.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; }

    /// <summary>
    /// Vertical alignment
    /// TBD: Make this into a stylable property.
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; }

    /// <summary>
    /// Margin
    /// TBD: Make this into a stylable property.
    /// </summary>
    public Thickness Margin { get; set; }

    /// <summary>
    /// Called to measure the view and set MeasuredSize.
    /// Invisible views and views not attached to a control always have a size 
    /// of (0,0) and do not measure children (i.e. a control that adds extra views to the
    /// view tree takes responsibility for making sure measurement works for that control)
    /// The Size property (when not NAN) override the measurement.
    /// The MaxSize and MinSize also override the measurement.
    /// MeasuredSize is guaranteed to fit inside available.
    /// Similar to MeasureCore in WPF
    /// </summary>
    public void Measure(Size avaliable)
    {
        if (!IsVisible)
        {
            DesiredSize = new();
            return;
        }

        var controlSize = Control?.MeasureView(avaliable) ?? new();

        // Size override
        var sizeOverride = Size;
        if (!double.IsNaN(sizeOverride.Width))
            controlSize.Width = sizeOverride.Width;
        if (!double.IsNaN(sizeOverride.Height))
            controlSize.Height = sizeOverride.Height;

        // Max size override
        var maxSize = MaxSize;
        controlSize.Width = Math.Min(controlSize.Width, maxSize.Width);
        controlSize.Height = Math.Min(controlSize.Height, maxSize.Height);
        
        // Min  size override
        var minSize = MinSize;
        controlSize.Width = Math.Max(controlSize.Width, minSize.Width);
        controlSize.Height = Math.Max(controlSize.Height, minSize.Height);

        // Min available
        controlSize.Width = Math.Min(controlSize.Width, avaliable.Width);
        controlSize.Height = Math.Min(controlSize.Height, avaliable.Height);

        if (double.IsNaN(controlSize.Width) || double.IsNaN(controlSize.Height))
            throw new InvalidOperationException("Received NAN in Measure");

        DesiredSize = controlSize.MaxZero;
    }

    /// <summary>
    /// Called to set the Bounds of the control within the parent.
    /// Similar to ArrangeCore in WPF
    /// </summary>
    public void Arrange(Rect finalRect)
    {
        if (!IsVisible)
            return;

        var margin = Margin;
        var availableSize = finalRect.Size.Deflate(margin);
        var x = finalRect.X + margin.Left;
        var y = finalRect.Y + margin.Top;
        var width = availableSize.Width;
        var height = availableSize.Height;

        var horizontalAlignment = HorizontalAlignment;
        if (horizontalAlignment != HorizontalAlignment.Stretch)
            width = Math.Min(width, DesiredSize.Width - margin.Left - margin.Right);

        var verticalAlignment = VerticalAlignment;
        if (verticalAlignment != VerticalAlignment.Stretch)
            height = Math.Min(height, DesiredSize.Height - margin.Top - margin.Bottom);

        if (Control != null)
            (width, height) = Control.ArrangeViews(new Size(width, height)).Constrain(new Size(width, height));

        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Center:
            case HorizontalAlignment.Stretch:
                x += (availableSize.Width - width) / 2;
                break;
            case HorizontalAlignment.Right:
                x += availableSize.Width - width;
                break;
        }

        switch (verticalAlignment)
        {
            case VerticalAlignment.Center:
            case VerticalAlignment.Stretch:
                y += (availableSize.Height - height) / 2;
                break;
            case VerticalAlignment.Bottom:
                y += availableSize.Height - height;
                break;
        }

        Bounds = new Rect(x, y, width, height);
    }



}
