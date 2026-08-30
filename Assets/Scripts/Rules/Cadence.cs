using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// The game's time step (GDD §4.1): the snake moves one cell per tick, never between two ticks.
    /// The tick is the unit of measure for everything that gets tuned later.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Engine-side dependency, not covered here</b>: the 2026-08-27 ruling on catch-up (§4.1)
    /// assumes that <b>losing window focus pauses the game</b>. That is written with
    /// <c>Application.focusChanged</c>, so on the <c>Gameplay/</c> side — not in <c>Rules/</c>.
    /// Without that pause, alt-tabbing stays playable but costs the player all the time spent
    /// outside the window: the catch-up cap discards the backlog, it does not give it back.
    /// </remarks>
    public static class Cadence
    {
        /// <summary>
        /// Default rate, in ticks per second.
        /// </summary>
        /// <remarks>
        /// ⚠ Value set <b>by judgement, to be confirmed in play</b> — no session is recorded in
        /// <c>docs/TEST_REPORT.md</c> as of 2026-08-27. Range worth trying: 6 to 10 ticks/s
        /// (<see cref="SuggestedMinimumRate"/> / <see cref="SuggestedMaximumRate"/>). The reasoning
        /// of §4.1: the input window for a turn is exactly one tick, so 125 ms — shorter than a
        /// simple visual reaction time. You do not react to an incoming wall, you decide one cell
        /// ahead; that is the skill being asked for.
        ///
        /// <para>⚠ <b>Queue depth and rate are linked</b> (§4.2): a depth-2 queue covers an L-shaped
        /// turn made in one gesture, i.e. 250 ms at 8 ticks/s. Revisit
        /// <see cref="InputQueue.DefaultDepth"/> if this value moves.</para>
        /// </remarks>
        public const double DefaultTicksPerSecond = 8.0;

        /// <summary>Tick duration at the default rate: 125 ms.</summary>
        public const double DefaultTickDurationSeconds = 1.0 / DefaultTicksPerSecond;

        /// <summary>Lower end of the range to try in play (§4.1) — not a hard limit.</summary>
        public const double SuggestedMinimumRate = 6.0;

        /// <summary>Upper end of the range to try in play (§4.1) — not a hard limit.</summary>
        public const double SuggestedMaximumRate = 10.0;

        /// <summary>
        /// Tick duration, in seconds, for a given rate.
        /// </summary>
        /// <remarks>
        /// The parameterised overload is what makes the rate tunable <b>without recompiling</b>
        /// (§4.1): the engine-side caller reads the value from a JSON file in
        /// <c>StreamingAssets</c> and passes it here. The constant is only the fallback when no
        /// setting is supplied.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Rate is zero, negative or non-finite. Deliberately <b>no silent clamping</b>: a mistyped
        /// tuning file must show itself at once, not produce a frozen game or an infinitely long
        /// tick that nobody could explain.
        /// </exception>
        public static double TickDurationSeconds(double ticksPerSecond = DefaultTicksPerSecond)
        {
            if (double.IsNaN(ticksPerSecond) || double.IsInfinity(ticksPerSecond) || ticksPerSecond <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ticksPerSecond),
                    ticksPerSecond,
                    "The rate must be a finite, strictly positive number (ticks per second).");
            }

            return 1.0 / ticksPerSecond;
        }

        /// <summary>
        /// True if the rate falls inside the range the design intends to try (§4.1).
        /// </summary>
        /// <remarks>
        /// Meant to <b>warn</b> a caller loading a tuning file, not to reject the value: outside the
        /// range is still playable, and that is precisely what we want to be able to try. The
        /// "invalid" / "unusual" distinction belongs to the game designer.
        /// </remarks>
        public static bool IsWithinSuggestedRange(double ticksPerSecond)
        {
            return ticksPerSecond >= SuggestedMinimumRate
                   && ticksPerSecond <= SuggestedMaximumRate;
        }

        /// <summary>
        /// The rate actually applied at any moment of a game. It <b>depends on neither snake length
        /// nor score</b>: it is always the base rate.
        /// </summary>
        /// <remarks>
        /// ⚠ This method exists to <b>lock a decision down</b>, not to compute: "constant rate for
        /// the whole game", ruled by the author on 2026-08-27 against Nokia Snake canon (§4.1,
        /// detailed rejection in §7). Speeding up with length is a multiplier, not a named rule: it
        /// stacks on a difficulty that already rises on its own, it blurs the attribution of death
        /// (§2), and it makes the tick variable, so two runs incomparable on a bench.
        ///
        /// <para><paramref name="snakeLength"/> is ignored <b>on purpose</b>: it is the choke point
        /// that a future "what if we sped it up a bit?" would modify, and the test that goes with it
        /// would then fail. Reopening the subject goes through §7, not through this file.</para>
        /// </remarks>
        public static double EffectiveRate(double baseRate, int snakeLength)
        {
            return baseRate;
        }

        /// <summary>
        /// Default catch-up cap: <b>1 tick per frame</b> (§4.1, author's ruling of 2026-08-27).
        /// </summary>
        /// <remarks>
        /// Without a cap, a one-second freeze (alt-tab, loading) covers eight cells at once,
        /// <b>invisibly</b>: the death that follows cannot be attributed to any turn, which §2
        /// forbids. The accepted price is a brief drift of the rate after a hitch — preferable to
        /// cells crossed out of the player's sight.
        ///
        /// <para>Tunable like the rest: someone will want to try it at 2.</para>
        /// </remarks>
        public const int DefaultCatchUpCap = 1;

        /// <summary>
        /// Splits accumulated time into a capped number of ticks to play, and returns the leftover.
        /// </summary>
        /// <param name="accumulatedSeconds">Elapsed time not yet converted into ticks.</param>
        /// <param name="tickDurationSeconds">Tick duration, from <see cref="TickDurationSeconds"/>.</param>
        /// <param name="leftover">Remainder to carry to the next frame — always &lt; one tick.</param>
        /// <param name="catchUpCap">Maximum ticks played in one frame (§4.1).</param>
        /// <remarks>
        /// <b>Two behaviours coexist here, and they must not be confused:</b>
        ///
        /// <para>1. <b>In the normal regime the leftover is carried, not discarded.</b> Zeroing the
        /// accumulator at every tick drifts the real rate downwards as soon as the frame step does
        /// not divide the tick duration (at 60 Hz, 125 ms falls between two frames). A drift of a
        /// few percent raises nothing but skews any measurement of run length.</para>
        ///
        /// <para>2. <b>Beyond the cap, the backlog is LOST</b> (§4.1). ⚠ That is the trap of this
        /// rule: the returned leftover is <b>always the sub-tick fraction alone</b>, never the full
        /// backlog. Carrying the full backlog would make the cap entirely useless — the eight cells
        /// of a one-second freeze would go through over eight successive frames instead of one, and
        /// the player would watch them scroll past helplessly, which is exactly the flaw the cap
        /// fixes. Keeping the sub-tick fraction catches nothing up (it is less than one tick by
        /// construction): it only preserves the tick's phase, and that is what leaves the normal
        /// regime identical to the behaviour from before the cap.</para>
        /// </remarks>
        public static int TickCount(
            double accumulatedSeconds,
            double tickDurationSeconds,
            out double leftover,
            int catchUpCap = DefaultCatchUpCap)
        {
            if (tickDurationSeconds <= 0.0 || double.IsNaN(tickDurationSeconds) || double.IsInfinity(tickDurationSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tickDurationSeconds),
                    tickDurationSeconds,
                    "A tick duration must be a finite, strictly positive number.");
            }

            if (catchUpCap < 1)
            {
                // A zero cap would freeze the game without raising anything: the snake would stop.
                throw new ArgumentOutOfRangeException(
                    nameof(catchUpCap),
                    catchUpCap,
                    "A frame must be able to play at least one tick.");
            }

            if (double.IsNaN(accumulatedSeconds) || double.IsInfinity(accumulatedSeconds))
            {
                // A non-finite accumulator can only come from an absurd frame delta upstream:
                // letting it through would produce a negative tick count (cast of infinity) and a
                // snake moving backwards. Better that the caller finds out here.
                throw new ArgumentOutOfRangeException(
                    nameof(accumulatedSeconds),
                    accumulatedSeconds,
                    "Accumulated time must be a finite number.");
            }

            if (accumulatedSeconds <= 0.0)
            {
                leftover = 0.0;
                return 0;
            }

            double ticksDue = Math.Floor(accumulatedSeconds / tickDurationSeconds);

            // The sub-tick fraction, and nothing else. Computed from the ticks DUE rather than the
            // ticks played: that is what discards the backlog instead of carrying it.
            double subTickFraction = accumulatedSeconds - (ticksDue * tickDurationSeconds);
            leftover = subTickFraction >= 0.0 && subTickFraction < tickDurationSeconds ? subTickFraction : 0.0;

            if (ticksDue > catchUpCap)
            {
                // Early return: past the cap, `ticksDue` can exceed the capacity of an int (tiny
                // tick duration), and casting it would yield a negative number.
                return catchUpCap;
            }

            return (int)ticksDue;
        }
    }
}
