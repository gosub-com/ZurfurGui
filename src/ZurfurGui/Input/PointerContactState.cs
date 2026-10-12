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

    public View? CapturedView { get; private set; }

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

    // Cancels the entire press relationship for this contact. Capture cancellation represents cancellation of the
    // interaction, so leaving unrelated ancestors in the press chain would keep them visually pressed.
    public List<View> CancelPress()
    {
        var affectedViews = PressChain
            .Concat(CurrentPressChain)
            .Distinct()
            .ToList();

        PressChain.Clear();
        CurrentPressChain.Clear();
        return affectedViews;
    }

    // Replaces this contact's capture owner and returns whether ownership changed.
    public bool Capture(View view, out View? previousOwner)
    {
        previousOwner = CapturedView;
        if (CapturedView == view)
            return false;

        CapturedView = view;
        return true;
    }

    // Releases capture only when the given view currently owns it.
    public bool Release(View view)
    {
        if (CapturedView != view)
            return false;

        CapturedView = null;
        return true;
    }

    // Releases and returns this contact's capture owner, if any.
    public View? ReleaseCapture()
    {
        var capturedView = CapturedView;
        CapturedView = null;
        return capturedView;
    }

    // Clears this contact's hover, press, and capture state and returns affected views.
    public List<View> Reset()
    {
        var affectedViews = HoverChain
            .Concat(CurrentPressChain)
            .Distinct()
            .ToList();
        if (CapturedView is not null && !affectedViews.Contains(CapturedView))
            affectedViews.Add(CapturedView);

        HoverView = null;
        HoverChain.Clear();
        PressChain.Clear();
        CurrentPressChain.Clear();
        CapturedView = null;
        return affectedViews;
    }
}
