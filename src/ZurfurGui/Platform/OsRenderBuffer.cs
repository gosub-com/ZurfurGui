using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ZurfurGui.Base;
using ZurfurGui.Collections;

namespace ZurfurGui.Platform;

/// <summary>
/// Low level render commands used by OsRenderBuffer.
/// </summary>
public enum OsRenderCommand : ushort
{
    SetStrokeColorWidth = 3,
    SetFillColor = 4,
    SetFontNameSize =5,
    FillRect = 6,
    StrokeRect = 7,
    FillText = 8,
    StrokePolyLine = 9,
    FillPolygon = 10,
    PushClip = 11,
    PopClip = 12
}

/// <summary>
/// This is a low level internal render buffer, used to communicate with platform device drivers.
/// Commands are very low level, and mostly follow the JavaScript canvas API.
/// The producer side calls the functions (e.g. FillRect, etc.), and the consumer side calls
/// GetCommand and GetString.
/// 
/// Strings are initially stored as indices into the local string buffer so they can be retained
/// for long periods of time.  Later, when Composite is called, they are marshaled to the platform
/// and stored as LRU indices.
/// </summary>
public class OsRenderBuffer
{
    int _commandsLength = 0;
    double[] _commands = Array.Empty<double>();

    int _stringsLength = 0;
    string [] _strings = Array.Empty<string>();

    public static readonly OsRenderBuffer Empty = new OsRenderBuffer();

    public OsRenderBuffer()
    {
    }

    /// <summary>
    /// Return the underlying buffer array.
    /// </summary>
    public double[] Commands => _commands;

    /// <summary>
    /// Current length of the buffer
    /// </summary>
    public int CommandsLength => _commandsLength;

    /// <summary>
    /// Resets the length back to 0, but does not clear the underlying array.
    /// This is so you can reuse the buffer without allocating a new one each time.
    /// </summary>
    public void Clear()
    {
        _commandsLength = 0;
        _stringsLength = 0;
    }


    /// <summary>
    /// Make a clone of this buffer.  If maybeInto is provided, it will try to
    /// re-use this buffer by cloning into it and returning it, but it won't re-use
    /// the buffer if it is not the correct length.
    /// </summary>
    public OsRenderBuffer Clone()
    {
        if (_commandsLength == 0 && _stringsLength == 0)
            return Empty;

        return new OsRenderBuffer
        {
            _commandsLength = _commandsLength,
            _commands = _commands.AsSpan(0, _commandsLength).ToArray(),
            _stringsLength = _stringsLength,
            _strings = _strings.AsSpan(0, _stringsLength).ToArray()
        };
    }

    public (OsRenderCommand command, int paramCount) GetCommand(int index)
    {
        var commandHeader = (long)_commands[index];
        var paramCount = (int)(commandHeader & 0xFFFFFFFF);
        var command = (OsRenderCommand)(commandHeader >> 32);
        return (command, paramCount);
    }


    /// <summary>
    /// TBD: Remove heap allocation here by using a Span or similar. 
    /// </summary>
    public double[] GetArray(int index, int length)
    {
        var buffer = Commands;
        var result = new double[length];
        for (int i = 0; i < length; i++)
            result[i] = buffer[index + i];
        return result;
    }

    /// <summary>
    /// Adds a string to the internal string buffer and returns its index.
    /// </summary>
    int AddString(string s)
    {
        if (_stringsLength >= _strings.Length)
            Array.Resize(ref _strings, Math.Max(4, _strings.Length * 2));
        _strings[_stringsLength++] = s;
        return _stringsLength-1;
    }

    public void SetStrokeColorWidth(Color color, double width)
    {
        var s = AppendCommandSpan(OsRenderCommand.SetStrokeColorWidth, 2);
        s[0] = AddString(color.CssColor);
        s[1] = width;
    }

    public void SetFillColor(Color color)
    {
        var s = AppendCommandSpan(OsRenderCommand.SetFillColor, 1);
        s[0] = AddString(color.CssColor);
    }

    public void SetFontNameSize(string fontName, double fontSize)
    {
        var s = AppendCommandSpan(OsRenderCommand.SetFontNameSize, 2);
        s[0] = AddString(fontName);
        s[1] = fontSize;
    }


    public void FillRect( 
        double x, double y, double width, double height, double radius)
    {
        var s = AppendCommandSpan(OsRenderCommand.FillRect, 5);
        s[0] = x;
        s[1] = y;
        s[2] = width;
        s[3] = height;
        s[4] = radius;
    }

    public void StrokeRect(
        double x, double y, double width, double height, double radius)
    {
        var s = AppendCommandSpan(OsRenderCommand.StrokeRect, 5);
        s[0] = x;
        s[1] = y;
        s[2] = width;
        s[3] = height;
        s[4] = radius;
    }

    public void FillText(string text, double x, double y)
    {
        var s = AppendCommandSpan(OsRenderCommand.FillText, 3);
        s[0] = AddString(text);
        s[1] = x;
        s[2] = y;
    }

    public void StrokePolyLine(ReadOnlySpan<double> points)
    {
        var s = AppendCommandSpan(OsRenderCommand.StrokePolyLine, points.Length);
        for (var i = 0; i < points.Length; i++)
            s[i] = points[i];
    }

    public void FillPolygon(ReadOnlySpan<double> points)
    {
        var s = AppendCommandSpan(OsRenderCommand.FillPolygon, points.Length);
        for (var i = 0; i < points.Length; i++)
            s[i] = points[i];
    }

    public void Clip(Rect r) =>
        Clip(r.X, r.Y, r.Width, r.Height);

    public void Clip(double x, double y, double width, double height)
    {
        var s = AppendCommandSpan(OsRenderCommand.PushClip, 4);
        s[0] = x;
        s[1] = y;
        s[2] = width;
        s[3] = height;
    }

    public void PopClip()
    {
        _ = AppendCommandSpan(OsRenderCommand.PopClip, 0);
    }

    /// <summary>
    /// Composite renderBuffer into this buffer, applying the given offset and scale to all coordinates.
    /// Convert string indices into stringCache LRU indices.
    /// </summary>
    public void Composite(OsRenderBuffer renderBuffer, ObjectCache<string> stringCache, Point offset, double scale)
    {
        if (renderBuffer._commandsLength == 0)
            return;

        var buffer = AppendSpan(renderBuffer._commandsLength);
        renderBuffer._commands.AsSpan(0, renderBuffer._commandsLength).CopyTo(buffer);


        int pc = 0;
        while (pc < buffer.Length)
        {
            var commandHeader = (long)buffer[pc];
            var paramCount = (int)(commandHeader & 0xFFFFFFFF);
            var command = (OsRenderCommand)(commandHeader >> 32);
            var pi = pc + 1;
            pc = pc + paramCount + 1;

            switch (command)
            {
                case OsRenderCommand.SetStrokeColorWidth:
                    buffer[pi] = stringCache.GetIndex(renderBuffer._strings[(int)buffer[pi]]);
                    buffer[pi + 1] = buffer[pi + 1] * scale;
                    break;
                case OsRenderCommand.SetFillColor:
                    buffer[pi] = stringCache.GetIndex(renderBuffer._strings[(int)buffer[pi]]);
                    break;
                case OsRenderCommand.SetFontNameSize:
                    buffer[pi] = stringCache.GetIndex(renderBuffer._strings[(int)buffer[pi]]);
                    buffer[pi + 1] = buffer[pi + 1] * scale;
                    break;
                case OsRenderCommand.FillRect:
                    buffer[pi + 0] = buffer[pi + 0] * scale + offset.X;
                    buffer[pi + 1] = buffer[pi + 1] * scale + offset.Y;
                    buffer[pi + 2] = buffer[pi + 2] * scale;
                    buffer[pi + 3] = buffer[pi + 3] * scale;
                    buffer[pi + 4] = buffer[pi + 4] * scale;
                    break;
                case OsRenderCommand.StrokeRect:
                    buffer[pi + 0] = buffer[pi + 0] * scale + offset.X;
                    buffer[pi + 1] = buffer[pi + 1] * scale + offset.Y;
                    buffer[pi + 2] = buffer[pi + 2] * scale;
                    buffer[pi + 3] = buffer[pi + 3] * scale;
                    buffer[pi + 4] = buffer[pi + 4] * scale;
                    break;
                case OsRenderCommand.FillText:
                    buffer[pi] = stringCache.GetIndex(renderBuffer._strings[(int)buffer[pi]]);
                    buffer[pi + 1] = buffer[pi + 1] * scale + offset.X;
                    buffer[pi + 2] = buffer[pi + 2] * scale + offset.Y;
                    break;
                case OsRenderCommand.FillPolygon:
                case OsRenderCommand.StrokePolyLine:
                    for (var i = 0; i < paramCount; i += 2)
                    {
                        buffer[pi + i] = buffer[pi + i] * scale + offset.X;
                        buffer[pi + i + 1] = buffer[pi + i + 1] * scale + offset.Y;
                    }
                    break;
                case OsRenderCommand.PushClip:
                    buffer[pi] = buffer[pi] * scale + offset.X;
                    buffer[pi + 1] = buffer[pi + 1] * scale + offset.Y;
                    buffer[pi + 2] = buffer[pi + 2] * scale;
                    buffer[pi + 3] = buffer[pi + 3] * scale;
                    break;
                case OsRenderCommand.PopClip:
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }


        }
    }


    /// <summary>
    /// Appends a command, returns a span with the given number of parameters.
    /// </summary>
    private Span<double> AppendCommandSpan(OsRenderCommand command, int paramCount)
    {
        var s = AppendSpan(paramCount + 1);
        s[0] = ((long)command << 32) + paramCount;
        return s.Slice(1);
    }

    /// <summary>
    /// Returns a span of the given count, resizing the underlying array if needed.
    /// </summary>
    Span<double> AppendSpan(int count)
    {
        while (_commands.Length < _commandsLength + count)
            Array.Resize(ref _commands, Math.Max(4, _commands.Length * 2));

        var s = _commands.AsSpan(_commandsLength, count);
        _commandsLength += count;
        return s;
    }

}



