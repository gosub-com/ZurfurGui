using System.Collections;
using System.Diagnostics;
using ZurfurGui.Layout;
using ZurfurGui.Draw;
using ZurfurGui.Base;
using static ZurfurGui.Base.Size;
using System.Net.Http.Headers;

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

public sealed class View
{
    /// <summary>
    /// All child views. 
    /// </summary>
    readonly List<View> _children = new List<View>();

    /// <summary>
    /// View properties
    /// </summary>
    readonly Properties _properties = new();

    /// <summary>
    /// Iterate and find child views.  Use AddChild (and friends) to modify the children
    /// </summary>
    public RoList<View> Children => new RoList<View>(_children);

    /// <summary>
    /// Parent view
    /// </summary>
    public View? Parent { get; private set; }

    /// <summary>
    /// The controller
    /// </summary>
    public Controllable Controller { get; private set; }

    /// <summary>
    /// When null, the control is treated as a panel.
    /// </summary>
    public Layoutable? Layout { get; set; }

    /// <summary>
    /// When null, the control is treated as a panel (i.e. draw backgrond & border, performs hit testing, etc.)
    /// </summary>
    public Drawable? Draw { get; set; }

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

    public bool IsMeasureInvalid { get; private set; }
    public bool IsVisualInvalid { get; private set; }

    public Point toDevice(Point p) => Origin.ToVector + p * Scale;
    public Size toDevice(Size s) => Scale * s;
    public Rect toDevice(Rect r) => new Rect(Origin.ToVector + r.Position * Scale, Scale * r.Size);
    public Point toClient(Point p) => ((p - Origin) / Scale).ToPoint;

    public bool PointerHoverTarget { get; internal set; }

    public View(Controllable control)
    {
        Controller = control;
    }

    public override string ToString()
    {
        return $"{Controller.TypeName}: {GetProperty(Zui.Name) ?? "(no name)"}";
    }

    public void InvalidateMeasure()
    {
        if (IsMeasureInvalid)
            return;

        IsMeasureInvalid = true;
        var parent = Parent;
        while (parent != null && !parent.IsMeasureInvalid && parent.GetStyle(Zui.IsVisible, true))
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
        while (parent != null && !parent.IsVisualInvalid && parent.GetStyle(Zui.IsVisible, true))
        {
            parent.InvalidateVisual();
            parent = parent.Parent;
        }
    }

    public int PropertiesCount => _properties.Count;
    internal void PropertiesSetUnionInternal(Properties properties)
        => _properties.SetUnion(properties);
    

    /// <summary>
    /// Retrieve a property from the view's property collection.
    /// </summary>
    public T? GetProperty<T>(PropertyKey<T> key, T? defaultProperty = default)
    {
        return _properties.Get(key, defaultProperty);
    }

    public void SetProperty<T>(PropertyKey<T> key, T value)
    {
        _properties.Set(key, value);
    }

    /// <summary>
    /// Retrieve a style property from the view's property collection,
    /// or if it's not found walk up the tree to find a style property
    /// based on the classes property.
    /// </summary>
    public T GetStyle<T>(PropertyKey<T> key, T defaultProperty)
    {
        if (_properties.TryGet(key, out var value) && value is T)
            return value;

        // TBD: Walk up the tree to find a style property

        return defaultProperty;
    }


    /// <summary>
    /// Add an event to the property collection
    /// </summary>
    public void AddEvent<T>(PropertyKey<T> property, T ev) where T : Delegate
    {
        _properties.AddEvent(property, ev);
    }

    /// <summary>
    /// Remove an event from the property collection
    /// </summary>
    public void RemoveEvent<T>(PropertyKey<T> property, T ev) where T : Delegate
    {
        _properties.RemoveEvent(property, ev);
    }


    /// <summary>
    /// Called to measure the view and set DesiredSize.
    /// Invisible views do not measure children (i.e. a control that adds extra views to the
    /// view tree takes responsibility for making sure measurement works for that control)
    /// Similar to MeasureCore in WPF
    /// </summary>
    public void Measure(Size available, MeasureContext measure)
    {
        if (GetStyle(Zui.IsVisible, true))
        {
            IsMeasureInvalid = false;
            DesiredSize = LayoutManager.Measure(this, available, measure);
        }
    }


    /// <summary>
    /// Called to set the Bounds of the control within the parent.
    /// Similar to ArrangeCore in WPF
    /// </summary>
    public void Arrange(Rect finalRect, MeasureContext measure)
    {
        if (GetStyle(Zui.IsVisible, true))
            (Position, Size) = LayoutManager.Arrange(this, finalRect, measure);
    }


    /// <summary>
    /// Set view's origin and scale
    /// </summary>
    internal void PostArrange(Point origin, double scale)
    {
        if (GetStyle(Zui.IsVisible, true))
        {
            Scale = scale * GetStyle(Zui.Magnification, null).Or(1);
            Origin = origin.ToVector + scale * Position;

            foreach (var view in Children)
                view.PostArrange(Origin, Scale);
        }
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
        if (GetProperty(Zui.Name) == name)
            views.Add(this);
        foreach (var view in Children)
            view.FindAllByName(name, views);
    }

    public void AddChild(View view)
    {
        if (view.Parent != null)
            throw new ArgumentException("AddChild: Parent must be null");
        view.Parent = this;
        _children.Add(view);
    }

    public void RemoveChild(int index)
    {
        var view = _children[index];
        _children.RemoveAt(index);
        view.Parent = null;
    }

    public void RemoveChild(View view)
    {
        _children.Remove(view);
        view.Parent = null;
    }

    public void ClearChildren()
    {
        while (_children.Count > 0)
            RemoveChild(_children.Count - 1);
    }

    public void BringToFront()
    {
        var parent = Parent;
        if (parent == null)
            throw new ArgumentException("BringToFront: Parent view cannot be null");
        var i = parent.Children.FindIndex(v => v == this);
        if (i < 0)
            throw new ArgumentException("BringToFront: Parent does not contain this view");
        parent._children.RemoveAt(i);
        parent._children.Add(this);
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
                if (view.Controller is AppWindow appWindow)
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
