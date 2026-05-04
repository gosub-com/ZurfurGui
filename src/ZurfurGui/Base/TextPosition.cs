using System;
using System.Collections.Generic;
using System.Text;

namespace ZurfurGui.Base;

/// <summary>
/// The location of a position in text, represented as a line and column number.  
/// Line and column numbers are 0-based.  The column is an index into the UTF-16
/// enocded string (i.e. it could point into the middle of a 32 bit code point)
/// </summary>
public record struct TextPosition(int Line, int Column) : IComparable<TextPosition>
{
    public int CompareTo(TextPosition other)
    {
        var c = Line.CompareTo(other.Line);
        return c != 0 ? c : Column.CompareTo(other.Column);
    }

    public static bool operator <=(TextPosition a, TextPosition b) => a.CompareTo(b) <= 0;
    public static bool operator >=(TextPosition a, TextPosition b) => a.CompareTo(b) >= 0;
    public static bool operator <(TextPosition a, TextPosition b) => a.CompareTo(b) < 0;
    public static bool operator >(TextPosition a, TextPosition b) => a.CompareTo(b) > 0;
}
