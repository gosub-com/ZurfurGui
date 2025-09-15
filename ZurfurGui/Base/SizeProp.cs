using System;

namespace ZurfurGui.Base;

/// <summary>
/// A type-safe Size that allows each component to be optional.  Defaults to null.
/// </summary>
public readonly record struct SizeProp(DoubleProp Width, DoubleProp Height)
{
    public SizeProp() : this(new(), new()) { }
    public override string ToString() => $"Width: {Width}, Height: {Height}";
    public string ToString(string format) => $"Width: {Width.ToString(format)}, Height: {Height.ToString(format)}";

    /// <summary>
    /// Replace null components with v.
    /// </summary>
    public Size Or(double v) => new Size(Width.Or(v), Height.Or(v));

    /// <summary>
    /// Replace null components with v.
    /// </summary>
    public Size Or(Size v) => new Size(Width.Or(v.Width), Height.Or(v.Height));

    /// <summary>
    /// Implicit conversion from Size to SizeQ.
    /// </summary>
    public static implicit operator SizeProp(Size value) => new SizeProp(value.Width, value.Height);

    /// <summary>
    /// Implicit conversion from Size? to SizeQ.
    /// </summary>
    public static implicit operator SizeProp(Size? value) => value.HasValue ? value : new SizeProp();


}
