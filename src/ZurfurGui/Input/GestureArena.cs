using System.Runtime.ExceptionServices;
using ZurfurGui.Base;

namespace ZurfurGui.Input;

/// <summary>
/// Arbitrates recognizer sessions for one pointer contact.
/// </summary>
/// <remarks>
/// The router creates an arena on an uncaptured pointer-down. The arena gathers recognizers from the hit view up its
/// ancestor chain, creates one session per recognizer, and sends each session a copy of the contact's events. Pending
/// sessions compete until one accepts or all reject. Acceptance cancels the other sessions and captures the winner's
/// view while the contact is active. The arena owns this per-contact arbitration; it does not own recognizer
/// registration or recognizer disposal.
///
/// If recognizer code throws while the arena is calling it, the arena is canceled (releasing a winner's capture) and
/// the exception is rethrown to the caller.
/// </remarks>
internal sealed class GestureArena
{
    readonly PointerRouter _router;
    readonly PointerContactState _contactState;
    readonly List<Candidate> _candidates = [];
    Candidate? _winner;
    PointerEventKind _currentKind;
    bool _ended;

    // Creates an empty arena. Use Start to build the candidates.
    GestureArena(PointerRouter router, PointerContactState contactState)
    {
        _router = router;
        _contactState = contactState;
    }

    /// <summary>
    /// Builds the candidates for a new contact. Sessions are created in hit-target-to-root order and receive the down
    /// event before raw pointer routing begins. Returns null if no candidate remains after the down event.
    /// </summary>
    public static GestureArena? Start(
        PointerRouter router,
        PointerContactState contactState,
        IReadOnlyList<View> hitChain,
        PointerEvent pointerEvent)
    {
        var arena = new GestureArena(router, contactState);
        try
        {
            arena.CreateCandidates(hitChain, pointerEvent.Contact);
        }
        catch
        {
            arena.Abort();
            throw;
        }

        if (arena._candidates.Count == 0)
            return null;

        contactState.GestureArena = arena;
        arena._currentKind = PointerEventKind.Down;
        arena.DeliverDown(pointerEvent);
        return arena._ended ? null : arena;
    }

    /// <summary>
    /// Delivers later contact events to pending candidates or the winner. The router calls this after raw pointer
    /// routing, allowing raw handlers to request capture before gesture candidates process a move or release.
    /// </summary>
    public void Process(PointerEvent pointerEvent)
    {
        if (_ended)
            return;

        _currentKind = pointerEvent.Kind;
        if (pointerEvent.Kind == PointerEventKind.Cancel)
        {
            Cancel(preserveCapture: true);
            return;
        }

        var targets = _winner is null ? _candidates.ToArray() : [_winner];
        foreach (var candidate in targets)
        {
            if (_ended)
                break;
            if (candidate.State != CandidateState.Done)
                Deliver(candidate, pointerEvent);
        }

        if (pointerEvent.Kind == PointerEventKind.Up)
            End(completeWinner: true, releaseWinnerCapture: false);
    }

    /// <summary>
    /// Ends the arena when the platform or router cancels the contact. Capture is preserved when cancellation came
    /// from the router's ordinary platform/capture path; recognizer disposal releases a winner's gesture capture.
    /// </summary>
    public void Cancel(bool preserveCapture)
    {
        End(completeWinner: false, releaseWinnerCapture: !preserveCapture);
    }

    /// <summary>
    /// Cancels the sessions of a recognizer that was removed from a view. A pending session is rejected. The
    /// winner cancels the whole arena and releases capture, unless the contact is already being released: a
    /// winner receiving pointer-up is completing normally, so removal from its own handler does not cancel it.
    /// </summary>
    public void CancelRecognizer(IGestureRecognizer recognizer, View view)
    {
        var matches = _candidates.Where(c => c.Recognizer == recognizer && c.View == view).ToArray();
        foreach (var candidate in matches)
        {
            if (_ended)
                break;
            if (candidate.State == CandidateState.Done)
                continue;

            if (candidate != _winner)
                Reject(candidate);
            else if (_currentKind != PointerEventKind.Up)
                End(completeWinner: false, releaseWinnerCapture: true);
        }
    }

    /// <summary>
    /// Cancels the candidates whose view is the given view or one of its descendants, because the subtree left the
    /// view tree. Pending candidates are rejected. A winner in the subtree cancels the arena and releases capture,
    /// unless it is already receiving pointer-up and is completing normally.
    /// </summary>
    public void CancelViewTree(View root)
    {
        var matches = _candidates.Where(c => PointerRouter.IsInViewTree(c.View, root)).ToArray();
        foreach (var candidate in matches)
        {
            if (_ended)
                break;
            if (candidate.State == CandidateState.Done)
                continue;

            if (candidate != _winner)
                Reject(candidate);
            else if (_currentKind != PointerEventKind.Up)
                End(completeWinner: false, releaseWinnerCapture: true);
        }
    }

    // Creates a candidate (context and session) for every recognizer registered on the hit chain.
    void CreateCandidates(IReadOnlyList<View> hitChain, PointerContact contact)
    {
        foreach (var candidateView in hitChain)
        {
            foreach (var recognizer in candidateView.GestureRecognizers.ToArray())
            {
                // An earlier recognizer's callback may have removed this one since the snapshot was taken.
                if (!candidateView.GestureRecognizers.Contains(recognizer))
                    continue;

                var candidate = new Candidate(this, recognizer, candidateView, contact);
                try
                {
                    candidate.Session = recognizer.CreateSession(candidate.Context);
                }
                catch
                {
                    candidate.Context.End();
                    throw;
                }

                if (candidate.Session is null)
                {
                    candidate.State = CandidateState.Done;
                    candidate.Context.End();
                    continue;
                }

                _candidates.Add(candidate);
            }
        }
    }

    // Sends the down event to each candidate, then applies any accept or reject requested while it was created.
    void DeliverDown(PointerEvent pointerEvent)
    {
        foreach (var candidate in _candidates.ToArray())
        {
            if (_ended)
                break;
            if (candidate.State == CandidateState.Done)
                continue;

            Deliver(candidate, pointerEvent);
            ApplyPendingResolution(candidate);
        }
    }

    // Applies an accept or reject that the recognizer requested before its session existed.
    void ApplyPendingResolution(Candidate candidate)
    {
        var pending = candidate.PendingResolution;
        candidate.PendingResolution = PendingResolution.None;
        if (_ended || candidate.State == CandidateState.Done)
            return;

        if (pending == PendingResolution.Accept)
            Accept(candidate);
        else if (pending == PendingResolution.Reject)
            Reject(candidate);
    }

    // Sends one pointer event copy to a candidate's session. Recognizer exceptions cancel the arena and rethrow.
    void Deliver(Candidate candidate, PointerEvent pointerEvent)
    {
        var gestureEvent = new PointerEvent(pointerEvent.Kind)
        {
            Contact = pointerEvent.Contact,
            Cancelled = pointerEvent.Cancelled
        };

        try
        {
            switch (gestureEvent.Kind)
            {
                case PointerEventKind.Down:
                    candidate.Session!.OnPointerDown(gestureEvent);
                    break;
                case PointerEventKind.Move:
                    candidate.Session!.OnPointerMove(gestureEvent);
                    break;
                case PointerEventKind.Up:
                    candidate.Session!.OnPointerUp(gestureEvent);
                    break;
            }
        }
        catch
        {
            Abort();
            throw;
        }
    }

    // Makes a candidate the winner, cancels the rest, and captures the winner's view unless the contact is ending.
    void Accept(Candidate candidate)
    {
        if (_ended || _winner is not null || candidate.State != CandidateState.Pending)
            return;

        if (candidate.Session is null)
        {
            if (candidate.PendingResolution == PendingResolution.None)
                candidate.PendingResolution = PendingResolution.Accept;
            return;
        }

        _winner = candidate;
        candidate.State = CandidateState.Winner;
        foreach (var other in _candidates.Where(other => other != candidate).ToArray())
            RemoveCandidate(other);

        if (_currentKind != PointerEventKind.Up)
            _router.CaptureFromGesture(_contactState, candidate.View);
    }

    // Removes a pending candidate. The arena finishes if it was the last one.
    void Reject(Candidate candidate)
    {
        if (_ended || candidate == _winner || candidate.State != CandidateState.Pending)
            return;

        if (candidate.Session is null)
        {
            if (candidate.PendingResolution == PendingResolution.None)
                candidate.PendingResolution = PendingResolution.Reject;
            return;
        }

        RemoveCandidate(candidate);
        if (_candidates.Count == 0)
            End(completeWinner: true, releaseWinnerCapture: false);
    }

    // Removes a candidate from the arena and tells its session it was cancelled.
    void RemoveCandidate(Candidate candidate)
    {
        _candidates.Remove(candidate);
        CloseCandidate(candidate, cancelled: true);
    }

    // Marks a candidate done, optionally notifies its session of cancellation, and ends its context.
    static void CloseCandidate(Candidate candidate, bool cancelled)
    {
        candidate.State = CandidateState.Done;
        try
        {
            if (cancelled)
                candidate.Session?.OnCancelled();
        }
        finally
        {
            candidate.Context.End();
        }
    }

    // Ends the arena. The winner is completed silently when requested; every other candidate is cancelled.
    // A failure in one session's cancellation does not stop the others; the first failure is rethrown at the end.
    void End(bool completeWinner, bool releaseWinnerCapture)
    {
        if (_ended)
            return;

        _ended = true;
        var winner = _winner;
        var candidates = _candidates.ToArray();
        _candidates.Clear();
        if (_contactState.GestureArena == this)
            _contactState.GestureArena = null;

        Exception? failure = null;
        foreach (var candidate in candidates)
        {
            try
            {
                CloseCandidate(candidate, cancelled: !(completeWinner && candidate == winner));
            }
            catch (Exception ex)
            {
                failure ??= ex;
            }
        }

        if (releaseWinnerCapture && winner is not null)
            _router.ReleaseGestureCapture(_contactState, winner.View);

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    // Cancels the arena after a recognizer fault, ignoring secondary failures so the original exception wins.
    void Abort()
    {
        try
        {
            End(completeWinner: false, releaseWinnerCapture: true);
        }
        catch
        {
            // The caller rethrows the original exception.
        }
    }

    // Where a candidate is in its arbitration lifetime.
    enum CandidateState
    {
        Pending,
        Winner,
        Done
    }

    // An accept or reject requested before the candidate's session was created.
    enum PendingResolution
    {
        None,
        Accept,
        Reject
    }

    // One recognizer/contact attempt: its view, context, session, and arbitration state.
    sealed class Candidate : IGestureResolver
    {
        readonly GestureArena _arena;

        public Candidate(GestureArena arena, IGestureRecognizer recognizer, View view, PointerContact contact)
        {
            _arena = arena;
            Recognizer = recognizer;
            View = view;
            Context = new GestureSessionContext(view, contact, this);
        }

        public IGestureRecognizer Recognizer { get; }
        public View View { get; }
        public GestureSessionContext Context { get; }
        public IGestureRecognizerSession? Session { get; set; }
        public CandidateState State { get; set; } = CandidateState.Pending;
        public PendingResolution PendingResolution { get; set; }

        public void Accept() => _arena.Accept(this);
        public void Reject() => _arena.Reject(this);
    }
}
