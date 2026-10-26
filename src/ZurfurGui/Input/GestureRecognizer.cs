using ZurfurGui.Base;

namespace ZurfurGui.Input;

/// <summary>
/// A gesture recognizer attached to a view and considered for contacts whose hit path includes that view.
/// </summary>
/// <remarks>
/// The router asks each recognizer to create one session for each contact. Attach a recognizer with
/// <see cref="View.AddGestureRecognizer{T}"/> and detach it with <see cref="View.RemoveGestureRecognizer"/>;
/// detaching cancels the recognizer's active sessions.
/// </remarks>
public interface IGestureRecognizer
{
    /// <summary>Creates state for one contact and its arena-resolution context.</summary>
    IGestureRecognizerSession? CreateSession(GestureSessionContext context);
}

/// <summary>
/// Implements one recognizer's interpretation of one pointer contact.
/// </summary>
/// <remarks>
/// The arena supplies copies of pointer events. Implementations resolve their participation through the context
/// provided when the session is created. <see cref="OnCancelled"/> is called when a session loses or is canceled;
/// normal completion on pointer-up does not call it.
/// </remarks>
public interface IGestureRecognizerSession
{
    /// <summary>Called when the contact begins.</summary>
    void OnPointerDown(PointerEvent e);

    /// <summary>Called for contact movement while this session is pending or has won.</summary>
    void OnPointerMove(PointerEvent e);

    /// <summary>Called when the contact is released.</summary>
    void OnPointerUp(PointerEvent e);

    /// <summary>Called when this session loses, is canceled, or its recognizer is disposed.</summary>
    void OnCancelled();
}

// Lets a session context forward accept and reject requests to the arena without exposing it.
internal interface IGestureResolver
{
    void Accept();
    void Reject();
}

/// <summary>
/// A session-scoped handle for resolving
/// </summary>
/// <remarks>
/// The arena creates this context for one recognizer/contact candidate and supplies it to
/// <see cref="IGestureRecognizer.CreateSession"/>. Accepting or rejecting is safe from a session callback or timer
/// callback. Calls made after the session ends have no effect.
/// </remarks>
public sealed class GestureSessionContext
{
    readonly IGestureResolver _resolver;
    bool _ended;

    // Creates the context for one candidate. The resolver forwards resolution requests to the arena.
    internal GestureSessionContext(View candidateView, PointerContact contact, IGestureResolver resolver)
    {
        CandidateView = candidateView;
        Contact = contact;
        _resolver = resolver;
    }

    /// <summary>The view this recognizer/contact candidate belongs to.</summary>
    public View CandidateView { get; }

    /// <summary>The contact snapshot this context resolves.</summary>
    public PointerContact Contact { get; }

    /// <summary>The key identifying the contact this context resolves.</summary>
    public PointerContactKey ContactKey => Contact.Key;

    /// <summary>Accepts this session and cancels competing candidates for its contact.</summary>
    public void Accept()
    {
        if (!_ended)
            _resolver.Accept();
    }

    /// <summary>Rejects this session and removes it from consideration for its contact.</summary>
    public void Reject()
    {
        if (!_ended)
            _resolver.Reject();
    }

    internal void End()
    {
        if (_ended)
            return;

        _ended = true;
    }
}