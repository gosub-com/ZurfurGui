using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Base;

/// <summary>
/// A type-safe Point that allows each component to be optional.  Defaults to null.
/// </summary>
public readonly record struct ThicknessProp(DoubleProp Left, DoubleProp Top, DoubleProp Right, DoubleProp Bottom)
{
    public ThicknessProp() : this(new (), new (), new(), new()) { }
    public override string ToString() => $"Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}";
    public string ToString(string format) => $"Left: {Left.ToString(format)}, Top: {Top.ToString(format)}, "
        + $"Right: {Right.ToString(format)}, Bottom: {Bottom.ToString(format)}";

    /// <summary>
    /// Replace null components with v.
    /// </summary>
    public Thickness Or(double v) => new Thickness(Left.Or(v), Top.Or(v), Right.Or(v), Bottom.Or(v));

    /// <summary>
    /// Replace null components with v.
    /// </summary>
    public Thickness Or(Thickness v) => new Thickness(Left.Or(v.Left), Top.Or(v.Top), Right.Or(v.Top), Bottom.Or(v.Bottom));

    /// <summary>
    /// Implicit conversion from Thickness to ThicknessQ.
    /// </summary>
    public static implicit operator ThicknessProp(Thickness v) => new ThicknessProp(v.Left, v.Top, v.Right, v.Bottom);

    /// <summary>
    /// Implicit conversion from Thickness? to ThicknessQ.
    /// </summary>
    public static implicit operator ThicknessProp(Thickness? v) => v.HasValue ? v : new ThicknessProp();

}
