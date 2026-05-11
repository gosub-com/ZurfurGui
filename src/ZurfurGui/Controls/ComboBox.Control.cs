using System.Collections.ObjectModel;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Layout;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Windows;

namespace ZurfurGui.Controls;

/// <summary>
/// Non-generic companion class holding PropertyKey fields for ComboBox&lt;Item&gt;.
/// Keys must live here rather than on the generic class to avoid per-closed-type
/// static field duplication (which would cause duplicate PropertyKey registration).
/// </summary>
public static partial class ComboBox
{
    public static readonly PropertyKey<Color> ScrimColor = new("ComboBox.scrimColor", typeof(ComboBox<>), new Color(0, 0, 0, 20), ViewFlags.ReDraw);
}

public sealed partial class ComboBox<Item> : Controllable
    where Item : IComboBoxItemData
{

    View? _dropdownView;
    View? _dismissOverlay;

    public ComboBox()
    {
        InitializeControl();
        View.AddEvent(Panel.PointerClick, OnClick);
        DataContext.PropertyChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "SelectedIndex" || e.PropertyName == "" || e.PropertyName == null)
            SyncSelectedItem();
    }

    Controllable CreateItemController(IComboBoxItemData itemData)
    {
        var controller = global::ZurfurGui.Loader.CreateDataController<IComboBoxItem>(itemData);
        controller.DataContext = itemData;
        return (Controllable)controller;
    }

    void SyncSelectedItem()
    {
        var idx = DataContext.SelectedIndex;
        var items = DataContext.Items;
        _selectedItem.View.ClearChildren();
        if (idx.HasValue && idx.Value >= 0 && idx.Value < items.Count)
        {
            var item = CreateItemController(items[idx.Value]);
            _selectedItem.View.AddChild(item.View);
        }
    }

    void OnClick(object? sender, PointerEvent e)
    {
        var appWindow = View.AppWindow;
        if (appWindow == null)
            return;

        if (_dropdownView != null)
        {
            CloseDropdown();
            return;
        }

        OpenDropdown(appWindow);
    }

    void OpenDropdown(AppWindow appWindow)
    {
        var origin = View.Origin;
        var size = View.Size;
        var scale = View.Scale;

        // origin is in device pixels, size is in logical pixels
        var logicalX = origin.X / scale;
        var logicalY = origin.Y / scale + size.Height;

        // Full-screen transparent overlay catches clicks outside the dropdown.
        // BackgroundColor alpha must be > 16 to pass the panel hit test (see DrawHelper.ALPHA_HIT_THRESHOLD).
        // Align must be set AFTER ShowFloatingPanel because ShowFloatingPanel overwrites it with Left/Top.
        var overlay = new Panel();
        overlay.View.SetProperty(Panel.BackgroundColor, View.GetStyle(ComboBox.ScrimColor));
        overlay.View.AddEvent(Panel.PointerClick, (s, ev) => CloseDropdown());
        appWindow.ShowFloatingPanel(overlay, new(0, 0));
        overlay.View.SetProperty(Panel.Align, new(AlignHorizontal.Stretch, AlignVertical.Stretch));
        _dismissOverlay = overlay.View;

        var popup = new Panel();
        popup.View.Layout = new LayoutColumn();
        popup.View.SetProperty(Panel.Classes, ["ComboBox.Dropdown"]);
        popup.View.SetProperty(Panel.Align, new(AlignHorizontal.Left, AlignVertical.Top));

        // The popup is parented to _floatingWindows under AppWindow, so it is outside the
        // originating form's view subtree.  Style lookup walks ancestors looking for UseStyles,
        // and would only find AppWindow's built-in sheets (e.g. "ZurfurDefault").  Any custom
        // item stylesheet declared on the form (e.g. "ComboBoxItemBadge") would be invisible to
        // the popup items.  Collect every UseStyles name from the ComboBox's own ancestor chain
        // and stamp them onto the popup so its items can resolve their styles correctly.
        var collectedStyles = new List<string>();
        for (var v = View; v != null; v = v.Parent)
        {
            var ancestorStyles = v.GetProperty(Panel.UseStyles);
            if (ancestorStyles != null)
                foreach (var name in ancestorStyles)
                    if (!collectedStyles.Contains(name))
                        collectedStyles.Add(name);
        }
        if (collectedStyles.Count > 0)
            popup.View.SetProperty(Panel.UseStyles, new TextLines([.. collectedStyles]));

        var items = DataContext.Items;
        for (int i = 0; i < items.Count; i++)
        {
            var index = i;
            var itemData = items[i];
            var item = CreateItemController(itemData);

            // Wrap each item in a thin host panel that owns the dropdown-specific class and click
            // handler.  This keeps the item's root view untouched so its own classes and styles
            // (e.g. "ComboBoxItemBadge") are fully preserved.
            var wrapper = new Panel();
            wrapper.View.SetProperty(Panel.Classes, ["ComboBox.DropdownItem"]);
            wrapper.View.AddEvent(Panel.PointerClick, (s, ev) =>
            {
                DataContext.SelectedIndex = index;
                SyncSelectedItem();
                CloseDropdown();
            });
            wrapper.View.AddChild(item.View);
            popup.View.AddChild(wrapper.View);
        }

        _dropdownView = appWindow.ShowFloatingPanel(popup, new(logicalX, logicalY));
    }

    void CloseDropdown()
    {
        if (_dismissOverlay != null)
        {
            _dismissOverlay.RemoveFromParent();
            _dismissOverlay = null;
        }
        if (_dropdownView != null)
        {
            _dropdownView.RemoveFromParent();
            _dropdownView = null;
        }
    }

    public void OnAttach() { }
    public void OnDetach()
    {
        CloseDropdown();
    }
}
