using ZurfurGui.Base;

namespace ZurfurGui.Render;

public interface Drawable
{
    /// <summary>
    /// Name of the layout type, which must be the same for all instances of this type.
    /// </summary>
    string DrawType { get; }

    /// <summary>
    /// Draw over the panel background, but under the child views.
    /// The view and is inside the margin, but contains padding and border.
    /// The contentRect is the area inside the padding and border.
    /// </summary>
    public void Draw(View view, RenderContext context);

    /// <summary>
    /// Draw over child views.
    /// </summary>
    public void DrawOver(View view, RenderContext context) { }

    public bool IsHit(View view, Point point);

}
