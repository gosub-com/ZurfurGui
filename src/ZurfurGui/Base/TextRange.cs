using System;
using System.Collections.Generic;
using System.Text;

namespace ZurfurGui.Base;

/// <summary>
/// Selected text range.
/// Start is inclusive and End is exclusive.
/// The constructor normalizes the range so that Start &lt;= End.
/// </summary>
public readonly struct TextRange : IEquatable<TextRange>
{
    public TextPosition Start { get; }
    public TextPosition End { get; }

    public TextRange(TextPosition start, TextPosition end)
    {
        if (end < start)
            (start, end) = (end, start);

        Start = start;
        End = end;
    }

    public bool Equals(TextRange other) => Start.Equals(other.Start) && End.Equals(other.End);
    public override bool Equals(object? obj) => obj is TextRange other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(TextRange left, TextRange right) => left.Equals(right);
    public static bool operator !=(TextRange left, TextRange right) => !left.Equals(right);
}

