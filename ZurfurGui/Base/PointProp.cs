using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Base;

/// <summary>
/// A type-safe Point that allows each component to be optional.  Defaults to null.
/// </summary>
public readonly record struct PointProp(DoubleProp X, DoubleProp Y)
{
    public PointProp() : this(new(), new()) { }
    public override string ToString() => $"X: {X}, Y: {Y}";
    public string ToString(string format) => $"X: {X.ToString(format)}, Y: {Y.ToString(format)}";

    /// <summary>
    /// Replace null components with v.
    /// </summary>
    public Point Or(double v) => new Point(X.Or(v), Y.Or(v));

    /// <summary>
    /// Replace null components with v.
    /// </summary>
    public Point Or(Point v) => new Point(X.Or(v.X), Y.Or(v.Y));

    /// <summary>
    /// Implicit conversion from Point to PointQ.
    /// </summary>
    public static implicit operator PointProp(Point value) => new PointProp(value.X, value.Y);

    /// <summary>
    /// Implicit conversion from Point? to PointQ.
    /// </summary>
    public static implicit operator PointProp(Point? value) => value.HasValue ? value : new PointProp();


}
