using System.Collections.ObjectModel;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Input;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Windows;

namespace ZurfurGui.Controls;

/// <summary>
/// A control that lets the user select an item from a drop-down list. See
/// <see href="../../../docs/combo-box.md">docs/combo-box.md</see> for usage and implementation details.
/// </summary>
/// <typeparam name="Item">The type of item displayed in the list.</typeparam>
public sealed partial class ComboBox<Item> : Controllable
    where Item : IComboBoxItem
{

    const double MIN_DROPDOWN_HEIGHT = 300;

    View? _dropdownView;
    View? _dismissOverlay;

    /// <summary>
    /// Initializes a combo box and registers its click and data-change handlers.
    /// </summary>
    public ComboBox()
    {
        InitializeControl();
        View.AddGestureRecognizer(new ClickRecognizer(OnClick));
        DataContext.PropertyChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "SelectedIndex" || e.PropertyName == "" || e.PropertyName == null)
            SyncSelectedItem();
    }

    Controllable CreateItemController(Item itemData)
    {
        var controller = global::ZurfurGui.Loader.CreateDataController(itemData);
        return controller;
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
        var logicalTop = origin.Y / scale;
        var logicalBottom = logicalTop + size.Height;

        // Max height is about half the window minus the combo box, but at least MIN_DROPDOWN_HEIGHT.
        // Always drop down, except when it can't fit below, then drop up.
        var windowHeight = appWindow.View.Size.Height;
        var maxHeight = Math.Max((windowHeight - size.Height) / 2, MIN_DROPDOWN_HEIGHT);
        var spaceBelow = windowHeight - logicalBottom;
        var placeBelow = spaceBelow >= maxHeight || spaceBelow >= logicalTop;
        maxHeight = Math.Min(maxHeight, Math.Max(placeBelow ? spaceBelow : logicalTop, 0));

        // Full-screen transparent overlay catches clicks outside the dropdown.
        // BackgroundColor alpha must be > 16 to pass the panel hit test (see DrawHelper.ALPHA_HIT_THRESHOLD).
        // Align must be set AFTER ShowFloatingPanel because ShowFloatingPanel overwrites it with Left/Top.
        var scrim = new Scrim();
        scrim.Dismissed += (_, _) => CloseDropdown();
        _dismissOverlay = scrim.Show(appWindow);

        var popup = new ListBox<Item>();
        foreach (var itemData in DataContext.Items)
            popup.DataContext.Items.Add(itemData);
        popup.ItemClicked += (_, index) =>
        {
            DataContext.SelectedIndex = index;
            SyncSelectedItem();
            CloseDropdown();
        };

        var offsetY = placeBelow ? logicalBottom : logicalTop - windowHeight;
        _dropdownView = appWindow.ShowFloatingPanel(popup, new(logicalX, offsetY));
        _dropdownView.SetProperty(Panel.SizeMax, new SizeProp(default, maxHeight));
        _dropdownView.SetProperty(Panel.SizeMin, new SizeProp(size.Width, default));

        // Drop up by bottom aligning in the window, then offsetting so the bottom is at the top
        // of the combo box.  Align is set AFTER ShowFloatingPanel because it overwrites it.
        if (!placeBelow)
            _dropdownView.SetProperty(Panel.Align, new(AlignHorizontal.Left, AlignVertical.Bottom));

        var selected = DataContext.SelectedIndex;
        if (selected.HasValue && selected.Value >= 0 && selected.Value < DataContext.Items.Count)
            popup.Select(selected.Value);
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

    void Controllable.OnAttach() { }

    void Controllable.OnDetach()
    {
        CloseDropdown();
    }
}
