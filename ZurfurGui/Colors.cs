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
    public static Color White => Color.FromHex(0xFFFFFF);
    public static Color Silver => Color.FromHex(0xC0C0C0);
    public static Color Gray => Color.FromHex(0x808080);
    public static Color Black => Color.FromHex(0x000000);
    public static Color Red => Color.FromHex(0xFF0000);
    public static Color Maroon => Color.FromHex(0x800000);
    public static Color Yellow => Color.FromHex(0xFFFF00);
    public static Color Olive => Color.FromHex(0x808000);
    public static Color Lime => Color.FromHex(0x00FF00);
    public static Color Green => Color.FromHex(0x008000);
    public static Color Aqua => Color.FromHex(0x00FFFF);
    public static Color Teal => Color.FromHex(0x008080);
    public static Color Blue => Color.FromHex(0x0000FF);
    public static Color Navy => Color.FromHex(0x000080);
    public static Color Fuchsia => Color.FromHex(0xFF00FF);
    public static Color Purple => Color.FromHex(0x800080);
    public static Color Pink => Color.FromHex(0xFFC0CB);
    public static Color Orange => Color.FromHex(0xFFA500);
    public static Color Brown => Color.FromHex(0xA52A2A);
    public static Color LightGray => Color.FromHex(0xD3D3D3);
}
