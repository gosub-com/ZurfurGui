using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

namespace ZurfurGui.Draw;


public class DrawContext
{
    static readonly Rect EMPTY_DEVICE_CLIP = new Rect(0, 0, double.MaxValue, double.MaxValue);

    OsContext _context;

    View? _currentView;
    Point _origin;
    double _scale = 1.0;
    Rect _clip = EMPTY_DEVICE_CLIP;
    List<Rect> _clipStack = new List<Rect>();

    public int ClipLevel => _clipStack.Count;
    public Rect DeviceClip => _clip;

    // TBD: Turn into a class, PointerInfo
    public Point PointerPosition { get; private set; }

    public DrawContext(OsContext context)
    {
        _context = context;
    }

    public double MeasureTextWidth(string fontName, double fontSize, string text)
    {
        _context.FontName = fontName;
        _context.FontSize = 1000;
        return _context.MeasureTextWidth(text) * 0.001 * fontSize;
    }

    /// <summary>
    /// Set the currently rendering view, and  _origin and _scale.
    /// </summary>
    internal void SetCurrentViewInternal(View? view)
    {
        _currentView = view;
        if (view != null)
        {
            _origin = view.Origin;
            _scale = view.Scale;
        }
    }

    internal void SetPointerPosition(Point pos)
    {
        PointerPosition = pos;
    }

    public Color FillColor { set { _context.FillColor = value; } }
    public Color StrokeColor { set { _context.StrokeColor = value; } }
    public double LineWidth { set { _context.LineWidth = _scale * value; } }
    public string FontName { set { _context.FontName = value; } }
    public double FontSize { set { _context.FontSize = _scale * value; } }
    public void FillRect(double x, double y, double width, double height, double radius = 0)
    {
        // NOTE: Winforms throws an exception if width or height are 0 or negative. 
        // TBD: Fix winforms driver to match Javascript and then remove this test.
        if (width > 0 && height > 0)
            _context.FillRect(_scale * x + _origin.X, _scale * y + _origin.Y, _scale * width, _scale * height, radius);
    }
    public void FillRect(Rect r, double radius = 0)
        => FillRect(r.X, r.Y, r.Width, r.Height, radius);

    public void StrokeRect(double x, double y, double width, double height, double radius = 0)
    {
        // NOTE: Winforms throws an exception if width or height are 0 or negative.
        // TBD: Fix winforms driver to match Javascript and then remove this test.
        if (width > 0 && height > 0)
            _context.StrokeRect(_scale * x + _origin.X, _scale * y + _origin.Y, _scale * width, _scale * height, radius);
    }
    public void StrokeRect(Rect r, double radius = 0 )
        => StrokeRect(r.X, r.Y, r.Width, r.Height, radius);

    public void FillText(string text, double x, double y)
        => _context.FillText(text, _scale * x + _origin.X, _scale * y + _origin.Y);

    public void FillText(string text, Point p)
        => _context.FillText(text, p.X, p.Y);

    /// <summary>
    /// Clip to the specified rectangle in device pixels.
    /// Saves clip state to a stack. Restore is automatic and doesn't need to be done by user code.
    /// </summary>
    public void PushDeviceClip(Rect clip)
    { 
        if (_currentView == null)
            throw new InvalidOperationException("No current view");
        if (_currentView.PushedContentClip)
            return; // Already pushed
        _currentView.PushedContentClip = true;

        _clipStack.Add(_clip);
        _clip = _clip.Intersect(clip);
        _context.Clip(_clip.X, _clip.Y, _clip.Width, _clip.Height);
    }

    /// <summary>
    /// Pops the old clip state from the stack.
    /// </summary>
    internal void PopDeviceClip(View view)
    {
        if (!view.PushedContentClip)
            return;  // Not clipped
        view.PushedContentClip = false;

        if (_clipStack.Count == 0)
        {
            Debug.Assert(false, "UnClip without matching Clip");
            _clip = EMPTY_DEVICE_CLIP;
        }
        else
        {
            _clip = _clipStack[^1];
            _clipStack.RemoveAt(_clipStack.Count - 1);
            if (_clipStack.Count == 0)
                Debug.Assert(_clip == EMPTY_DEVICE_CLIP);
        }
        _context.UnClip();
    }
}
