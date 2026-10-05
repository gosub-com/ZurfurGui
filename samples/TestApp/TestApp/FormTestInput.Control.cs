using System.Text;

using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Input;

namespace TestApp;

public partial class FormTestInput
{
    const int MAXIMUM_LOG_ENTRIES = 1000;

    readonly List<string> _eventLog = [];

    public FormTestInput()
    {
        InitializeControl();

        buttonClearLog.Click += ButtonClearLog_Click;

        inputSurface.View.AddEvent(Panel.PreviewPointerDown, (s, e) => Observe("preview", e));
        inputSurface.View.AddEvent(Panel.PreviewPointerMove, (s, e) => Observe("preview", e));
        inputSurface.View.AddEvent(Panel.PreviewPointerUp, (s, e) => Observe("preview", e));
        inputSurface.View.AddEvent(Panel.PreviewPointerClick, (s, e) => Observe("preview", e));
        inputSurface.View.AddEvent(Panel.PointerDown, OnPointerDown);
        inputSurface.View.AddEvent(Panel.PointerMove, (s, e) => Observe("bubble", e));
        inputSurface.View.AddEvent(Panel.PointerUp, OnPointerUp);
        inputSurface.View.AddEvent(Panel.PointerClick, (s, e) => Observe("bubble", e));
        inputSurface.View.AddEvent(Panel.PointerCaptureLost, OnPointerCaptureLost);        

        UpdateState("Waiting for pointer input...");
    }

    void ButtonClearLog_Click(object? sender, EventArgs e)
    {
        _eventLog.Clear();
        eventLogText.DataContext.Text = new TextLines("No events yet.");
    }

    void OnPointerDown(object? sender, PointerEvent e)
    {
        Observe("bubble", e);
        inputSurface.View.CapturePointer = true;
        AddLog("capture requested");
    }

    void OnPointerUp(object? sender, PointerEvent e)
    {
        Observe("bubble", e);
        AddLog("capture release requested");
        inputSurface.View.CapturePointer = false;
    }

    void OnPointerCaptureLost(object? sender, EventArgs e)
    {
        AddLog("pointercapturelost");
    }

    void Observe(string route, PointerEvent e)
    {
        var contact = e.Contact;
        var position = inputSurface.View.toClient(contact.DevicePosition);
        var line = $"Route: {route}\n" +
            $"Event: {e.Kind}\n" +
            $"Device: {contact.Device}\n" +
            $"Contact ID: {contact.Id}\n" +
            $"Device position: ({contact.DevicePosition.X:0.##},{contact.DevicePosition.Y:0.##})\n" +
            $"View position: ({position.X:0.##},{position.Y:0.##})\n" +
            $"Contact size: ({contact.Width:0.##},{contact.Height:0.##})\n" +
            $"Buttons: {contact.Buttons}\n" +
            $"Changed button: {contact.ChangedButton}\n" +
            $"Modifiers: {contact.Modifiers}";

        AddLog(line.Replace("\n", ", "));
        UpdateState(line);
    }

    void AddLog(string line)
    {
        _eventLog.Add(line);
        if (_eventLog.Count > MAXIMUM_LOG_ENTRIES)
            _eventLog.RemoveAt(0);

        eventLogText.DataContext.Text = new TextLines(_eventLog.ToArray());
    }

    void UpdateState(string message)
    {
        var builder = new StringBuilder(message);
        builder.AppendLine();
        builder.Append($"IsPointerOver: {inputSurface.View.GetProperty(Panel.IsPointerOver)}");
        builder.AppendLine();
        builder.Append($"IsPressed: {inputSurface.View.GetProperty(Panel.IsPressed)}");
        builder.AppendLine();
        builder.Append($"CapturePointer: {inputSurface.View.CapturePointer}");
        currentState.DataContext.Text = new TextLines(builder.ToString());
    }
}