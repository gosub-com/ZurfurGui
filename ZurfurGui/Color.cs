using static ZurfurGui.Helpers;

namespace ZurfurGui;


/// <summary>
/// Represents a 32 bit color in RGBA format
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    // TBD: Make sure this is compatible with canvas pixel format in webassembly, which should be RGBA (little endian)
    //      https://developer.mozilla.org/en-US/docs/Web/API/Canvas_API/Tutorial/Pixel_manipulation_with_canvas
    readonly uint _rgba;

    public Color(int r, int g, int b, int a = 255) 
    {
        _rgba = ((uint)r&255) | ((uint)(g&255) << 8) | ((uint)(b&255) << 16) | (uint)((a&255) << 24);
    }

    // Create color from hex ABGR, a=0xFF000000, b=0x00FF0000, g=0x0000FF00, r=0x000000FF
    Color(uint color)
    {
        _rgba = color;
    }

    /// <summary>
    /// Create an opaque color from web hexadecimal value, eg. r=0xFF0000, g=0x00FF00, b=0x0000FF
    /// </summary>
    public static Color FromWebHexRgb(uint webHexColor)
        => new Color((webHexColor >> 16) & 0xFF // Red
            | (webHexColor & 0xFF00) // Green
            | ((webHexColor & 0xFF) << 16) // Blue
            | 0xFF000000);

    /// <summary>
    /// Create an alpha color from web hexadecimal value, eg. r=0xFF000000, g=0x00FF0000, b=0x0000FF00, a=0x000000FF
    /// </summary>
    public static Color FromWebHexArgb(uint webHexColor)
        => new Color((webHexColor >> 24) & 0xFF // Red
            | ((webHexColor >> 8) & 0xFF00) // Green
            | ((webHexColor & 0xFF00) << 8) // Blue
            | (webHexColor << 24)); // Alpha


    public int A => (int)((_rgba >> 24) & 0xff);
    public int B => (int)((_rgba >> 16) & 0xff);
    public int G => (int)((_rgba >> 8) & 0xff);
    public int R => (int)((_rgba) & 0xff);

    public bool Equals(Color v) => _rgba == v._rgba;
    public override bool Equals(object? obj) => obj is Color v && Equals(v);
    public static bool operator ==(Color a, Color b) => a.Equals(b);
    public static bool operator !=(Color a, Color b) => !a.Equals(b);
    public override string ToString() => $"{R},{G},{B},{A}";
    public override int GetHashCode() => HashMix((int)_rgba);
    public string CssColor => $"#{R:x2}{G:x2}{B:x2}{A:x2}";

    public static Color? ParseCss(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        value = value.Trim();

        if (value.StartsWith("#"))
        {
            value = value[1..];
            if (value.Length == 6)
            {
                if (uint.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out var hex))
                    return FromWebHexRgb(hex);
            }
            else if (value.Length == 8)
            {
                if (uint.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out var hex))
                    return FromWebHexArgb(hex);
            }
            return null;
        }

        if (Colors.NamedColors.TryGetValue(value, out var named))
            return named;

        var parts = value.Split(',');
        if (parts.Length == 3 || parts.Length == 4)
        {
            if (int.TryParse(parts[0], out var r)
                && int.TryParse(parts[1], out var g)
                && int.TryParse(parts[2], out var b))
            {
                int a = 255;
                if (parts.Length == 4 && !int.TryParse(parts[3], out a))
                    return null;
                return new Color(r, g, b, a);
            }
        }
        return null;
    }

}
