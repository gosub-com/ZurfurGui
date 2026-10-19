using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Input;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public partial class TextView : Controllable, Layoutable, Renderable
{
    const double CLIP_ROUNDING_ERROR = 0.0001; // Don't clip because of rounding errors
    const int CLIENT_CLIP_EXTRA_LINES = 2;

    Size _lastMeasuredSize;
    int _renderedFirstLine;
    int _renderedLastLine;

    public TextView()
    {
        InitializeControl();
        View.Render = this;
        View.Layout = this;
    }


    public Size MeasureView(View view, MeasureContext measure, Size available)
    {
        // Quick exit if text hasn't changed since last measure
        if (!view.Flags.HasFlag(ViewFlags.Measure))
            return _lastMeasuredSize;

        var font =  view.GetProperty(FontProperty);
        var fontName = font.Name ?? "Arial";
        var fontSize = font.Size.Or(16.0);
        var text = view.GetProperty(TextProperty);
        var metrics = measure.GetFontMetrics(fontName, fontSize);

        var maxWidth = text.Count == 0 ? 0 : text.Max(line => measure.MeasureTextWidth(fontName, fontSize, line));

        // Height = (lines - 1) * lineHeight + (ascent + descent) for last line
        var height = text.Count == 0 ? 0
            : (text.Count - 1) * metrics.LineHeight + metrics.Ascent + metrics.Descent;

        _lastMeasuredSize = new Size(maxWidth, height);
        return _lastMeasuredSize;
    }

    public void ArrangeViews(View view, MeasureContext measure)
    {
        if (view.Children.Count != 0)
            throw new InvalidOperationException("Text layout does not support children");
    }

    public RenderResult Render(View view, RenderContext context, Rect clientClip)
    {
        var color = view.GetProperty(ColorProperty);
        var text = view.GetProperty(TextProperty);

        if (text.Count == 0 || color.A == 0)
        {
            _renderedFirstLine = 0;
            _renderedLastLine = 0;
            return RenderResult.Discard;
        }

        if (clientClip.IsEmpty)
        {
            _renderedFirstLine = 0;
            _renderedLastLine = 0;
            return RenderResult.RenderedClip;
        }

        var fontProp = view.GetProperty(FontProperty);
        var fontName = fontProp.Name ?? "Arial";
        var fontSize = fontProp.Size.Or(16.0);
        var metrics = context.MeasureContext.GetFontMetrics(fontName, fontSize);

        // Calculate range of lines to display
        var firstLine = 0;
        var lastLine = text.Count;
        if (metrics.LineHeight > 0)
        {
            firstLine = (int)Math.Floor((clientClip.Y - view.ContentRect.Y) / metrics.LineHeight);
            lastLine = (int)Math.Ceiling((clientClip.Bottom - view.ContentRect.Y) / metrics.LineHeight);
            firstLine = Math.Max(0, Math.Min(text.Count, firstLine));
            lastLine = Math.Max(firstLine, Math.Min(text.Count, lastLine));
        }

        // Re-use previous rendering if it's still valid
        if (!view.Flags.HasFlag(ViewFlags.Render)
            && firstLine >= _renderedFirstLine
            && lastLine <= _renderedLastLine)
        {
            return RenderResult.Keep;
        }

        // Draw a little above and below so we don't need to re-render on small scrolls
        var renderFirstLine = Math.Max(0, firstLine - CLIENT_CLIP_EXTRA_LINES);
        var renderLastLine = Math.Min(text.Count, lastLine + CLIENT_CLIP_EXTRA_LINES);
        _renderedFirstLine = renderFirstLine;
        _renderedLastLine = renderLastLine;

        // Clip if content size is larger than available size
        var contentSize = view.ContentRect.Size.Inflate(CLIP_ROUNDING_ERROR);
        if (contentSize.Width < view.DesiredContentSize.Width
            || contentSize.Height < view.DesiredContentSize.Height)
        {
            context.PushClip(view.ContentRect);
        }

        // Render visible lines
        var baselineY = view.ContentRect.Y + metrics.Ascent;
        var font = new Font(fontName, fontSize);
        var brush = new Brush(color);
        for (int i = renderFirstLine; i < renderLastLine; i++)
        {
            var y = baselineY + i * metrics.LineHeight;
            context.FillText(font, brush, text[i], view.ContentRect.X, y);
        }

        return renderFirstLine == 0 && renderLastLine == text.Count
            ? RenderResult.RenderedAll
            : RenderResult.RenderedClip;
    }

    public bool IsHit(View view, HitTestContext context)
    {
        var p = view.toClient(context.Contact.DevicePosition);
        return new Rect(new(0, 0), view.DesiredTotalSize).Contains(p);
    }



}

