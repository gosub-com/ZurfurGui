﻿using System.Collections;
using System.Diagnostics;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public enum AlignHorizontal : byte
{
    Stretch,
    Left,
    Center,
    Right
}

public enum AlignVertical : byte
{
    Stretch,
    Top,
    Center,
    Bottom
}

public class View
{
    /// <summary>
    /// All child views. 
    /// </summary>
    readonly List<View> _views = new List<View>();

    /// <summary>
    /// Iterate and find child views.  Use AddView (and friends) to modify the view list
    /// </summary>
    public RoList<View> Views => new RoList<View>(_views);

    /// <summary>
    /// Parent view
    /// </summary>
    public View? Parent { get; private set; }

    /// <summary>
    /// Control properties
    /// </summary>
    public readonly Properties Properties = new();

    /// <summary>
    /// The control, if there is one
    /// </summary>
    public Controllable Control { get; private set; }

    /// <summary>
    /// Measured size of view as calculated by the measure pass.
    /// </summary>
    public Size DesiredSize { get; private set; }

    /// <summary>
    /// Position of view within parent as caluclated by the arrange pass.
    /// </summary>
    public Point Position { get; private set; }

    /// <summary>
    /// Size of view as caluclated by the arrange pass.
    /// </summary>
    public Size Size { get; private set; }

    /// <summary>
    /// Location of view in device pixels as calculated by the post arrange pass.
    /// </summary>
    public Point Origin { get; private set; }

    /// <summary>
    /// Scale of view, relative to device pixels as calculated by the post arrange pass.
    /// </summary>
    public double Scale { get; private set; } = 1;

    /// <summary>
    /// Visually clipped region of the view in control coordinates
    /// </summary>
    public Rect Clip { get; private set; }


    public bool IsMeasureInvalid { get; private set; }
    public bool IsVisualInvalid { get; private set; }

    public Point toDevice(Point p) => Origin + p * Scale;
    public Size toDevice(Size s) => Scale * s;
    public Rect toDevice(Rect r) => new Rect(Origin + r.Position * Scale, Scale * r.Size);
    public Point toClient(Point p) => (p - Origin) / Scale;

    public bool PointerHoverTarget { get; internal set; }

    public View(Controllable control)
    {
        Control = control;
    }

    public override string ToString()
    {
        return $"{Control.Type}: {Properties.Get(Zui.Name) ?? "(no name)"}";
    }

    public void InvalidateMeasure()
    {
        if (IsMeasureInvalid)
            return;

        IsMeasureInvalid = true;
        var parent = Parent;
        while (parent != null && !parent.IsMeasureInvalid && parent.Properties.Get(Zui.IsVisible, true))
        {
            parent.InvalidateMeasure();
            parent = parent.Parent;
        }
    }

    public void InvalidateVisual()
    {
        if (IsVisualInvalid)
            return;

        IsVisualInvalid = true;
        var parent = Parent;
        while (parent != null && !parent.IsVisualInvalid && parent.Properties.Get(Zui.IsVisible, true))
        {
            parent.InvalidateVisual();
            parent = parent.Parent;
        }
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
        var isVisible = Properties.Get(Zui.IsVisible, true);
        if (!isVisible)
            return;

        IsMeasureInvalid = false;

        var margin = Properties.Get(Zui.Margin);
        var constrained = ApplyLayoutConstraints(this, available.Deflate(margin));
        var measured = Control.MeasureView(constrained, measure);

        // Size override
        var sizeOverride = Properties.Get(Zui.Size, new(double.NaN, double.NaN));
        if (!double.IsNaN(sizeOverride.Width))
            measured.Width = sizeOverride.Width;
        if (!double.IsNaN(sizeOverride.Height))
            measured.Height = sizeOverride.Height;

        // Max size override
        var sizeMax = Properties.Get(Zui.SizeMax, new(double.PositiveInfinity, double.PositiveInfinity));
        measured.Width = Math.Min(measured.Width, sizeMax.Width);
        measured.Height = Math.Min(measured.Height, sizeMax.Height);

        // Min  size override
        var sizeMin = Properties.Get(Zui.SizeMax);
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
        var isVisible = Properties.Get(Zui.IsVisible, true);
        if (!isVisible)
            return;

        var margin = Properties.Get(Zui.Margin);
        var availableSize = finalRect.Size.Deflate(margin);
        var x = finalRect.X + margin.Left;
        var y = finalRect.Y + margin.Top;
        var size = availableSize;

        var horizontalAlignment = Properties.Get(Zui.AlignHorizontal);
        if (horizontalAlignment != AlignHorizontal.Stretch)
            size.Width = Math.Min(size.Width, DesiredSize.Width - margin.Left - margin.Right);

        var verticalAlignment = Properties.Get(Zui.AlignVertical);
        if (verticalAlignment != AlignVertical.Stretch)
            size.Height = Math.Min(size.Height, DesiredSize.Height - margin.Top - margin.Bottom);

        size = ApplyLayoutConstraints(this, size);

        size = Control.ArrangeViews(size, measure).Min(size);

        switch (horizontalAlignment)
        {
            case AlignHorizontal.Center:
            case AlignHorizontal.Stretch:
                x += (availableSize.Width - size.Width) / 2;
                break;
            case AlignHorizontal.Right:
                x += availableSize.Width - size.Width;
                break;
        }

        switch (verticalAlignment)
        {
            case AlignVertical.Center:
            case AlignVertical.Stretch:
                y += (availableSize.Height - size.Height) / 2;
                break;
            case AlignVertical.Bottom:
                y += availableSize.Height - size.Height;
                break;
        }

        Position = new(x, y);
        Size = new(size.Width, size.Height);
    }

    /// <summary>
    /// Set view's origin and scale
    /// </summary>
    internal void PostArrange(Point origin, double scale, Rect clip)
    {
        var isVisible = Properties.Get(Zui.IsVisible, true);
        if (!isVisible)
            return;

        Scale = scale * Properties.Get(Zui.Magnification, 1);
        Origin = origin + scale * Position;

        clip = clip.Intersect(new(Position, Size)).Move(-Position);
        Clip = clip;
        foreach (var view in Views)
            view.PostArrange(Origin, Scale, clip);
    }

    public static Size ApplyLayoutConstraints(View v, Size constraints)
    {
        var size = v.Properties.Get(Zui.Size, new(double.NaN, double.NaN));
        var sizeMax = v.Properties.Get(Zui.SizeMax, new(double.PositiveInfinity, double.PositiveInfinity));
        var sizeMin = v.Properties.Get(Zui.SizeMin);

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
    /// Find exactly one view by name.  Throws an exception if none (or multiples) are found
    /// </summary>
    public View FindByName(string name)
    {
        var views = FindAllByName(name);
        if (views.Count == 0)
            throw new ArgumentException($"Can't find view named '{name}'");
        if (views.Count > 1)
            throw new ArgumentException($"Found multiple views names '{name}'");
        return views[0];
    }

    /// <summary>
    /// TBD: This needs to be a lot better, to support components, etc.
    /// https://dev.to/marcel-goldammer/dynamic-ids-in-angular-components-1b6n
    /// </summary>
    public List<View> FindAllByName(string name)
    {
        var views = new List<View>();
        FindAllByName(name, views);
        return views;
    }

    void FindAllByName(string name, List<View> views)
    {
        if (Properties.Get(Zui.Name) == name)
            views.Add(this);
        foreach (var view in Views)
            view.FindAllByName(name, views);
    }

    public static View? FindHitTarget(View view, Point target)
    {
        // Quick exit when not visible or not in clip region
        var clip = view.toDevice(view.Clip);
        if (!clip.Contains(target))
            return null;
        if (!view.Properties.Get(Zui.IsVisible, true))
            return null;

        // Check children first
        var views = view.Views;
        for (var i = views.Count - 1; i >= 0; i--)
        {
            var hit = FindHitTarget(views[i], target);
            if (hit != null)
                return hit;
        }

        if (!view.Properties.Get(Zui.DisableHitTest, false) && view.Control.IsHit(target))
            return view;

        return null;
    }

    public void AddView(View view)
    {
        if (view.Parent != null)
            throw new ArgumentException("AddView: Parent must be null");
        view.Parent = this;
        _views.Add(view);
    }
    public void AddViews(IEnumerable<View> views)
    {
        foreach (var view in views)
            AddView(view);
    }

    public void RemoveView(int index)
    {
        var view = _views[index];
        _views.RemoveAt(index);
        view.Parent = null;
    }

    public void ClearViews()
    {
        while (_views.Count > 0)
            RemoveView(_views.Count - 1);
    }

    public void BringToFront()
    {
        var parent = Parent;
        if (parent == null)
            throw new ArgumentException("BringToFront: Parent view cannot be null");
        var i = parent.Views.FindIndex(v => v == this);
        if (i < 0)
            throw new ArgumentException("BringToFront: Parent does not contain this view");
        parent._views.RemoveAt(i);
        parent._views.Add(this);
    }


    /// <summary>
    /// Walks up the tree to find the main AppWindow.  Returns null if this view is not attached to the tree.
    /// </summary>
    public AppWindow? AppWindow
    {
        get
        {
            var view = this;
            while (view != null)
            {
                if (view.Control is AppWindow appWindow)
                    return appWindow;
                view = view.Parent;
            }
            return null;
        }
    }

    /// <summary>
    /// True when the pointer is captured.  Automatically reset (and PointerCaptureLost event sent) when the pointer
    /// is released. There isn't a need to use the PointerUp event if you capture the pointer and use the CaptureLost event.
    /// Can only be set inside of PointerDown event (can be reset any time)
    /// </summary>
    public bool CapturePointer
    {
        get { return AppWindow?.GetIsPointerCaptured(this) ?? false; }
        set
        {
            if (AppWindow is not AppWindow appWindow)
                throw new InvalidOperationException("View is not attached to main tree");
            appWindow.SetIsPointerCapture(this, value);
        }
    }

}
