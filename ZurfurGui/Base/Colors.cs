using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Base;

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
    public static readonly Color LightBlue = Color.FromWebHexRgb(0xADD8E6);
    public static readonly Color LightSkyBlue = Color.FromWebHexRgb(0x87CEFA);
    public static readonly Color DeepSkyBlue = Color.FromWebHexRgb(0x00BFFF);
    public static readonly Color DarkSlateBlue = Color.FromWebHexRgb(0x483D8B);

    // Additional web colors
    public static readonly Color TransParent = new();
    public static readonly Color Beige = Color.FromWebHexRgb(0xF5F5DC);
    public static readonly Color Coral = Color.FromWebHexRgb(0xFF7F50);
    public static readonly Color Crimson = Color.FromWebHexRgb(0xDC143C);
    public static readonly Color Cyan = Color.FromWebHexRgb(0x00FFFF); // Same as Aqua
    public static readonly Color DarkBlue = Color.FromWebHexRgb(0x00008B);
    public static readonly Color DarkCyan = Color.FromWebHexRgb(0x008B8B);
    public static readonly Color DarkGoldenRod = Color.FromWebHexRgb(0xB8860B);
    public static readonly Color DarkGray = Color.FromWebHexRgb(0xA9A9A9);
    public static readonly Color DarkGreen = Color.FromWebHexRgb(0x006400);
    public static readonly Color DarkKhaki = Color.FromWebHexRgb(0xBDB76B);
    public static readonly Color DarkMagenta = Color.FromWebHexRgb(0x8B008B);
    public static readonly Color DarkOliveGreen = Color.FromWebHexRgb(0x556B2F);
    public static readonly Color DarkOrange = Color.FromWebHexRgb(0xFF8C00);
    public static readonly Color DarkOrchid = Color.FromWebHexRgb(0x9932CC);
    public static readonly Color DarkRed = Color.FromWebHexRgb(0x8B0000);
    public static readonly Color DarkSalmon = Color.FromWebHexRgb(0xE9967A);
    public static readonly Color DarkSeaGreen = Color.FromWebHexRgb(0x8FBC8F);
    public static readonly Color DarkSlateGray = Color.FromWebHexRgb(0x2F4F4F);
    public static readonly Color DarkTurquoise = Color.FromWebHexRgb(0x00CED1);
    public static readonly Color DarkViolet = Color.FromWebHexRgb(0x9400D3);
    public static readonly Color Gold = Color.FromWebHexRgb(0xFFD700);
    public static readonly Color GoldenRod = Color.FromWebHexRgb(0xDAA520);
    public static readonly Color Indigo = Color.FromWebHexRgb(0x4B0082);
    public static readonly Color Ivory = Color.FromWebHexRgb(0xFFFFF0);
    public static readonly Color Khaki = Color.FromWebHexRgb(0xF0E68C);
    public static readonly Color Lavender = Color.FromWebHexRgb(0xE6E6FA);
    public static readonly Color LavenderBlush = Color.FromWebHexRgb(0xFFF0F5);
    public static readonly Color LemonChiffon = Color.FromWebHexRgb(0xFFFACD);
    public static readonly Color LightCoral = Color.FromWebHexRgb(0xF08080);
    public static readonly Color LightCyan = Color.FromWebHexRgb(0xE0FFFF);
    public static readonly Color LightGoldenRodYellow = Color.FromWebHexRgb(0xFAFAD2);
    public static readonly Color LightGreen = Color.FromWebHexRgb(0x90EE90);
    public static readonly Color LightPink = Color.FromWebHexRgb(0xFFB6C1);
    public static readonly Color LightSalmon = Color.FromWebHexRgb(0xFFA07A);
    public static readonly Color LightSeaGreen = Color.FromWebHexRgb(0x20B2AA);
    public static readonly Color LightSlateGray = Color.FromWebHexRgb(0x778899);
    public static readonly Color LightSteelBlue = Color.FromWebHexRgb(0xB0C4DE);
    public static readonly Color MediumBlue = Color.FromWebHexRgb(0x0000CD);
    public static readonly Color MediumOrchid = Color.FromWebHexRgb(0xBA55D3);
    public static readonly Color MediumPurple = Color.FromWebHexRgb(0x9370DB);
    public static readonly Color MediumSeaGreen = Color.FromWebHexRgb(0x3CB371);
    public static readonly Color MediumSlateBlue = Color.FromWebHexRgb(0x7B68EE);
    public static readonly Color MediumSpringGreen = Color.FromWebHexRgb(0x00FA9A);
    public static readonly Color MediumTurquoise = Color.FromWebHexRgb(0x48D1CC);
    public static readonly Color MediumVioletRed = Color.FromWebHexRgb(0xC71585);
    public static readonly Color MidnightBlue = Color.FromWebHexRgb(0x191970);
    public static readonly Color MintCream = Color.FromWebHexRgb(0xF5FFFA);
    public static readonly Color MistyRose = Color.FromWebHexRgb(0xFFE4E1);
    public static readonly Color Moccasin = Color.FromWebHexRgb(0xFFE4B5);
    public static readonly Color NavajoWhite = Color.FromWebHexRgb(0xFFDEAD);
    public static readonly Color OldLace = Color.FromWebHexRgb(0xFDF5E6);
    public static readonly Color PaleGoldenRod = Color.FromWebHexRgb(0xEEE8AA);
    public static readonly Color PaleGreen = Color.FromWebHexRgb(0x98FB98);
    public static readonly Color PaleTurquoise = Color.FromWebHexRgb(0xAFEEEE);
    public static readonly Color PaleVioletRed = Color.FromWebHexRgb(0xDB7093);
    public static readonly Color PapayaWhip = Color.FromWebHexRgb(0xFFEFD5);
    public static readonly Color PeachPuff = Color.FromWebHexRgb(0xFFDAB9);
    public static readonly Color Peru = Color.FromWebHexRgb(0xCD853F);
    public static readonly Color Plum = Color.FromWebHexRgb(0xDDA0DD);
    public static readonly Color PowderBlue = Color.FromWebHexRgb(0xB0E0E6);
    public static readonly Color RosyBrown = Color.FromWebHexRgb(0xBC8F8F);
    public static readonly Color RoyalBlue = Color.FromWebHexRgb(0x4169E1);
    public static readonly Color SaddleBrown = Color.FromWebHexRgb(0x8B4513);
    public static readonly Color Salmon = Color.FromWebHexRgb(0xFA8072);
    public static readonly Color SandyBrown = Color.FromWebHexRgb(0xF4A460);
    public static readonly Color SeaGreen = Color.FromWebHexRgb(0x2E8B57);
    public static readonly Color SeaShell = Color.FromWebHexRgb(0xFFF5EE);
    public static readonly Color Sienna = Color.FromWebHexRgb(0xA0522D);
    public static readonly Color SkyBlue = Color.FromWebHexRgb(0x87CEEB);
    public static readonly Color SlateBlue = Color.FromWebHexRgb(0x6A5ACD);
    public static readonly Color SlateGray = Color.FromWebHexRgb(0x708090);
    public static readonly Color Snow = Color.FromWebHexRgb(0xFFFAFA);
    public static readonly Color SpringGreen = Color.FromWebHexRgb(0x00FF7F);
    public static readonly Color SteelBlue = Color.FromWebHexRgb(0x4682B4);
    public static readonly Color Tan = Color.FromWebHexRgb(0xD2B48C);
    public static readonly Color Thistle = Color.FromWebHexRgb(0xD8BFD8);
    public static readonly Color Tomato = Color.FromWebHexRgb(0xFF6347);
    public static readonly Color Turquoise = Color.FromWebHexRgb(0x40E0D0);
    public static readonly Color Violet = Color.FromWebHexRgb(0xEE82EE);
    public static readonly Color Wheat = Color.FromWebHexRgb(0xF5DEB3);
    public static readonly Color WhiteSmoke = Color.FromWebHexRgb(0xF5F5F5);
    public static readonly Color YellowGreen = Color.FromWebHexRgb(0x9ACD32);
    public static readonly Color AntiqueWhite = Color.FromWebHexRgb(0xFAEBD7);
    public static readonly Color Aquamarine = Color.FromWebHexRgb(0x7FFFD4);
    public static readonly Color Azure = Color.FromWebHexRgb(0xF0FFFF);
    public static readonly Color Bisque = Color.FromWebHexRgb(0xFFE4C4);
    public static readonly Color BlanchedAlmond = Color.FromWebHexRgb(0xFFEBCD);
    public static readonly Color BlueViolet = Color.FromWebHexRgb(0x8A2BE2);
    public static readonly Color BurlyWood = Color.FromWebHexRgb(0xDEB887);
    public static readonly Color CadetBlue = Color.FromWebHexRgb(0x5F9EA0);
    public static readonly Color Chartreuse = Color.FromWebHexRgb(0x7FFF00);
    public static readonly Color Chocolate = Color.FromWebHexRgb(0xD2691E);
    public static readonly Color CornflowerBlue = Color.FromWebHexRgb(0x6495ED);
    public static readonly Color Cornsilk = Color.FromWebHexRgb(0xFFF8DC);
    public static readonly Color DeepPink = Color.FromWebHexRgb(0xFF1493);
    public static readonly Color DimGray = Color.FromWebHexRgb(0x696969);
    public static readonly Color DodgerBlue = Color.FromWebHexRgb(0x1E90FF);
    public static readonly Color FireBrick = Color.FromWebHexRgb(0xB22222);
    public static readonly Color FloralWhite = Color.FromWebHexRgb(0xFFFAF0);
    public static readonly Color ForestGreen = Color.FromWebHexRgb(0x228B22);
    public static readonly Color Gainsboro = Color.FromWebHexRgb(0xDCDCDC);
    public static readonly Color GhostWhite = Color.FromWebHexRgb(0xF8F8FF);
    public static readonly Color GreenYellow = Color.FromWebHexRgb(0xADFF2F);
    public static readonly Color HoneyDew = Color.FromWebHexRgb(0xF0FFF0);
    public static readonly Color HotPink = Color.FromWebHexRgb(0xFF69B4);
    public static readonly Color IndianRed = Color.FromWebHexRgb(0xCD5C5C);
    public static readonly Color LawnGreen = Color.FromWebHexRgb(0x7CFC00);
    public static readonly Color LightYellow = Color.FromWebHexRgb(0xFFFFE0);
    public static readonly Color LimeGreen = Color.FromWebHexRgb(0x32CD32);
    public static readonly Color Linen = Color.FromWebHexRgb(0xFAF0E6);
    public static readonly Color Magenta = Color.FromWebHexRgb(0xFF00FF); // Same as Fuchsia
    public static readonly Color MediumAquaMarine = Color.FromWebHexRgb(0x66CDAA);
    public static readonly Color OrangeRed = Color.FromWebHexRgb(0xFF4500);
    public static readonly Color Orchid = Color.FromWebHexRgb(0xDA70D6);
    public static readonly Color RebeccaPurple = Color.FromWebHexRgb(0x663399);

    public static readonly IReadOnlyDictionary<string, Color> NamedColors =
        typeof(Colors)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Color))
            .ToDictionary(f => f.Name, f => (Color)f.GetValue(null)!);
}
