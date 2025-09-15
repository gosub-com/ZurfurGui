using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZurfurGui.Base;

namespace ZurfurGui.Styles;

/// <summary>
/// This is like a css file
/// </summary>
public record class StyleSheet
{
    /// <summary>
    /// Unique name of style sheet, or "" if not named
    /// </summary>
    public string Name { get; init; } = "";
    public StyleProperty[] Styles { get; init; } = [];
}


