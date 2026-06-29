using System;
using System.Collections.Generic;
using System.Text;
using ZurfurGui.Base;

namespace ZurfurGui.Input;

/// <summary>
/// Use View.ToClinet to convert DevicePosition to view position
/// </summary>
public record class PointerEvent(string Type, Point DevicePosition);
