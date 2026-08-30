using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Reason for a rejection as the <b>visual feedback layer</b> knows it
    /// (<c>docs/ART.md</c> §5.5).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This enum is distinct from <see cref="EnqueueResult"/>, and that is the entire point of
    /// its existence.</b> A rejection reason does not have a single source:
    /// <list type="bullet">
    /// <item><see cref="QueueFull"/>, <see cref="GamePaused"/> and <see cref="Duplicate"/> come from
    /// <see cref="InputQueue.Enqueue"/>, at press time;</item>
    /// <item><see cref="Reversal"/> comes from <see cref="InputQueue.Tick"/>
    /// (<see cref="TickResult.ReversalRejected"/>), <b>one tick later</b>, because a reversal is
    /// judged against the direction actually applied — the North/South counter-example of
    /// GDD §4.2;</item>
    /// <item><see cref="Reversal"/> <i>also</i> comes from <see cref="Startup.Decide"/>, before any
    /// tick has happened at all (GDD §4.1: pressing West at the start shows the rejection and
    /// launches nothing).</item>
    /// </list>
    ///
    /// <para>⚠ <b>Never "unify" by adding reversal to <see cref="EnqueueResult"/></b>: that would
    /// declare that a reversal can be rejected on enqueue, exactly the mistake
    /// <see cref="InputQueue"/> was written to make impossible. Practical corollary: a UI listening
    /// only to <c>Enqueue()</c> would <b>never</b> show the reversal rejection — the very case GDD
    /// §3 requires to be made visible.</para>
    /// </remarks>
    public enum RejectionReason
    {
        /// <summary>Instant reversal. Sources: the tick, and the start decision.</summary>
        Reversal,

        /// <summary>Queue full: the third turn is ignored. Source: the enqueue.</summary>
        QueueFull,

        /// <summary>Direction pressed during the pause. Source: the enqueue.</summary>
        GamePaused,

        /// <summary>
        /// Direction already being followed. Source: the enqueue. <b>No feedback</b> (ART §5.3) —
        /// present in the enum so it is filtered explicitly rather than passed over in silence.
        /// </summary>
        Duplicate
    }

    /// <summary>
    /// Visual feedback channel for a rejection reason (<c>docs/ART.md</c> §5.2).
    /// </summary>
    public enum FeedbackChannel
    {
        /// <summary>No feedback. The player missed nothing: nothing is displayed.</summary>
        None,

        /// <summary>Barred chevron anchored to the edge of the head cell (§5.4, variant A).</summary>
        Pictogram,

        /// <summary>A line of text added to the already visible pause screen (§5.4).</summary>
        PauseText
    }

    /// <summary>
    /// Translates rejections from both sources into feedback-layer reasons, and says where each one
    /// goes (<c>docs/ART.md</c> §5.2 and §5.5, ruled by the author on 2026-08-27).
    /// </summary>
    public static class RejectionRouting
    {
        /// <summary>
        /// Translates an enqueue result into a feedback reason.
        /// </summary>
        /// <param name="reason">The translated reason. Only meaningful if the method returns <c>true</c>.</param>
        /// <returns><c>false</c> for <see cref="EnqueueResult.Accepted"/>: nothing was rejected.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Unknown enqueue result — see the remark on <see cref="Channel"/>.
        /// </exception>
        public static bool FromEnqueue(EnqueueResult result, out RejectionReason reason)
        {
            switch (result)
            {
                case EnqueueResult.Accepted:
                    reason = RejectionReason.Duplicate; // Unused value: the method returns false.
                    return false;

                case EnqueueResult.RejectedDuplicate:
                    reason = RejectionReason.Duplicate;
                    return true;

                case EnqueueResult.RejectedQueueFull:
                    reason = RejectionReason.QueueFull;
                    return true;

                case EnqueueResult.RejectedGamePaused:
                    reason = RejectionReason.GamePaused;
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(result), result, "Enqueue result with no decided feedback reason (docs/ART.md §5.5).");
            }
        }

        /// <summary>
        /// Channel for the reason. <see cref="RejectionReason.Duplicate"/> gets <b>nothing</b> — and
        /// that is written in black and white, not omitted.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Unknown reason. ⚠ <b>Deliberately noisy</b>: adding a reason without deciding its channel
        /// would give a mute rejection, therefore "invisible, therefore non-existent" (GDD §3).
        /// Better an exception on the first press than a player who thinks the game is broken.
        /// </exception>
        public static FeedbackChannel Channel(RejectionReason reason)
        {
            switch (reason)
            {
                case RejectionReason.Duplicate:
                    // ⚠ FILTERED EXPLICITLY (ART §5.3), this is not an oversight to fix.
                    // It is not an error: the player's intent (keep going in that direction) is
                    // already satisfied by what is about to run, and the snake carrying straight on
                    // IS the confirmation. It is also the most frequent of the four reasons:
                    // showing it would desensitise players to the same pictogram, the one case
                    // where that sign must stay tied to "I made a mistake".
                    return FeedbackChannel.None;

                case RejectionReason.Reversal:
                case RejectionReason.QueueFull:
                    // The SAME pictogram for both (ART §5.2): at 125 ms per tick, nothing can teach
                    // the nuance between "reversal" and "one turn too many". What must read is that
                    // the press did not count.
                    return FeedbackChannel.Pictogram;

                case RejectionReason.GamePaused:
                    // Outside any time pressure: the simulation is frozen, the player can read a
                    // sentence.
                    return FeedbackChannel.PauseText;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(reason), reason, "Rejection reason with no decided visual channel (docs/ART.md §5.2).");
            }
        }
    }

    /// <summary>
    /// The anti-repeat of the rejection feedback (<c>docs/ART.md</c> §5.5): a <b>state with a
    /// deadline</b>, never a replayed animation.
    /// </summary>
    /// <remarks>
    /// The brief requires three behaviours, and each one fixes a specific defect:
    /// <list type="number">
    /// <item>a notification shows the feedback and sets its deadline;</item>
    /// <item>a notification received while it is showing <b>extends</b> the deadline <b>without
    /// restarting the appearance</b> — otherwise hammering produces flicker;</item>
    /// <item>a <b>continuous-extension cap</b> forces the feedback out: "a signal that is always
    /// visible stops being read as a signal".</item>
    /// </list>
    ///
    /// <para>⚠ <b>Going out is protected, fade-out AND dead time</b>: a notification received during
    /// either is ignored. Without that protection the cap caps nothing under hammering — the same
    /// trap as the catch-up cap in <see cref="Cadence"/>: a cap that forces no observable
    /// interruption is not a cap.
    ///
    /// <para>⚠ And the dead time cannot be dropped "since the fade is enough": without it, a
    /// notification landing exactly at the end of the fade relights the feedback on the very frame
    /// it just went out. The player then sees <b>no</b> break at all — just an opacity that dips and
    /// comes back within one frame. The first guard written here measured "opacity falls back to
    /// zero": it went green on both implementations, because the moment of going out exists in
    /// either case. What distinguishes a visible extinction from a non-extinction is its
    /// <b>duration</b>, not its existence.</para>
    ///
    /// <para>The dead time equals one fade duration: it is <b>derived</b> from that parameter rather
    /// than added as yet another value, so that a single setting governs the whole envelope.</para>
    ///
    /// <para>⚠ <b>Durations: none has been tried in play</b> (ART §5.5, "by judgement, to be
    /// confirmed by the game tester"). They are tunable without recompiling through
    /// <see cref="GameSettings"/>.</para>
    ///
    /// <para>A stateful class with no engine dependency: time is <b>given</b> to it on every call
    /// rather than read from a clock. That is what makes it testable in microseconds, and what lets
    /// two seconds of hammering be replayed without waiting two seconds.</para>
    /// </remarks>
    public sealed class TimedFeedback
    {
        private readonly double _displayDuration;
        private readonly double _extensionCap;
        private readonly double _fadeDuration;

        private bool _active;
        private double _start;
        private double _deadline;

        /// <param name="displayDurationSeconds">Display duration per trigger (ART §5.5).</param>
        /// <param name="extensionCapSeconds">
        /// Continuous visibility beyond which the feedback goes out, even if it relights when the
        /// hammering continues.
        /// </param>
        /// <param name="fadeDurationSeconds">
        /// Duration of the fade envelope, in and out. One envelope per trigger (ART §5.7: never a
        /// strobe).
        /// </param>
        public TimedFeedback(double displayDurationSeconds, double extensionCapSeconds, double fadeDurationSeconds)
        {
            if (displayDurationSeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayDurationSeconds), displayDurationSeconds,
                    "Feedback that lasts zero seconds is invisible feedback, therefore non-existent (GDD §3).");
            }

            if (extensionCapSeconds < displayDurationSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(extensionCapSeconds), extensionCapSeconds,
                    "The extension cap cannot be shorter than the display duration: the feedback would go out before being read.");
            }

            if (fadeDurationSeconds <= 0.0)
            {
                // ⚠ A zero fade would make the cap useless: going out would have no duration, so
                // nothing for the player to see, and the feedback would relight straight away under
                // hammering. That is exactly what ART §5.7 forbids.
                throw new ArgumentOutOfRangeException(
                    nameof(fadeDurationSeconds), fadeDurationSeconds,
                    "Without a fade, the extinction forced by the cap has no duration: it would be invisible, therefore non-existent (ART §5.7).");
            }

            if (fadeDurationSeconds * 2.0 > displayDurationSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fadeDurationSeconds), fadeDurationSeconds,
                    "The fades must fit inside the display duration, otherwise the feedback never reaches full opacity.");
            }

            _displayDuration = displayDurationSeconds;
            _extensionCap = extensionCapSeconds;
            _fadeDuration = fadeDurationSeconds;
        }

        /// <summary>Display duration per trigger, in seconds.</summary>
        public double DisplayDuration
        {
            get { return _displayDuration; }
        }

        /// <summary>Continuous-visibility cap, in seconds.</summary>
        public double ExtensionCap
        {
            get { return _extensionCap; }
        }

        /// <summary>
        /// Signals a rejection to display.
        /// </summary>
        /// <returns>
        /// True <b>only</b> if this is a fresh appearance — that is, if the caller must
        /// (re)position the pictogram and play the envelope. False for an extension or for a
        /// notification landing during the extinction: in both cases, restart nothing.
        /// </returns>
        public bool Notify(double now)
        {
            if (_active && now < _deadline)
            {
                // Extension: we push the deadline back WITHOUT touching _start, so without
                // restarting the appearance. The cap counts from the appearance, not from the last
                // press: that is what bounds continuous visibility.
                double pushed = now + _displayDuration;
                double capped = _start + _extensionCap;
                _deadline = pushed < capped ? pushed : capped;
                return false;
            }

            if (_active && now < EndOfDeadTime)
            {
                // Extinction under way: fade-out, THEN dead time at zero opacity. We let it run to
                // completion (see the class remarks). It is the only thing that guarantees the cap
                // produces a break of observable duration, and not a one-frame dip.
                return false;
            }

            _active = true;
            _start = now;
            _deadline = now + _displayDuration;
            return true;
        }

        /// <summary>
        /// End of the dead time that follows the fade-out: before that instant, no notification can
        /// relight the feedback.
        /// </summary>
        private double EndOfDeadTime
        {
            get { return _deadline + (2.0 * _fadeDuration); }
        }

        /// <summary>True as long as the feedback has non-zero opacity.</summary>
        public bool IsVisible(double now)
        {
            return _active && now >= _start && now < _deadline + _fadeDuration;
        }

        /// <summary>
        /// Feedback opacity, between 0 and 1: rise, hold, fall. A pure function of time — no state
        /// is modified here, so that a renderer called several times per frame does not drift the
        /// deadline.
        /// </summary>
        public double Opacity(double now)
        {
            if (!IsVisible(now))
            {
                return 0.0;
            }

            if (_fadeDuration <= 0.0)
            {
                return 1.0;
            }

            if (now < _start + _fadeDuration)
            {
                return (now - _start) / _fadeDuration;
            }

            if (now >= _deadline)
            {
                return 1.0 - ((now - _deadline) / _fadeDuration);
            }

            return 1.0;
        }

        /// <summary>Puts the feedback out at once (game state change, new game).</summary>
        public void Clear()
        {
            _active = false;
            _start = 0.0;
            _deadline = 0.0;
        }
    }
}
