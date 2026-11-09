using System;
using System.Collections.Generic;
using System.Text;



namespace ZurfurGui.Base;

/// <summary>Specifies the fill style used by a brush.</summary>
public enum BrushType : byte
{
    Solid,
}

/// <summary>Describes a mutable brush by its color and fill type.</summary>
public record struct Brush(Color Color, BrushType Type = BrushType.Solid)
{

    /// <summary>
    /// Provides a convenient conversion from a Color to a solid Brush.
    /// </summary>
    public static implicit operator Brush(Color color) => new(color);
}

