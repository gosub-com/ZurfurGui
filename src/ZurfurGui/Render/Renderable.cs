using ZurfurGui.Base;
using ZurfurGui.Input;

namespace ZurfurGui.Render;

public enum RenderResult : byte
{
    /// <summary>
    /// The renderable has no visible content and the previous frame will be discarded.
    /// When returning this, the renderable should not call any render functions.
    /// </summary>
    Discard,

    /// <summary>
    /// The rendered content did not change and the previous frame will be reused.  
    /// The previous render result is also retained, so it is OK to return this when the
    /// clientClip changed but didn't affect the rendered content.
    /// When returning this, the renderable should not call any render functions.
    /// </summary>
    Keep,

    /// <summary>
    /// The entire control was rendered.
    /// When this is returned, the render functions won't be called because of changes to clientClip.
    /// </summary>
    RenderedAll,

    /// <summary>
    /// Only the un-clipped portion was rendered.
    /// When this is returned, the render functions will be called because of changes to clientClip.
    /// The ViewFlags.Render should be consulted to find out if the content has changed or it was
    /// just a clientClip change.
    /// </summary>
    RenderedClip
}

public interface Renderable
{
    /// <summary>
    /// Render over the panel background, but under the child views. The clientClip is the visible 
    /// portion of the view, which can be used to discard clipped draw commands.  This can be
    /// called when ViewFlags.Render is false and RenderedClip was returned from the previous Render call,
    /// so check that flag to determine if the content has changed or it was just a clientClip change.
    /// </summary>
    public RenderResult Render(View view, RenderContext context, Rect clientClip);

    /// <summary>
    /// Override to determine if the point hits the renderable area of the view.
    /// </summary>
    public bool IsHit(View view, HitTestContext context);


}
