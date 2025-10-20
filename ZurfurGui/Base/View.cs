using System.Diagnostics;
using ZurfurGui.Draw;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Windows;

namespace ZurfurGui.Base;


public sealed class View
{

    // TBD: Should we be caching style lookups in the properties?
    const int PROPERTY_STYLE_CACHE_BEGIN = 10000;
    const int PROPERTY_STYLE_CACHE_END = 20000;

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


    internal bool PushedContentClip;

    public Point toDevice(Point p) => Origin.ToVector + p * Scale;
    public Size toDevice(Size s) => Scale * s;
    public Rect toDevice(Rect r) => new Rect(Origin.ToVector + r.Position * Scale, Scale * r.Size);
    public Point toClient(Point p) => ((p - Origin) / Scale).ToPoint;

    internal struct MeasureCache
    {
        public Size AvaliableAtMeasure;
        public Rect FinalAtArrange;
        public Point OriginAtArrange;
        public double ScaleAtArrange;

        // NOTE: Caching these might not be worth it since we bypass most measuring when nothing changes.
        // TBD: Profile
        public bool IsVisible;
        public SizeProp SizeRequest;
        public SizeProp SizeMin;
        public SizeProp SizeMax;
        public Thickness Padding;
        public Thickness Margin;
        public BackgroundProp Background;
        public bool Clip;
    }

    internal MeasureCache _cache;


    public View(Controllable control)
    {
        Controller = control;
    }

    public override string ToString()
    {
        var name = GetProperty(Zui.Name) ?? "";
        return $"{Controller.TypeName}: {(name == "" ? "(no name)" : name)}";
    }

    /// <summary>
    /// Call when a view's measurement becomes invalid and needs to be re-measured.
    /// NOTE: This is called automatically when properties that affect measurement are changed.
    /// </summary>
    public void InvalidateMeasure()
    {
        SetFlags(ViewFlags.ReMeasure);
    }

    /// <summary>
    /// Call when a view's drawing becomes invalid and needs to be redrawn.
    /// Note: This is called automatically when properties that affect drawing are changed.
    /// </summary>
    public void InvalidateDraw()
    {
        SetFlags(ViewFlags.ReDraw);
    }

    void SetFlags(ViewFlags flags)
    {
        if ((Flags & flags) == flags)
            return;

        Flags |= flags;
        var view = this;
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
    }

    /// <summary>
    /// Set a property, don't invalidate
    /// </summary>
    internal void SetPropertyNoFlags<T>(PropertyKey<T> key, T value)
    {
        _properties.Set(key, value);
    }

    /// <summary>
    /// Retrieve a style property from the view's property collection,
    /// or if it's not found walk up the tree to find a style property
    /// based on the classes property.
    /// </summary>
    public T GetStyle<T>(PropertyKey<T> key) where T : IProperty<T>, new()
    {
        // Properties set by code take precedence
        T property;
        if (_properties.TryGet(key, out var value) && value is T)
        {
            if (value.IsComplete)
                return value;
            property = value;
        }
        else
        {
            property = new T();
        }

        // Properties cached from style lookup below
        if (_properties.TryGetById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN),
                out var styledValue) && styledValue is T typedStyledValue)
        {
            return property.Or(typedStyledValue);
        }

        // Lookup styledProperty (matches first, then defaults)
        var styledProperty = FindStyledProperty(key, new());

        // Cache the styled property for quick lookup above
        _properties.SetById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN), styledProperty);

        return property.Or(styledProperty);
    }

    internal void ClearStyleCache()
    {
        var keysToRemove = _properties
            .Select(k => k.key)
            .Where( k => k.IdAsInt >= PROPERTY_STYLE_CACHE_BEGIN && k.IdAsInt < PROPERTY_STYLE_CACHE_END)
            .ToList();
        foreach (var key in keysToRemove)
            _properties.RemoveById(key);
    }


    /// <summary>
    /// Walk up the view tree to find a style property based on the classes property.
    /// </summary>
    T FindStyledProperty<T>(PropertyKey<T> key, T property) where T : IProperty<T>, new()
    {
        var classes = _properties.Get(Zui.Classes);
        if (classes == null || classes.Count == 0)
            return new();

        // Walk up the view tree
        for (var view = this; view != null; view = view.Parent)
        {
            if (!view._properties.TryGet(Zui.Styles, out var styles) || styles == null)
                continue;

            foreach (var style in styles.Reverse())
            {
                if (style.TryGet(Zui.Selectors, out var selectors) && selectors != null
                    && style.TryGet(key, out var p) && p != null 
                    && StyleMatches(selectors, classes))
                {
                    property = property.Or(p);
                    if (property.IsComplete)
                        return property;
                }
            }
        }
        return property;
    }

    bool StyleMatches(TextLines selectors, TextLines classes)
    {
        foreach (var selector in selectors)
        {
            var s = selector.Split(':');
            foreach (var c in classes)
            {
                if (c == s[0])
                {
                    // Check pseudo classes
                    var match = true;
                    for (int i = 1; i < s.Length; i++)
                    {
                        switch (s[i])
                        {
                            case "IsPointerOver":
                                if (!GetProperty(Zui.IsPointerOver).Or(false))
                                    match = false;
                                break;
                            default:
                                match = false;
                                break;
                        }
                    }
                    return match;
                }
            }
        }
        return false;
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
        // Quick exit if invisible
        _cache.IsVisible = GetStyle(Zui.IsVisible).Or(true);
        if (!_cache.IsVisible)
            return;

        // No need to re-measure if the last measurement is still valid
        if ( ((Flags | FlagsChild) & ViewFlags.ReMeasure) == ViewFlags.None
            && available == _cache.AvaliableAtMeasure)
        {
            return;
        }

        _cache.AvaliableAtMeasure = available;
        _cache.SizeRequest = GetStyle(Zui.SizeRequest);
        _cache.SizeMin = GetStyle(Zui.SizeMin);
        _cache.SizeMax = GetStyle(Zui.SizeMax);
        _cache.Padding = GetStyle(Zui.Padding).Or(0);
        _cache.Margin = GetStyle(Zui.Margin).Or(0);
        _cache.Background = GetStyle(Zui.Background);
        _cache.Clip = GetStyle(Zui.Clip).Or(false);

        // Include padding and border in the measurement
        var margin = _cache.Margin;
        var padding = _cache.Padding + new Thickness(_cache.Background.BorderWidth.Or(0));

        var constrained = ClampViewSize(available.Deflate(margin)).Deflate(padding);

        // Measure control content (default is a panel)
        if (Layout is Layoutable layout)
            DesiredContentSize = layout.MeasureView(this, measure, constrained);
        else
            DesiredContentSize = LayoutPanel.MeasurePanel(this, measure, constrained);

        // Desired total view size includes padding and border
        var measured = DesiredContentSize.Inflate(padding);

        measured = ClampViewSize(measured).Min(available);

        if (double.IsNaN(measured.Width) || double.IsNaN(measured.Height))
            throw new InvalidOperationException("Received NAN in Measure");

        DesiredTotalSize = measured.Inflate(margin).MaxZero;
    }



    /// <summary>
    /// Called to set the Position and Size of the control within the parent.
    /// Similar to ArrangeCore in WPF.
    /// </summary>
    public void Arrange(Rect final, MeasureContext measure)
    {
        // Quick exit if invisible
        if (!_cache.IsVisible)
            return;

        var margin = _cache.Margin;
        var availableSize = final.Size.Deflate(margin);

        var x = final.X + margin.Left;
        var y = final.Y + margin.Top;
        var size = availableSize;

        var align = GetStyle(Zui.Align);
        var alignHor = align.Horizontal ?? AlignHorizontal.Stretch;
        var alignVert = align.Vertical ?? AlignVertical.Stretch;

        if (alignHor != AlignHorizontal.Stretch)
            size.Width = Math.Min(size.Width, DesiredTotalSize.Width - margin.Left - margin.Right);

        if (alignVert != AlignVertical.Stretch)
            size.Height = Math.Min(size.Height, DesiredTotalSize.Height - margin.Top - margin.Bottom);

        size = ClampViewSize(size);

        var padding = _cache.Padding + new Thickness(_cache.Background.BorderWidth.Or(0));

        ContentRect = new Rect(new Point(0, 0), size).Deflate(padding);
        Size = size;

        switch (alignHor)
        {
            case AlignHorizontal.Center:
            case AlignHorizontal.Stretch:
                x += (availableSize.Width - size.Width) / 2;
                break;
            case AlignHorizontal.Right:
                x += availableSize.Width - size.Width;
                break;
        }

        switch (alignVert)
        {
            case AlignVertical.Center:
            case AlignVertical.Stretch:
                y += (availableSize.Height - size.Height) / 2;
                break;
            case AlignVertical.Bottom:
                y += availableSize.Height - size.Height;
                break;
        }

        Position = new Vector(x, y) + GetStyle(Zui.Offset).Or(0);
        var scale = (Parent?.Scale??1) * GetStyle(Zui.Magnification).Or(1);
        var origin = (Parent?.Origin??new()).ToVector + Scale * Position;

        // No need to re-arrange children if nothing changed
        if (((Flags | FlagsChild) & ViewFlags.ReMeasure) == ViewFlags.None
            && final == _cache.FinalAtArrange && scale == _cache.ScaleAtArrange && origin == _cache.OriginAtArrange)
        {
            return;
        }
        _cache.FinalAtArrange = final;
        _cache.ScaleAtArrange = scale;
        _cache.OriginAtArrange = origin;
        Scale = scale;
        Origin = origin;


        // Arrange child views
        if (Layout is Layoutable layout)
            layout.ArrangeViews(Controller.View, measure);
        else
            LayoutPanel.ArrangePanel(Controller.View, measure);
    }

    /// <summary>
    /// Clamp the requestedSize to be within the view's SizeMin..SizeMax property constraints.
    /// Uses the view's SizeRequest property if it's avaliable and ignoreSizeRequest is false.
    /// NOTE: Always >= 0 and SizeMin (even if Size < SizeMin)
    /// </summary>
    Size ClampViewSize(Size requestedSize)
    {
        var size = _cache.SizeRequest.Or(requestedSize);
        var sizeMax = _cache.SizeMax.Or(double.PositiveInfinity);
        var sizeMin = _cache.SizeMin.Or(0).MaxZero;
        return size.Min(sizeMax).Max(sizeMin);
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

    public void AddChild(View child)
    {
        if (child.Parent != null)
            throw new ArgumentException("AddChild: Parent must be null");
        if (child.Controller.GetType() == typeof(AppWindow))
            throw new InvalidOperationException("AddChild: AppWindow cannot be added to the view tree");
        child.Parent = this;
        _children.Add(child);
        if (AppWindow != null)
            SendAttachMessages(child);
    }

    void SendAttachMessages(View view)
    {
        Debug.WriteLine($"Attach: {view.Controller.TypeName}");
        view.Controller.OnAttach();
        foreach (var child in view.Children)
            SendAttachMessages(child);
    }

    public void RemoveChild(View view)
    {
        var index = _children.IndexOf(view);
        if (index >= 0)
            RemoveChild(index);
    }

    public void RemoveChild(int index)
    {
        var child = _children[index];
        Debug.Assert(child.Parent == this);
        if (child.AppWindow != null)
            SendDetachMessages(child);
        _children.RemoveAt(index);
        child.Parent = null;
    }

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
        get { return AppWindow?.PointerHover?.GetIsPointerCaptured(this) ?? false; }
        set
        {
            if (AppWindow is not AppWindow appWindow)
                throw new InvalidOperationException("View is not attached to main tree");
            appWindow?.PointerHover?.SetIsPointerCapture(this, value);
        }
    }

}
