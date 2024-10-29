using System.Diagnostics;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public enum HorizontalAlignment : byte
{
    Stretch,
    Left,
    Center,
    Right
}

public enum VerticalAlignment : byte
{
    Stretch,
    Top,
    Center,
    Bottom
}

public class View
{
    public static readonly PropertyIndex<string> Name = new("Name");
    public static readonly PropertyIndex<bool> IsVisible = new("IsVisible");
    public static readonly PropertyIndex<HorizontalAlignment> AlignHorizontal = new("AlignHorizontal");
    public static readonly PropertyIndex<VerticalAlignment> AlignVertical = new("AlignVertical");
    public static readonly PropertyIndex<string> Text = new("Text");
    public static readonly PropertyIndex<Thickness> Margin = new("Margin");
    public static readonly PropertyIndex<Size> Size = new("Size");
    public static readonly PropertyIndex<Size> SizeMax = new("SizeMax");
    public static readonly PropertyIndex<Size> SizeMin = new("SizeMin");

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
    /// Bounds of view within parent (as caluclated by the arrange pass)
    /// </summary>
    public Rect Bounds { get; private set; }

    public View(Controllable control)
    {
        Control = control;
    }
    public View(Controllable control, IEnumerable<View> children)
    {
        Control = control;
        Views = children.ToList();
    }

    public string Type => Control?.Type ?? "";

    public override string ToString()
    {
        return $"{(Control == null ? "View" : $"View:{Control}")}";
    }

    public static View BuildViewTree(Controllable control)
        => ViewHelper.BuildViewTree(control);


    /// <summary>
    /// Called to measure the view and set MeasuredSize.
    /// Invisible views and views not attached to a control always have a size 
    /// of (0,0) and do not measure children (i.e. a control that adds extra views to the
    /// view tree takes responsibility for making sure measurement works for that control)
    /// The Size property (when not NAN) override the measurement.
    /// MeasuredSize is guaranteed to fit inside available.
    /// Similar to MeasureCore in WPF
    /// </summary>
    public void Measure(Size available, MeasureContext measure)
    {
        var isVisible = Control?.Properties?.Gets(IsVisible) ?? true;
        if (!isVisible)
        {
            DesiredSize = new();
            return;
        }

        var margin = Control?.Properties.Gets(Margin) ?? new();
        var constrained = ApplyLayoutConstraints(this, available.Deflate(margin));
        var measured = Control?.MeasureView(constrained, measure) ?? new();

        // Size override
        var sizeOverride = Control?.Properties.Gets(Size)??new(double.NaN, double.NaN);
        if (!double.IsNaN(sizeOverride.Width))
            measured.Width = sizeOverride.Width;
        if (!double.IsNaN(sizeOverride.Height))
            measured.Height = sizeOverride.Height;

        // Max size override
        var sizeMax = Control?.Properties.Gets(SizeMax) ?? new(double.PositiveInfinity, double.PositiveInfinity);
        measured.Width = Math.Min(measured.Width, sizeMax.Width);
        measured.Height = Math.Min(measured.Height, sizeMax.Height);

        // Min  size override
        var sizeMin = Control?.Properties.Gets(SizeMax) ?? new(0, 0);
        measured.Width = Math.Max(measured.Width, sizeMin.Width);
        measured.Height = Math.Max(measured.Height, sizeMin.Height);

        // Min available
        measured.Width = Math.Min(measured.Width, available.Width);
        measured.Height = Math.Min(measured.Height, available.Height);

        if (double.IsNaN(measured.Width) || double.IsNaN(measured.Height))
            throw new InvalidOperationException("Received NAN in Measure");

        DesiredSize = measured.Inflate(margin).MaxZero;
    }

    /// <summary>
    /// Called to set the Bounds of the control within the parent.
    /// Similar to ArrangeCore in WPF
    /// </summary>
    public void Arrange(Rect finalRect)
    {
        var isVisible = Control?.Properties?.Gets(IsVisible) ?? true;
        if (!isVisible)
            return;

        var margin = Control?.Properties.Gets(Margin) ?? new();
        var availableSize = finalRect.Size.Deflate(margin);
        var x = finalRect.X + margin.Left;
        var y = finalRect.Y + margin.Top;
        var size = availableSize;

        var horizontalAlignment = Control?.Properties.Gets(AlignHorizontal) ?? new();
        if (horizontalAlignment != HorizontalAlignment.Stretch)
            size.Width = Math.Min(size.Width, DesiredSize.Width - margin.Left - margin.Right);

        var verticalAlignment = Control?.Properties.Gets(AlignVertical) ?? new();
        if (verticalAlignment != VerticalAlignment.Stretch)
            size.Height = Math.Min(size.Height, DesiredSize.Height - margin.Top - margin.Bottom);

        size = ApplyLayoutConstraints(this, size);

        if (Control != null)
            size = Control.ArrangeViews(size).Constrain(size);

        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Center:
            case HorizontalAlignment.Stretch:
                x += (availableSize.Width - size.Width) / 2;
                break;
            case HorizontalAlignment.Right:
                x += availableSize.Width - size.Width;
                break;
        }

        switch (verticalAlignment)
        {
            case VerticalAlignment.Center:
            case VerticalAlignment.Stretch:
                y += (availableSize.Height - size.Height) / 2;
                break;
            case VerticalAlignment.Bottom:
                y += availableSize.Height - size.Height;
                break;
        }

        Bounds = new Rect(x, y, size.Width, size.Height);
    }

    public static Size ApplyLayoutConstraints(View v, Size constraints)
    {
        var size = v.Control?.Properties.Gets(Size)??new(double.NaN, double.NaN);
        var sizeMax = v.Control?.Properties.Gets(SizeMax) ?? new(double.PositiveInfinity, double.PositiveInfinity);
        var sizeMin = v.Control?.Properties.Gets(SizeMin) ?? new(0,0);

        var h = size.Height;
        var height = double.IsNaN(h) ? double.PositiveInfinity : h;
        var maxHeight = Math.Max(Math.Min(height, sizeMax.Height), sizeMin.Height);

        height = double.IsNaN(h) ? 0 : h;
        var minHeight = Math.Max(Math.Min(maxHeight, height), sizeMin.Height);

        var w = size.Width;
        var width = double.IsNaN(w) ? double.PositiveInfinity : w;
        var maxWidth = Math.Max(Math.Min(width, sizeMax.Width), sizeMin.Width);

        width = double.IsNaN(w) ? 0 : w;
        var minWidth = Math.Max(Math.Min(maxWidth, width), sizeMin.Width);

        return new Size(
            Math.Clamp(constraints.Width, minWidth, maxWidth),
            Math.Clamp(constraints.Height, minHeight, maxHeight));
    }

}
