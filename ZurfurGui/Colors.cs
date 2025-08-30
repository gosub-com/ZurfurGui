using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui;

/// <summary>
/// Standard web color names https://en.wikipedia.org/wiki/Web_colors
/// </summary>
public static class Colors
{
    public static readonly Color White = Color.FromWebHexRgb(0xFFFFFF);
    public static readonly Color Silver = Color.FromWebHexRgb(0xC0C0C0);
    public static readonly Color Gray = Color.FromWebHexRgb(0x808080);
    public static readonly Color Black = Color.FromWebHexRgb(0x000000);
    public static readonly Color Red = Color.FromWebHexRgb(0xFF0000);
    public static readonly Color Maroon = Color.FromWebHexRgb(0x800000);
    public static readonly Color Yellow = Color.FromWebHexRgb(0xFFFF00);
    public static readonly Color Olive = Color.FromWebHexRgb(0x808000);
    public static readonly Color Lime = Color.FromWebHexRgb(0x00FF00);
    public static readonly Color Green = Color.FromWebHexRgb(0x008000);
    public static readonly Color Aqua = Color.FromWebHexRgb(0x00FFFF);
    public static readonly Color Teal = Color.FromWebHexRgb(0x008080);
    public static readonly Color Blue = Color.FromWebHexRgb(0x0000FF);
    public static readonly Color Navy = Color.FromWebHexRgb(0x000080);
    public static readonly Color Fuchsia = Color.FromWebHexRgb(0xFF00FF);
    public static readonly Color Purple = Color.FromWebHexRgb(0x800080);
    public static readonly Color Pink = Color.FromWebHexRgb(0xFFC0CB);
    public static readonly Color Orange = Color.FromWebHexRgb(0xFFA500);
    public static readonly Color Brown = Color.FromWebHexRgb(0xA52A2A);
    public static readonly Color LightGray = Color.FromWebHexRgb(0xD3D3D3);
    public static readonly Color AliceBlue = Color.FromWebHexRgb(0xF0F8FF);
    public static readonly Color LightBlue = Color.FromWebHexRgb(0xADD8E6); // Use LightSkyBlue instead
    public static readonly Color LightSkyBlue = Color.FromWebHexRgb(0x87CEFA);
    public static readonly Color DeepSkyBlue = Color.FromWebHexRgb(0x00BFFF);
    public static readonly Color DarkSlateBlue = Color.FromWebHexRgb(0x483D8B);

    public static readonly IReadOnlyDictionary<string, Color> NamedColors =
        typeof(Colors)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Color))
            .ToDictionary(f => f.Name, f => (Color)f.GetValue(null)!);
}
