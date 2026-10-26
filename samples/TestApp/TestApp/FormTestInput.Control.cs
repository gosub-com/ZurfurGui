using System.Text;
using System.Linq;

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

        inputSurface.View.AddGestureRecognizer(new ClickRecognizer(
            (s, e) => AddLog($"***click: {e.Contact.Key}"),
            (s, e) =>
            {
                AddLog($"***longclick: {e.Contact.Key}");
                e.Handled = true;
            }));

        buttonClearLog.Click += ButtonClearLog_Click;
        buttonSurface.Click += (s, e) => AddLog("***surface button click");

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

    // Logs the surface's pointer-down observation after routed handlers have had a chance to capture the contact.
    void OnPointerDown(object? sender, PointerEvent e)
    {
        Observe("bubble", e);
    }

    // Logs raw pointer-up before the gesture arena resolves click recognition for this contact.
    void OnPointerUp(object? sender, PointerEvent e)
    {
        Observe("bubble", e);
    }

    // Logs framework logical capture loss; this event does not identify the release cause or platform capture state.
    void OnPointerCaptureLost(object? sender, PointerContactKey contactKey)
    {
        AddLog($"Framework PointerCaptureLost: {contactKey}; release cause and platform capture state are unknown.");
    }

    // Formats and logs the contact data for the current pointer event.
    void Observe(string route, PointerEvent e)
    {
        var contact = e.Contact;
        var position = inputSurface.View.toClient(contact.DevicePosition);
        var line = $"{contact.Device} {contact.Id}: {e.Kind.ToString().ToUpper()} ({route})\n" +
            $"Device position: ({contact.DevicePosition.X:0.##},{contact.DevicePosition.Y:0.##})\n" +
            $"View position: ({position.X:0.##},{position.Y:0.##})\n" +
            $"Contact size: ({contact.Width:0.##},{contact.Height:0.##})\n" +
            $"Buttons: {contact.Buttons}\n" +
            $"Changed button: {contact.ChangedButton}\n" +
            $"Modifiers: {contact.Modifiers}\n" +
            $"Handled: {e.Handled}\n" +
            $"Cancelled: {e.Cancelled}\n" +
            $"Capture owner: {DescribeCaptureOwner(contact.Key)}";

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

    string DescribeCaptureOwner(PointerContactKey contactKey)
    {
        var capture = inputSurface.View.AppWindow?.CapturedPointerContacts
            .FirstOrDefault(contact => contact.Contact.Key == contactKey);
        if (capture is null)
            return "none";

        return $"{capture.CapturedView.Controller.GetType().Name}; isInputSurface={capture.CapturedView == inputSurface.View}";
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
        var hasCapture = inputSurface.View.AppWindow?.CapturedPointerContacts
            .Any(contact => contact.CapturedView == inputSurface.View) ?? false;
        builder.Append($"HasCapture: {hasCapture}");
        currentState.DataContext.Text = new TextLines(builder.ToString());
    }
}