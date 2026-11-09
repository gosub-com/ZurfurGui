using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ZurfurGui.Base;

/// <summary>Specifies horizontal alignment within a layout area.</summary>
public enum AlignHorizontal : byte
{
    Stretch,
    Left,
    Center,
    Right
}

/// <summary>Specifies vertical alignment within a layout area.</summary>
public enum AlignVertical : byte
{
    Stretch,
    Top,
    Center,
    Bottom
}
