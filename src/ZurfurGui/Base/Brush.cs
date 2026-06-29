using System;
using System.Collections.Generic;
using System.Text;

namespace ZurfurGui.Base;

public enum BrushType : byte
{
    Solid,
}

public readonly record struct Brush
{
    public BrushType Type { get; init; }
    public Color Color { get; init; }

    // Convenient constructor matching your current design
    public Brush(Color color, BrushType type = BrushType.Solid)
    {
        Color = color;
        Type = type;
    }

    /// <summary>
    /// Provides a convenient conversion from a Color to a solid Brush.
    /// </summary>
    public static implicit operator Brush(Color color) => new(color);
}

