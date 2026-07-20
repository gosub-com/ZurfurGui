using System.Diagnostics;
using ZurfurGui.Controls;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Render;
using ZurfurGui.Windows;
using ZurfurGui.Styles;
using ZurfurGui.Platform;

namespace ZurfurGui.Base;


public sealed class View
{
    // TBD: Should we be caching style lookups in the properties?
    internal const int PROPERTY_STYLE_CACHE_BEGIN = 10000;
    internal const int PROPERTY_STYLE_CACHE_END = 20000;

    internal static long s_measureCount {  get; private set; }

    /// <summary>
    /// All child views. 
    /// </summary>
    readonly List<View> _children = new List<View>();

    /// <summary>
    /// View properties
    /// </summary>
    internal readonly Properties _properties = new();

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
    /// When null, the control is treated as a panel (i.e. render background & border, performs hit testing, etc.)
    /// </summary>
    public Renderable? Render { get; set; }

    /// <summary>
    /// Desired size of view including margin, border, and padding. Calculated by the measure pass.
    /// </summary>
    public Size DesiredTotalSize { get; private set; }

    /// <summary>
    /// Desired size of view's content excluding margin, border, and padding. Calculated by the measure pass.
    /// </summary>
    public Size DesiredContentSize { get; private set; }

    /// <summary>
    /// Content rect inside the view, accounting for padding and border.
    /// Can be smaller than the DesiredContentSize.
    /// </summary>
    public Rect ContentRect { get; private set; }

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

    public ViewFlags Flags { get; internal set; }
    public ViewFlags FlagsChild { get; internal set; }


    // TBD: Measure performance and remove if not needed.
    internal MeasureCache _measureCache;
    internal OsRenderBuffer? _renderUnderBuffer;
    internal OsRenderBuffer? _renderOverBuffer;

    internal struct MeasureCache
    {
        public bool IsVisible;
        public Size Avaliable;

        public Rect FinalAtArrange;
        public Point OriginAtArrange;
        public double ScaleAtArrange;
    }


    public View(Controllable control)
    {
        Controller = control;
    }

    public Point toDevice(Point p) => new Point(Origin.X + p.X * Scale, Origin.Y + p.Y * Scale);
    public Size toDevice(Size s) => Scale * s;
    public Rect toDevice(Rect r) => new Rect(toDevice(r.Position), toDevice(r.Size));
    public Point toClient(Point p) => new Point((p.X - Origin.X) / Scale, (p.Y - Origin.Y) / Scale);
    public Size toClient(Size p) => p / Scale;
    public Rect toClient(Rect r) => new Rect(toClient(r.Position), toClient(r.Size));

    public override string ToString()
    {
        var name = GetProperty(Panel.Name) ?? "";
        return $"{Controller.TypeName}: {(name == "" ? "(no name)" : name)}";
    }

    public string Name => GetProperty(Panel.Name) ?? "";

    /// <summary>
    /// Call when a view's measurement becomes invalid and needs to be re-measured.
    /// NOTE: This is called automatically when properties that affect measurement are changed.
    /// </summary>
    public void InvalidateMeasure()
    {
        SetFlags(ViewFlags.Measure);
    }

    /// <summary>
    /// Call when a view's rendering becomes invalid and needs to be re-rendered.
    /// Note: This is called automatically when properties that affect rendering are changed.
    /// </summary>
    public void InvalidateRender()
    {
        SetFlags(ViewFlags.Render);
    }

    internal void InvalidateStyleCacheInternal()
    {
        // Find cached properties
        var keysToRemove = _properties
            .Select(k => k.key)
            .Where(k => k.IdAsInt >= PROPERTY_STYLE_CACHE_BEGIN && k.IdAsInt < PROPERTY_STYLE_CACHE_END)
            .ToArray();

        foreach (var key in keysToRemove)
        {
            var nonCachedKey = new PropertyKeyId(key.IdAsInt - PROPERTY_STYLE_CACHE_BEGIN);
            nonCachedKey.Info?.RefreshCacheProperty(this);
        }
    }


    /// <summary>
    /// Invalidate the style cache for this view and all descendants, forcing a full re-style on the next frame.
    /// Call this when the global theme or active style sheets change.
    /// </summary>
    internal void InvalidateStyleTree()
    {
        SetFlags(ViewFlags.StyleDown);
    }

    /// <summary>
    /// Set the view flags (e.g. InvalidateMeasure, InvalidateRender, InvalidateStyle).  
    /// </summary>
    public void SetFlags(ViewFlags flags)
    {
        Flags |= flags;
        var view = Parent;
        while (view != null && (view.FlagsChild & flags) != flags)
        {
            view.FlagsChild |= flags;
            view = view.Parent;
        }
    }

    public int PropertiesCount => _properties.Count;
    internal void PropertiesSetUnionInternal(Properties properties)
        => _properties.SetUnion(properties);
    

    /// <summary>
    /// Get a property from the view's property collection, or return the property default when not found.
    /// </summary>
    public T? GetProperty<T>(PropertyKey<T> key)
    {
        return _properties.Get(key);
    }

    public void SetProperty<T>(PropertyKey<T> key, T value)
    {
        if (GetProperty(key) is T oldValue && oldValue.Equals(value))
            return;
        SetFlags(key.Flags);
        _properties.Set(key, value);
        _properties.RemoveById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN));
    }

    public bool ContainsProperty<T>(PropertyKey<T> key)
    {
        return _properties.ContainsKey(key);
    }

    public void RemoveProperty<T>(PropertyKey<T> key)
    {
        if (!_properties.ContainsKey(key))
            return;
        SetFlags(key.Flags);
        _properties.Remove(key);
        _properties.RemoveById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN));
    }

    /// <summary>
    /// Return the property or style (property overrides style)
    /// </summary>
    public T GetStyle<T>(PropertyKey<T> key)
    {
        // Properties cached from style lookup below
        if (_properties.TryGetById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN),
                out var styledValue) && styledValue is T typedStyledValue)
        {
            return typedStyledValue;
        }

        var styledProperty = StyleManager.FindStyle(this, key);

        // Cache the styled property for quick lookup above
        _properties.SetById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN), styledProperty!);

        return styledProperty;
    }

    internal void RefreshCacheProperty<T>(PropertyKey<T> key)
    {
        var styledProperty = StyleManager.FindStyle(this, key);

        // If cache did not change, no need to invalidate
        if (_properties.TryGetById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN),
                out var styledValue) && styledValue is T typedStyledValue
                && EqualityComparer<T>.Default.Equals(typedStyledValue, styledProperty))
        {
            return;
        }

        // Cache the new style property for quick lookup above
        _properties.SetById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN), styledProperty!);
        SetFlags(key.Flags);
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
        // No need to re-measure if the last measurement is still valid
        var needsMeasure = Flags.HasFlag(ViewFlags.Measure);
        var childNeedsMeasure = FlagsChild.HasFlag(ViewFlags.Measure);
        if (!needsMeasure && !childNeedsMeasure && available == _measureCache.Avaliable)
        {
            return;
        }

        // Quick exit if invisible
        _measureCache.IsVisible = GetStyle(Panel.IsVisible);
        if (!_measureCache.IsVisible)
            return;

        _measureCache.Avaliable = available;
        s_measureCount++;

        // Include padding and border in the measurement
        var margin = GetStyle(Panel.Margin).Or(0);
        var padding = GetStyle(Panel.Padding).Or(0) + new Thickness(GetStyle(Panel.BorderWidth));
        var sizeRequest = GetStyle(Panel.SizeRequest);
        var sizeMin = GetStyle(Panel.SizeMin).Or(0).MaxZero;
        var sizeMax = GetStyle(Panel.SizeMax).Or(double.PositiveInfinity);
        var constrained = sizeRequest.Or(available.Deflate(margin)).Min(sizeMax).Max(sizeMin).Deflate(padding);

        // Measure children controls
        var newDesiredContentSize = MeasureLayout(measure, constrained);

        // Desired total view size includes padding and border
        // Clamp to view size constaints, then min(available)
        var newDesiredTotalSize = sizeRequest.Or(newDesiredContentSize.Inflate(padding))
            .Min(sizeMax).Max(sizeMin).Min(available).Inflate(margin).MaxZero;

        if (double.IsNaN(newDesiredTotalSize.Width) || double.IsNaN(newDesiredTotalSize.Height))
            throw new InvalidOperationException("Received NAN in Measure");

        if (newDesiredContentSize != DesiredContentSize || newDesiredTotalSize != DesiredTotalSize)
        {
            InvalidateRender();
            DesiredContentSize = newDesiredContentSize;
            DesiredTotalSize = newDesiredTotalSize;
        }
    }

    Size MeasureLayout(MeasureContext measure, Size constrained)
    {
        if (Layout is Layoutable layout)
            return layout.MeasureView(this, measure, constrained);
        else
            return LayoutPanel.MeasurePanel(this, measure, constrained);
    }

    /// <summary>
    /// Called to set the Position and Size of the control within the parent.
    /// Similar to ArrangeCore in WPF.
    /// </summary>
    public void Arrange(Rect final, MeasureContext measure)
    {
        // Quick exit if invisible
        if (!_measureCache.IsVisible)
            return;

        var margin = GetStyle(Panel.Margin).Or(0);
        var availableSize = final.Size.Deflate(margin);

        var x = final.X + margin.Left;
        var y = final.Y + margin.Top;
        var newSize = availableSize;

        var align = GetStyle(Panel.Align);
        var alignHor = align.Horizontal ?? AlignHorizontal.Stretch;
        var alignVert = align.Vertical ?? AlignVertical.Stretch;

        if (alignHor != AlignHorizontal.Stretch)
            newSize.Width = Math.Min(newSize.Width, DesiredTotalSize.Width - margin.Left - margin.Right);

        if (alignVert != AlignVertical.Stretch)
            newSize.Height = Math.Min(newSize.Height, DesiredTotalSize.Height - margin.Top - margin.Bottom);

        // Clamp to view size constraints
        var sizeRequest = GetStyle(Panel.SizeRequest);
        var sizeMin = GetStyle(Panel.SizeMin).Or(0).MaxZero;
        var sizeMax = GetStyle(Panel.SizeMax).Or(double.PositiveInfinity);
        newSize = sizeRequest.Or(newSize).Min(sizeMax).Max(sizeMin);

        var padding = GetStyle(Panel.Padding).Or(0) + new Thickness(GetStyle(Panel.BorderWidth));
        var newContentRect = new Rect(new Point(0, 0), newSize).Deflate(padding);

        switch (alignHor)
        {
            case AlignHorizontal.Center:
            case AlignHorizontal.Stretch:
                x += (availableSize.Width - newSize.Width) / 2;
                break;
            case AlignHorizontal.Right:
                x += availableSize.Width - newSize.Width;
                break;
        }

        switch (alignVert)
        {
            case AlignVertical.Center:
            case AlignVertical.Stretch:
                y += (availableSize.Height - newSize.Height) / 2;
                break;
            case AlignVertical.Bottom:
                y += availableSize.Height - newSize.Height;
                break;
        }

        var newPosition = new Vector(x, y) + GetStyle(Panel.Offset).Or(0);
        var newScale = (Parent?.Scale ?? 1) * GetStyle(Panel.Magnification);
        var newOrigin = (Parent?.Origin ?? new()).ToVector + newScale * newPosition;

        // Invalidate if the size, scale, or content rect changed.
        if (final.Size != _measureCache.FinalAtArrange.Size
                || newScale != _measureCache.ScaleAtArrange
                || newSize != Size
                || newContentRect != ContentRect)
        {
            InvalidateRender();
        }


        ContentRect = newContentRect;
        Size = newSize;
        Position = newPosition;



        // No need to re-arrange children if nothing changed
        if (((Flags | FlagsChild) & ViewFlags.Measure) == ViewFlags.None
            && final == _measureCache.FinalAtArrange
            && newOrigin == _measureCache.OriginAtArrange
            && newScale == _measureCache.ScaleAtArrange)
        {
            return;
        }

        _measureCache.FinalAtArrange = final;
        _measureCache.ScaleAtArrange = newScale;
        _measureCache.OriginAtArrange = newOrigin;
        Scale = newScale;
        Origin = newOrigin;


        // Arrange child views
        if (Layout is Layoutable layout)
            layout.ArrangeViews(Controller.View, measure);
        else
            LayoutPanel.ArrangePanel(Controller.View, measure);
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
        if (GetProperty(Panel.Name) == name)
            views.Add(this);
        foreach (var view in Children)
            view.FindAllByName(name, views);
    }

    public void AddChild(View child)
    {
        if (child.Parent != null)
            throw new ArgumentException("AddChild: Parent must be null");
        if (child.Controller.GetType() == typeof(AppWindow))
            throw new InvalidOperationException("AddChild: AppWindow cannot be added to the view tree");
        child.Parent = this;
        _children.Add(child);
        SetFlags(ViewFlags.Render | ViewFlags.Measure);
        child.SetFlags(ViewFlags.Render | ViewFlags.Measure);
        if (AppWindow != null)
            SendAttachMessages(child);
    }

    void SendAttachMessages(View view)
    {
        view.Controller.OnAttach();
        foreach (var child in view.Children)
            SendAttachMessages(child);
    }

    /// <summary>
    /// Remove this view from the parent (thereby removing it from the view tree).  
    /// </summary>
    public void RemoveFromParent()
    {
        Parent?.RemoveChild(this);
    }

    /// <summary>
    /// Remove a child view from this view (thereby removing it from the view tree).  
    /// </summary>
    public void RemoveChild(View view)
    {
        var index = _children.IndexOf(view);
        if (index >= 0)
            RemoveChild(index);
    }

    /// <summary>
    /// Remove a child view from this view (thereby removing it from the view tree).  
    /// </summary>
    public void RemoveChild(int index)
    {
        var child = _children[index];
        Debug.Assert(child.Parent == this);
        if (child.AppWindow != null)
            SendDetachMessages(child);
        _children.RemoveAt(index);
        child.Parent = null;
        SetFlags(ViewFlags.Render | ViewFlags.Measure);
    }

    /// <summary>
    /// Remove all children views from this view (thereby removing them from the view tree).  
    /// </summary>
    public void ClearChildren()
    {
        while (_children.Count > 0)
            RemoveChild(_children.Count - 1);
    }

    void SendDetachMessages(View view)
    {
        foreach (var child in view.Children)
            SendDetachMessages(child);
        view.Controller.OnDetach();
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
        get { return AppWindow?.Renderer?.PointerHover?.GetIsPointerCaptured(this) ?? false; }
        set
        {
            if (AppWindow is not AppWindow appWindow)
                throw new InvalidOperationException("View is not attached to main tree");
            appWindow?.Renderer?.PointerHover?.SetIsPointerCapture(this, value);
        }
    }

}
