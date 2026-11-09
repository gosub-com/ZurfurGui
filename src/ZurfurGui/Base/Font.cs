using System;
using System.Collections.Generic;
using System.Text;

namespace ZurfurGui.Base;

/// <summary>Describes a font by its name and size.</summary>
public readonly record struct Font(string Name, double Size);
