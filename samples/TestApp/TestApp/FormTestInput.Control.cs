using System.Text;

using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Input;

namespace TestApp;

public partial class FormTestInput
{
    const int MAXIMUM_LOG_ENTRIES = 10000;

    // Initializes the input test surface and attaches handlers for all input events being demonstrated.
    public FormTestInput()
    {
        InitializeControl();

        buttonClearLog.Click += ButtonClearLog_Click;

        inputSurface.View.AddEvent(Panel.PreviewPointerDown, (s, e) => Observe("preview", e));
        inputSurface.View.AddEvent(Panel.PreviewPointerMove, (s, e) => Observe("preview", e));
        inputSurface.View.AddEvent(Panel.PreviewPointerUp, (s, e) => Observe("preview", e));
        inputSurface.View.AddEvent(Panel.PointerDown, OnPointerDown);
        inputSurface.View.AddEvent(Panel.PointerMove, (s, e) => Observe("bubble", e));
        inputSurface.View.AddEvent(Panel.PointerUp, OnPointerUp);
        inputSurface.View.AddEvent(Panel.PointerEntered, OnPointerEntered);
        inputSurface.View.AddEvent(Panel.PointerExited, OnPointerExited);
        inputSurface.View.AddEvent(Panel.PointerCaptureLost, OnPointerCaptureLost);

        UpdateState("Waiting for pointer input...");
    }

    // Logs a pointer enter with its per-contact data.
    void OnPointerEntered(object? sender, PointerEvent e)
    {
        Observe("entered", e);
    }

    // Logs a pointer exit with its per-contact data.
    void OnPointerExited(object? sender, PointerEvent e)
    {
        Observe("exited", e);
    }

    // Clears the event log when the clear button is clicked.
    void ButtonClearLog_Click(object? sender, EventArgs e)
    {
        eventLogText.DataContext.Text = new TextLines("No events yet.");
    }

    // Logs pointer-down and captures the contact for the routing demonstration.
    void OnPointerDown(object? sender, PointerEvent e)
    {
        Observe("bubble", e);
        inputSurface.View.CapturePointer = true;
        AddLog("capture requested");
    }

    // Logs pointer-up and releases the contact capture.
    void OnPointerUp(object? sender, PointerEvent e)
    {
        Observe("bubble", e);
        AddLog("capture release requested");
        inputSurface.View.CapturePointer = false;
    }

    // Logs when the captured contact is released by the router.
    void OnPointerCaptureLost(object? sender, EventArgs e)
    {
        AddLog("pointercapturelost");
    }

    // Formats and logs the contact data for the current pointer event.
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

    // Appends an entry to the event log while keeping it within the configured limit.
    void AddLog(string line)
    {
        var log = new List<string>(eventLogText.DataContext.Text ?? []);
        if (log.Count >= MAXIMUM_LOG_ENTRIES)
            log.RemoveAt(0);
        log.Add(line);
        eventLogText.DataContext.Text = new TextLines(log);
    }

    // Displays the latest event together with the current pointer pseudo-state.
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