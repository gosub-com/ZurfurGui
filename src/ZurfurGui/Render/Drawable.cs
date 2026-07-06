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

    /// <summary>
    /// Override to determine if the point hits the drawable area of the view.
    /// </summary>
    public bool IsHit(View view, Point point);


    /// <summary>
    /// We don't have any concept of visual bounds yet.
    /// So, be sure to clip if you can't garauntee you're drawing inside the control.
    /// </summary>
    public bool PromiseToDrawInsideControl {  get; }

}
