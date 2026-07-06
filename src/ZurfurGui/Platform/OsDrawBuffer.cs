using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ZurfurGui.Base;

namespace ZurfurGui.Platform;

/// <summary>
/// Low level draw commands used by OsDrawBuffer.
/// </summary>
public enum OsDrawCommand : ushort
{
    FillColor = 1,
    StrokeColor = 2,
    LineWidth = 3,
    FontName = 4,
    FillRect = 6,
    StrokeRect = 7,
    FillText = 8,
    StrokePolyLine = 9,
    FillPolygon = 10,
    PushClip = 11,
    PopClip = 12
}

/// <summary>
/// This is a low level internal draw buffer, used to communicate with platform device drivers.
/// Commands are very low level, and mostly follow the JavaScript canvas API.
/// The producer side calls the functions (e.g. FillRect, etc.), and the consumer side calls
/// GetCommand and GetString.
/// </summary>
public class OsDrawBuffer
{
    int _length = 0;
    double[] _buffer = new double[256];
    ObjectCache<string> _stringCache;

    Color? _fillColor = null;
    Color? _strokeColor = null;
    double? _lineWidtgh = null;
    string? _fontName = null;
    double? _fontSize = null;

    public OsDrawBuffer(ObjectCache<string> stringCache)
    {
        _stringCache = stringCache;
    }

    /// <summary>
    /// Return the underlying buffer array.
    /// </summary>
    public double[] Buffer => _buffer;

    /// <summary>
    /// Current length of the buffer
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Resets the array back to length 0, but does not clear the underlying array.
    /// </summary>
    public void Reset()
    {
        _length = 0;
        _fillColor = null;
        _strokeColor = null;
        _lineWidtgh = null;
        _fontName = null;
        _fontSize = null;
    }

    public (OsDrawCommand command, int paramCount) GetCommand(int index)
    {
        var commandHeader = (long)_buffer[index];
        var paramCount = (int)(commandHeader & 0xFFFFFFFF);
        var command = (OsDrawCommand)(commandHeader >> 32);
        return (command, paramCount);
    }

    public string GetString(int index)
    {
        return _stringCache.GetObject(index);
    }

    /// <summary>
    /// TBD: Remove heap allocation here by using a Span or similar. 
    /// </summary>
    public double[] GetArray(int index, int length)
    {
        var buffer = Buffer;
        var result = new double[length];
        for (int i = 0; i < length; i++)
            result[i] = buffer[index + i];
        return result;
    }


    public void FillColor(Color color)
    {
        if (_fillColor == color)
            return;
        _fillColor = color;
        AppendCommand(OsDrawCommand.FillColor, _stringCache.GetIndex(color.CssColor));
    }

    public void StrokeColor(Color color)
    {
        if (_strokeColor == color)
            return;
        _strokeColor = color;
        AppendCommand(OsDrawCommand.StrokeColor, _stringCache.GetIndex(color.CssColor));
    }

    public void LineWidth(double lineWidth)
    {
        if (_lineWidtgh == lineWidth)
            return;
        _lineWidtgh = lineWidth;
        AppendCommand(OsDrawCommand.LineWidth, lineWidth);
    }

    public void FontName(string fontName, double fontSize)
    {
        if (_fontName == fontName && _fontSize == fontSize)
            return;
        _fontName = fontName;
        _fontSize = fontSize;
        AppendCommand(OsDrawCommand.FontName, _stringCache.GetIndex(fontName), fontSize);
    }

    public void FillRect(double x, double y, double width, double height, double radius)
    {
        AppendCommand(OsDrawCommand.FillRect, x, y, width, height, radius);
    }

    public void StrokeRect(double x, double y, double width, double height, double radius)
    {
        AppendCommand(OsDrawCommand.StrokeRect, x, y, width, height, radius);
    }

    public void FillText(string text, double x, double y)
    {
        AppendCommand(OsDrawCommand.FillText, _stringCache.GetIndex(text), x, y);
    }

    public void StrokePolyLine(ReadOnlySpan<double> points)
    {
        AppendCommandHeader(OsDrawCommand.StrokePolyLine, points.Length);
        for (var i = 0; i < points.Length; i++)
            Append(points[i]);
    }

    public void FillPolygon(ReadOnlySpan<double> points)
    {
        AppendCommandHeader(OsDrawCommand.FillPolygon, points.Length);
        for (var i = 0; i < points.Length; i++)
            Append(points[i]);
    }

    public void Clip(double x, double y, double width, double height)
    {
        AppendCommand(OsDrawCommand.PushClip, x, y, width, height);
    }

    public void UnClip()
    {
        AppendCommand(OsDrawCommand.PopClip);
        _lineWidtgh = null;
        _fillColor = null;
        _strokeColor = null;
        _fontName = null;
        _fontSize = null;
    }


    /// <summary>
    /// Append an item, possibly resizing the underlying array if needed.
    /// </summary>
    private void Append(double item)
    {
        if (_length >= _buffer.Length)
            Array.Resize(ref _buffer, _buffer.Length * 2);
        _buffer[_length++] = item;
    }

    private void AppendCommand(OsDrawCommand command)
    {
        AppendCommandHeader(command, 0);
    }

    private void AppendCommand(OsDrawCommand command, double p1)
    {
        AppendCommandHeader(command, 1);
        Append(p1);
    }

    private void AppendCommand(OsDrawCommand command, double p1, double p2)
    {
        AppendCommandHeader(command, 2);
        Append(p1);
        Append(p2);
    }

    private void AppendCommand(OsDrawCommand command, double p1, double p2, double p3)
    {
        AppendCommandHeader(command, 3);
        Append(p1);
        Append(p2);
        Append(p3);
    }

    private void AppendCommand(OsDrawCommand command, double p1, double p2, double p3, double p4)
    {
        AppendCommandHeader(command, 4);
        Append(p1);
        Append(p2);
        Append(p3);
        Append(p4);
    }

    private void AppendCommand(OsDrawCommand command, double p1, double p2, double p3, double p4, double p5)
    {
        AppendCommandHeader(command, 5);
        Append(p1);
        Append(p2);
        Append(p3);
        Append(p4);
        Append(p5);
    }

    public void AppendCommandHeader(OsDrawCommand command, int paramCount)
    {
        Append( ((long)command << 32) + paramCount);
    }


    public Span<double> AsSpan(int start, int length)
    {
        if (start + length > _length)
            throw new ArgumentOutOfRangeException(nameof(length), "Span exceeds buffer length");
        return _buffer.AsSpan(start, length);
    }

    public Span<double> AsSpan(int start)
    {
        return _buffer.AsSpan(start, _length - start);
    }

    /// <summary>
    /// Transform the buffer in place, applying the given offset and scale to all coordinates.
    /// NOTE: PushClip is not transformed, as it is assumed to be in device coordinates.
    /// </summary>
    public static void TransformBuffer(Span<double> buffer, Point offset, double scale)
    {
        int pc = 0;
        while (pc < buffer.Length)
        {
            var commandHeader = (long)buffer[pc];
            var paramCount = (int)(commandHeader & 0xFFFFFFFF);
            var command = (OsDrawCommand)(commandHeader >> 32);
            var pi = pc + 1;
            pc = pc + paramCount + 1;

            switch (command)
            {
                case OsDrawCommand.FillRect:
                case OsDrawCommand.StrokeRect:
                    buffer[pi] = buffer[pi] * scale + offset.X;
                    buffer[pi + 1] = buffer[pi + 1] * scale + offset.Y;
                    buffer[pi + 2] = buffer[pi + 2] * scale;
                    buffer[pi + 3] = buffer[pi + 3] * scale;
                    buffer[pi + 4] = buffer[pi + 4] * scale;
                    break;
                case OsDrawCommand.FillText:
                    buffer[pi + 1] = buffer[pi + 1] * scale + offset.X;
                    buffer[pi + 2] = buffer[pi + 2] * scale + offset.Y;
                    break;
                case OsDrawCommand.StrokePolyLine:
                case OsDrawCommand.FillPolygon:
                    for (var i = 0; i < paramCount; i += 2)
                    {
                        buffer[pi + i] = buffer[pi + i] * scale + offset.X;
                        buffer[pi + i + 1] = buffer[pi + i + 1] * scale + offset.Y;
                    }
                    break;
                case OsDrawCommand.LineWidth:
                    buffer[pi] = buffer[pi] * scale;
                    break;
                case OsDrawCommand.FontName:
                    buffer[pi + 1] = buffer[pi + 1] * scale;
                    break;
                case OsDrawCommand.PushClip:
                    buffer[pi] = buffer[pi] * scale + offset.X;
                    buffer[pi + 1] = buffer[pi + 1] * scale + offset.Y;
                    buffer[pi + 2] = buffer[pi + 2] * scale;
                    buffer[pi + 3] = buffer[pi + 3] * scale;
                    break;
                case OsDrawCommand.FillColor:
                case OsDrawCommand.StrokeColor:
                case OsDrawCommand.PopClip:
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }


        }
    }

}



