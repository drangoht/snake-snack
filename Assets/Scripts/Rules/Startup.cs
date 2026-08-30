namespace SnakeSnack.Rules
{
    /// <summary>What the first directional press of a game triggers (GDD §4.1).</summary>
    public enum StartDecision
    {
        /// <summary>The game starts: this tick is the first one.</summary>
        Starts,

        /// <summary>
        /// Reversal: the rejection shows (§3) and <b>nothing moves</b>. The game does not start.
        /// </summary>
        RejectedReversal
    }

    /// <summary>
    /// The standing start (GDD §4.1, author's ruling of 2026-08-27): the first tick is triggered by
    /// the first <b>applicable</b> direction, not by any press at all.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This rule cannot live in <see cref="InputQueue"/></b>, and the GDD says so explicitly:
    /// the queue never judges a reversal on enqueue — it judges it at the tick, against the
    /// direction actually applied (§4.2, North/South counter-example). But at startup no tick has
    /// happened yet: the <i>starting orientation</i> is the reference, and it is up to the engine
    /// wiring to decide. Hence this class, tiny but named: without it the decision dissolves into an
    /// <c>if</c> in the middle of an <c>Update()</c> and nobody can test or re-read it any more.
    ///
    /// <para>⚠ <b>A duplicate starts the game.</b> A player pressing East on a snake already facing
    /// east gets <see cref="EnqueueResult.RejectedDuplicate"/> from the queue — but §4.1 says "the
    /// game starts on the first press that is not a reversal", and pressing the heading you are
    /// already following is a perfectly clear intent: "go". Making the start depend on the enqueue
    /// result would give a game that refuses to start on the Right arrow, showing nothing (a
    /// duplicate has no visual feedback, <c>docs/ART.md</c> §5.3): the player would conclude the
    /// game is broken.</para>
    /// </remarks>
    public static class Startup
    {
        /// <param name="startingOrientation">Orientation of the resting snake (§4.3: east).</param>
        /// <param name="requestedDirection">Direction the player just pressed.</param>
        public static StartDecision Decide(Direction startingOrientation, Direction requestedDirection)
        {
            return Directions.IsReversal(startingOrientation, requestedDirection)
                ? StartDecision.RejectedReversal
                : StartDecision.Starts;
        }
    }
}
