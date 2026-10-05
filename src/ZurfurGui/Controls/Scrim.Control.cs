using ZurfurGui.Base;
using ZurfurGui.Input;
using ZurfurGui.Layout;
using ZurfurGui.Windows;

namespace ZurfurGui.Controls;

/// <summary>
/// A full-screen overlay used to dim content and dismiss transient UI when clicked.
/// </summary>
/// <remarks>
/// Subscribe to <see cref="Dismissed"/> to close the transient UI owned by the caller, then remove the
/// <see cref="View"/> returned by <see cref="Show"/>. The Scrim does not remove itself because the caller
/// owns the overlay's lifetime.
/// </remarks>
public sealed partial class Scrim : Controllable
{
    public event EventHandler<PointerEvent>? Dismissed;

    public Scrim()
    {
        InitializeControl();
        View.AddEvent(Panel.PointerClick, OnPointerClick);
    }

    /// <summary>
    /// Shows the Scrim as a full-screen floating panel.
    /// </summary>
    /// <param name="appWindow">The application window that owns the floating panel.</param>
    /// <returns>The Scrim view added to the application window. Remove it from its parent to close the Scrim.</returns>
    /// <remarks>
    /// Use the returned view to retain the overlay for later cleanup. For example:
    /// <code>
    /// var scrim = new Scrim();
    /// scrim.Dismissed += (_, _) =>
    /// {
    ///     scrim.View.RemoveFromParent();
    /// };
    /// var scrimView = scrim.Show(appWindow);
    /// // Remove scrimView from its parent when the owning UI closes.
    /// </code>
    /// </remarks>
    public View Show(AppWindow appWindow)
    {
        var view = appWindow.ShowFloatingPanel(this, new(0, 0));
        view.SetProperty(Panel.Align, new(AlignHorizontal.Stretch, AlignVertical.Stretch));
        return view;
    }

    void OnPointerClick(object? sender, PointerEvent e)
    {
        Dismissed?.Invoke(this, e);
    }
}
