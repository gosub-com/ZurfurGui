using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;

namespace ZurfurGui.Styles;

public class StyleProperty
{
    public required TextLines Selector { get; init; } = [];
    public Properties Properties { get; init; } = [];
}

