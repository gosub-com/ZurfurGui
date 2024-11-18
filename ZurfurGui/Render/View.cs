using System.Diagnostics;
using ZurfurGui.Controls;

namespace ZurfurGui.Render;

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
    /// <summary>
    /// Parent view
    /// </summary>
    public View? ParentView { get; private set; }
    internal void SetParentView(View? parent) { ParentView = parent; }

    /// <summary>
    /// All child views
    /// </summary>
    public readonly List<View> Views = new List<View>();

    /// <summary>
    /// Control properties
    /// </summary>
    public Properties Properties { get; set; } = new();

    /// <summary>
    /// The control, if there is one
    /// </summary>
    public Controllable Control { get; private set; }

    /// <summary>
    /// Measured size of view as calculated by the measure pass.
    /// </summary>
    public Size DesiredSize { get; private set; }

    /// <summary>
    /// Bounds of view within parent as caluclated by the arrange pass.
    /// </summary>
    public Rect Bounds { get; private set; }

    /// <summary>
    /// Location of view in device pixels as calculated by the post arrange pass.
    /// </summary>
    public Point Origin { get; private set; }

    /// <summary>
    /// Scale of view, relative to device pixels as calculated by the post arrange pass.
    /// </summary>
    public double Scale { get; private set; } = 1;

    /// <summary>
    /// Visually clipped region of the view in device pixels.
    /// </summary>
    public Rect Clip {  get; private set; }

    public Point toDevice(Point p) => Origin + p;
    public Size toDevice(Size s) => Scale * s;
    public Rect toDevice(Rect r) => new Rect(Origin + r.Position, Scale * r.Size);


    public View(Controllable control)
    {
        Control = control;
    }

    public string Type => Control.Type;

    public override string ToString()
    {
        return $"View:{Control}";
    }

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
        var isVisible = Properties.Get(ZGui.IsVisible, true);
        if (!isVisible)
            return;

        var margin = Properties.Get(ZGui.Margin);
        var constrained = ApplyLayoutConstraints(this, available.Deflate(margin));
        var measured = Control.MeasureView(constrained, measure);

        // Size override
        var sizeOverride = Properties.Get(ZGui.Size, new(double.NaN, double.NaN));
        if (!double.IsNaN(sizeOverride.Width))
            measured.Width = sizeOverride.Width;
        if (!double.IsNaN(sizeOverride.Height))
            measured.Height = sizeOverride.Height;

        // Max size override
        var sizeMax = Properties.Get(ZGui.SizeMax, new(double.PositiveInfinity, double.PositiveInfinity));
        measured.Width = Math.Min(measured.Width, sizeMax.Width);
        measured.Height = Math.Min(measured.Height, sizeMax.Height);

        // Min  size override
        var sizeMin = Properties.Get(ZGui.SizeMax);
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
    public void Arrange(Rect finalRect, MeasureContext measure)
    {
        var isVisible = Properties.Get(ZGui.IsVisible, true);
        if (!isVisible)
            return;

        var margin = Properties.Get(ZGui.Margin);
        var availableSize = finalRect.Size.Deflate(margin);
        var x = finalRect.X + margin.Left;
        var y = finalRect.Y + margin.Top;
        var size = availableSize;

        var horizontalAlignment = Properties.Get(ZGui.AlignHorizontal);
        if (horizontalAlignment != HorizontalAlignment.Stretch)
            size.Width = Math.Min(size.Width, DesiredSize.Width - margin.Left - margin.Right);

        var verticalAlignment = Properties.Get(ZGui.AlignVertical);
        if (verticalAlignment != VerticalAlignment.Stretch)
            size.Height = Math.Min(size.Height, DesiredSize.Height - margin.Top - margin.Bottom);

        size = ApplyLayoutConstraints(this, size);

        size = Control.ArrangeViews(size, measure).Constrain(size);

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

    /// <summary>
    /// Set view's origin and scale
    /// </summary>
    internal void PostArrange(Point origin, double scale, Rect clip)
    {
        var isVisible = Properties.Get(ZGui.IsVisible, true);
        if (!isVisible)
            return;

        Scale = scale * Properties.Get(ZGui.Magnification, 1);
        Origin = origin + scale * Bounds.Position;

        clip = clip.Intersect(Bounds).Move(-Bounds.Position);
        Clip = clip;
        foreach (var view in Views)
            view.PostArrange(Origin, Scale, clip);
    }

    public static Size ApplyLayoutConstraints(View v, Size constraints)
    {
        var size = v.Properties.Get(ZGui.Size, new(double.NaN, double.NaN));
        var sizeMax = v.Properties.Get(ZGui.SizeMax, new(double.PositiveInfinity, double.PositiveInfinity));
        var sizeMin = v.Properties.Get(ZGui.SizeMin);

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


    /// <summary>
    /// TBD: This needs to be a lot better, to support components, etc.
    /// https://dev.to/marcel-goldammer/dynamic-ids-in-angular-components-1b6n
    /// </summary>
    public List<View> FindAllById(string id)
    {
        var views = new List<View>();
        FindAllById(id, views);
        return views;
    }

    void FindAllById(string id, List<View> views)
    {
        if (Properties.Get(ZGui.Id) == id)
            views.Add(this);
        foreach (var view in Views)
            view.FindAllById(id, views);
    }
}
