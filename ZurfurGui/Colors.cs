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
    public static readonly Color White = Color.FromHex(0xFFFFFF);
    public static readonly Color Silver = Color.FromHex(0xC0C0C0);
    public static readonly Color Gray = Color.FromHex(0x808080);
    public static readonly Color Black = Color.FromHex(0x000000);
    public static readonly Color Red = Color.FromHex(0xFF0000);
    public static readonly Color Maroon = Color.FromHex(0x800000);
    public static readonly Color Yellow = Color.FromHex(0xFFFF00);
    public static readonly Color Olive = Color.FromHex(0x808000);
    public static readonly Color Lime = Color.FromHex(0x00FF00);
    public static readonly Color Green = Color.FromHex(0x008000);
    public static readonly Color Aqua = Color.FromHex(0x00FFFF);
    public static readonly Color Teal = Color.FromHex(0x008080);
    public static readonly Color Blue = Color.FromHex(0x0000FF);
    public static readonly Color Navy = Color.FromHex(0x000080);
    public static readonly Color Fuchsia = Color.FromHex(0xFF00FF);
    public static readonly Color Purple = Color.FromHex(0x800080);
    public static readonly Color Pink = Color.FromHex(0xFFC0CB);
    public static readonly Color Orange = Color.FromHex(0xFFA500);
    public static readonly Color Brown = Color.FromHex(0xA52A2A);
    public static readonly Color LightGray = Color.FromHex(0xD3D3D3);
    public static readonly Color AliceBlue = Color.FromHex(0xF0F8FF);
    public static readonly Color LightBlue = Color.FromHex(0xADD8E6); // Use LightSkyBlue instead
    public static readonly Color LightSkyBlue = Color.FromHex(0x87CEFA);
    public static readonly Color DeepSkyBlue = Color.FromHex(0x00BFFF);
    public static readonly Color DarkSlateBlue = Color.FromHex(0x483D8B);
}
