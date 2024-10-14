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

    Color(uint webHexColor)
    {
        _rgba = (webHexColor >> 16) & 0xFF
            | (webHexColor & 0xFF00)
            | ((webHexColor & 0xFF) << 16)
            | 0xFF000000;
    }

    /// <summary>
    /// Create an opaque color from web hexadecimal value, eg. red=0xFF0000, lime/green = 0x00FF00, blue = 0x0000FF
    /// </summary>
    public static Color FromHex(uint webHexColor)
        => new Color(webHexColor);

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
}
