using ZurfurGui.Base;

namespace ZurfurGui.Render;

public interface Renderable
{
    /// <summary>
    /// Name of the layout type, which must be the same for all instances of this type.
    /// </summary>
    string RenderType { get; }

    /// <summary>
    /// Render over the panel background, but under the child views.
    /// The view and is inside the margin, but contains padding and border.
    /// The contentRect is the area inside the padding and border.
    /// </summary>
    public void Render(View view, RenderContext context);

    /// <summary>
    /// Render over child views.
    /// </summary>
    public void RenderOver(View view, RenderContext context) { }

    /// <summary>
    /// Override to determine if the point hits the renderable area of the view.
    /// </summary>
    public bool IsHit(View view, Point point);


}
