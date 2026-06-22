using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Property;
using ZurfurGui.Windows;

using TestApp.Test.Controls;
namespace TestApp.Test;

public partial class FormTestComboBox
{
    public FormTestComboBox()
    {
        InitializeControl();

        // Setup theme picker
        foreach (var label in new[] { "Zurfur Light", "Zurfur Dark", "Cherry Light", "Cherry Dark" })
            _themeComboBox.DataContext.Items.Add(new ComboBoxItemTextData() { Text = new TextLines(label) });
        _themeComboBox.DataContext.SelectedIndex = 0;
        _themeComboBox.DataContext.PropertyChanged += ThemeComboBox_PropertyChanged;

        _scrollTest.DataContext.PropertyChanged += ScrollTest_PropertyChanged;


        // Setup badge test
        var items = new (string Badge, string Text)[]
        {
            ("A", "Pick 1"),
            ("B", "Pick 2"),
            ("C", "Pick 3"),
            ("D", "Pick 4"),
            ("E", "Pick 5"),
            ("F", "Pick 6"),
        };

        foreach (var (badge, text) in items)
            _badgeCombo.DataContext.Items.Add(new ComboBoxItemBadgeData { Badge = new(badge), Text = new(text) });

        _badgeCombo.DataContext.SelectedIndex = 0;
    }

    private void ScrollTest_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Value")
        {
            _textViewScrollBar.DataContext.Text = new($"Value: {_scrollTest.DataContext.Value:F0}");
        }
    }

    void ThemeComboBox_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "SelectedIndex")
            return;
        var appWindow = View.AppWindow;
        if (appWindow == null)
            return;
        switch (_themeComboBox.DataContext.SelectedIndex)
        {
            case 0: appWindow.Theme = "ZurfurDefault"; break;
            case 1: appWindow.Theme = "ZurfurDefaultDark"; break;
            case 2: appWindow.Theme = "ZurfurCherry"; break;
            case 3: appWindow.Theme = "ZurfurCherryDark"; break;
        }
    }
}
