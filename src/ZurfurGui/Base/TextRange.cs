namespace ZurfurGui.Base;

/// <summary>
/// Selected text range.
/// Start is inclusive and End is exclusive.
/// The constructor normalizes the range so that Start &lt;= End.
/// </summary>
public readonly record struct TextRange
{
    /// <summary>Gets the inclusive start position of the range.</summary>
    public TextPosition Start { get; }

    /// <summary>Gets the exclusive end position of the range.</summary>
    public TextPosition End { get; }

    /// <summary>
    /// This normlizes the start and end so start is always &lt;= to end
    /// </summary>
    public TextRange(TextPosition start, TextPosition end)
    {
        if (end < start)
            (start, end) = (end, start);

        Start = start;
        End = end;
    }

}

