using ZurfurGui.Base;

namespace ZurfurGui.Input;

internal readonly record struct PointerContactKey(PointerDeviceKind Device, int Id);

internal sealed class PointerContactState
{
    public PointerContact Contact { get; set; }

    public View? HoverView { get; set; }

    public List<View> HoverChain { get; } = new();

    public List<View> PressChain { get; } = new();

    public List<View> CurrentPressChain { get; } = new();

    public List<View> CapturedViews { get; } = new();

    // Replaces the views pressed by this contact.
    public void SetPressChain(IEnumerable<View> chain)
    {
        PressChain.Clear();
        PressChain.AddRange(chain);
    }

    // Replaces this contact's hover target and chain, then updates its current press chain.
    public void UpdateHover(View? view, IEnumerable<View> chain)
    {
        HoverView = view;
        HoverChain.Clear();
        HoverChain.AddRange(chain);
        UpdateCurrentPressChain();
    }

    // Rebuilds the pressed views shared by this contact's hover and press chains.
    public void UpdateCurrentPressChain()
    {
        CurrentPressChain.Clear();
        foreach (var view in HoverChain)
        {
            if (PressChain.Contains(view))
                CurrentPressChain.Add(view);
        }
    }

    // Captures a view for this contact and reports whether capture was newly added.
    public bool Capture(View view)
    {
        if (CapturedViews.Contains(view))
            return false;

        CapturedViews.Add(view);
        return true;
    }

    // Releases a view for this contact and reports whether it was captured.
    public bool Release(View view)
    {
        return CapturedViews.Remove(view);
    }

    // Releases all views captured by this contact and returns the released views.
    public List<View> ReleaseAllCapture()
    {
        var capturedViews = CapturedViews.ToList();
        CapturedViews.Clear();
        return capturedViews;
    }

    // Clears this contact's hover, press, and capture state and returns affected views.
    public List<View> Reset()
    {
        var affectedViews = HoverChain
            .Concat(CurrentPressChain)
            .Concat(CapturedViews)
            .Distinct()
            .ToList();

        HoverView = null;
        HoverChain.Clear();
        PressChain.Clear();
        CurrentPressChain.Clear();
        CapturedViews.Clear();
        return affectedViews;
    }
}
