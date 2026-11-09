using System;
using System.Collections.Generic;
using System.Text;

namespace ZurfurGui.Base;

/// <summary>Defines a brush and thickness used to draw outlines.</summary>
public readonly record struct Pen(Brush Brush, double Thickness);
