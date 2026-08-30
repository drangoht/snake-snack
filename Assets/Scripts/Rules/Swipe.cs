namespace SnakeSnack.Rules
{
    /// <summary>What a finger's travel since its contact point amounts to (GDD §3, touch).</summary>
    public readonly struct SwipeReading
    {
        private SwipeReading(bool recognised, Direction direction)
        {
            Recognised = recognised;
            Direction = direction;
        }

        /// <summary>True when the travel has passed the threshold and names a direction.</summary>
        public bool Recognised { get; }

        /// <summary>The direction read. Meaningless while <see cref="Recognised"/> is false.</summary>
        public Direction Direction { get; }

        internal static SwipeReading Nothing
        {
            get { return new SwipeReading(false, Direction.East); }
        }

        internal static SwipeReading Of(Direction direction)
        {
            return new SwipeReading(true, direction);
        }
    }

    /// <summary>
    /// Turns a finger's travel into a direction (GDD §3, touch — reopened on 2026-08-30).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Unit: one pixel of the 1280×720 reference frame</b>, like <see cref="Board"/>. The
    /// caller converts real screen pixels before calling in, so this rule stays independent of the
    /// panel it ends up on: the same swipe means the same thing on a phone and in a desktop window.
    ///
    /// <para>⚠ <b>Y grows upwards</b>, as everywhere else in <c>Rules/</c>: a positive
    /// <c>dy</c> is <see cref="Direction.North"/>. Unity's screen coordinates already point that
    /// way; a caller reading a coordinate system with Y downwards must flip it before calling, and
    /// nothing here can detect that it did not.</para>
    ///
    /// <para><b>Why the reading is taken on travel and not on release.</b> Waiting for the finger to
    /// lift adds the whole length of the gesture to the latency — at 8 ticks/s a turn decided
    /// 200 ms late is a turn taken one cell too far, and death stops being attributable to the
    /// decision the player made (GDD §2). The direction fires the instant the threshold is passed,
    /// and the caller then re-arms the origin at that point, which is what lets an L-shaped turn be
    /// drawn in one continuous gesture without lifting.</para>
    /// </remarks>
    public static class Swipe
    {
        /// <summary>
        /// Travel from which a gesture stops being a tap and becomes a turn: 28 px of the reference
        /// frame.
        /// </summary>
        /// <remarks>
        /// Sized between two failures, both of which make the game unplayable. Too low, the jitter
        /// of a finger landing for a <i>tap</i> — restart, confirm — reads as a swipe and turns the
        /// snake the player never asked to turn. Too high, the gesture has to be drawn out and the
        /// turn lands late, which §2 forbids. 28 px is under a cell (44 px), so the turn is decided
        /// before the snake has crossed the cell the player is looking at.
        /// </remarks>
        public const double DefaultThreshold = 28.0;

        /// <param name="dx">Horizontal travel since the contact point, in reference pixels.</param>
        /// <param name="dy">Vertical travel since the contact point, Y upwards.</param>
        /// <param name="threshold">Travel from which the gesture counts.</param>
        public static SwipeReading Read(double dx, double dy, double threshold = DefaultThreshold)
        {
            double horizontal = dx < 0 ? -dx : dx;
            double vertical = dy < 0 ? -dy : dy;

            if (horizontal < threshold && vertical < threshold)
            {
                return SwipeReading.Nothing;
            }

            // ⚠ The dominant axis decides, and a tie goes to the horizontal. A tie is vanishingly
            // rare in floating point, but it has to be settled SOMEWHERE: leaving it unrecognised
            // would read as "the game missed my swipe", which §4.2 spends a whole rule avoiding.
            // Horizontal wins because the playfield is 21 cells wide against 15 high — there is
            // simply more room, hence more turning, along that axis.
            if (horizontal >= vertical)
            {
                return SwipeReading.Of(dx >= 0 ? Direction.East : Direction.West);
            }

            return SwipeReading.Of(dy >= 0 ? Direction.North : Direction.South);
        }

        /// <summary>
        /// True when the travel never left the threshold — the gesture is a tap, not a turn.
        /// </summary>
        /// <remarks>
        /// Deliberately the exact complement of <see cref="Read"/>: one gesture is either a turn or
        /// a tap, never both and never neither. A finger that lands and lifts without travelling is
        /// the only press a mobile player has for "start", "restart" and "confirm" — the three
        /// things the keyboard spends Space and Enter on.
        /// </remarks>
        public static bool IsTap(double dx, double dy, double threshold = DefaultThreshold)
        {
            return !Read(dx, dy, threshold).Recognised;
        }
    }
}
