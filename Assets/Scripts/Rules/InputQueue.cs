using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>Outcome of a directional press offered to the queue (GDD §4.2 and §3).</summary>
    /// <remarks>
    /// ⚠ The rejection <b>must be observable by the caller</b>: "invisible reads as non-existent"
    /// (§3). A press ignored with no on-screen feedback is read as a press <i>missed by the game</i>
    /// where the game in fact applied a rule. Hence an enum that distinguishes the reasons rather
    /// than a <c>bool</c>: the UI can pick a different feedback per reason if the art director
    /// decides so.
    /// </remarks>
    public enum EnqueueResult
    {
        /// <summary>The press entered the queue. It will be validated at the tick, not now.</summary>
        Accepted,

        /// <summary>
        /// Same direction as the last one already queued (or as the current direction if the queue
        /// is empty): it would change nothing and would use up a slot (§4.2).
        /// </summary>
        RejectedDuplicate,

        /// <summary>
        /// Queue full: the new key is ignored, the oldest is <b>not</b> overwritten (§4.2).
        /// Overwriting would silently cancel a turn that has already left the player's fingers.
        /// </summary>
        RejectedQueueFull,

        /// <summary>Direction pressed during the pause: never queued (§3, §4.2).</summary>
        RejectedGamePaused
    }

    /// <summary>What a tick decided (GDD §4.2).</summary>
    public readonly struct TickResult
    {
        public TickResult(Direction appliedDirection, bool inputConsumed, bool reversalRejected, Direction rejectedDirection)
        {
            AppliedDirection = appliedDirection;
            InputConsumed = inputConsumed;
            ReversalRejected = reversalRejected;
            RejectedDirection = rejectedDirection;
        }

        /// <summary>Direction the snake follows on this tick. Empty queue or rejected input: direction carried over.</summary>
        public Direction AppliedDirection { get; }

        /// <summary>True if an input was dequeued — whether it was applied or rejected.</summary>
        public bool InputConsumed { get; }

        /// <summary>True if the dequeued input was a reversal: it was discarded, and that must show (§3).</summary>
        public bool ReversalRejected { get; }

        /// <summary>The rejected direction. Only meaningful if <see cref="ReversalRejected"/> is true.</summary>
        public Direction RejectedDirection { get; }
    }

    /// <summary>
    /// The input queue of GDD §4.2: a FIFO of depth 2, one input dequeued per tick, reversal
    /// validated <b>at the tick against the direction actually applied on the previous tick</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ Unlike the other files in <c>Rules/</c>, this class is not static: the queue <b>is</b>
    /// state. It stays free of any engine dependency — that is the only criterion that counts here.
    ///
    /// <para><b>The counter-example that forces validation at the tick</b> (§4.2): snake heading
    /// east, the player presses North then South within the same tick. Neither is a reversal of
    /// <i>east</i>; validated on press, both would go through and the next tick would apply South to
    /// a snake that had gone north — it bites its own neck. Validated at the tick, South is compared
    /// with the North <i>actually applied</i>, recognised as a reversal, rejected.</para>
    ///
    /// <para>This is why <see cref="Enqueue"/> <b>never</b> tests for a reversal: doing so would be
    /// exactly the regression that counter-example describes.</para>
    /// </remarks>
    public sealed class InputQueue
    {
        /// <summary>
        /// Default depth: 2 (§4.2, reasoned, to be confirmed in play).
        /// </summary>
        /// <remarks>
        /// At 1, an S-bend pressed in under one tick loses its second half: the player who plays
        /// <i>faster</i> than the rate is punished (ruled out, §7). At 3, the snake executes a
        /// trajectory decided 375 ms earlier in a grid that has changed, and death stops being
        /// attributable to the last turn seen on screen (§2). 2 covers an L-shaped turn made in one
        /// gesture, i.e. 250 ms at 8 ticks/s. ⚠ <b>Depth and rate are linked</b>: revisit one if
        /// <see cref="Cadence.DefaultTicksPerSecond"/> moves.
        /// </remarks>
        public const int DefaultDepth = 2;

        private readonly Queue<Direction> _queue = new Queue<Direction>();
        private readonly int _depth;
        private Direction _currentDirection;
        private bool _paused;

        /// <param name="initialDirection">
        /// Orientation of the starting pose (§4.3: east). The snake stands still but is
        /// <b>oriented</b>: the reversal rule therefore applies from the very first tick.
        /// </param>
        /// <param name="depth">
        /// Queue depth. Parameterised so it stays tunable <b>without recompiling</b> and so tests
        /// can exercise overflow at other depths.
        /// </param>
        public InputQueue(Direction initialDirection, int depth = DefaultDepth)
        {
            if (depth < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depth), depth, "The queue must be able to hold at least one input.");
            }

            _depth = depth;
            _currentDirection = initialDirection;
        }

        /// <summary>Direction actually applied on the last tick — the only reference for reversal (§4.2).</summary>
        public Direction CurrentDirection
        {
            get { return _currentDirection; }
        }

        /// <summary>Number of inputs waiting.</summary>
        public int PendingCount
        {
            get { return _queue.Count; }
        }

        /// <summary>Effective depth of the queue.</summary>
        public int Depth
        {
            get { return _depth; }
        }

        /// <summary>True if the game is paused: no direction is queued then (§3).</summary>
        public bool IsPaused
        {
            get { return _paused; }
        }

        /// <summary>
        /// Offers a directional press to the queue. Reversal is not validated here: it is validated
        /// at the tick (§4.2).
        /// </summary>
        /// <returns>The exact reason, so the caller can show it on screen (§3).</returns>
        public EnqueueResult Enqueue(Direction direction)
        {
            // Order of the tests chosen for the on-screen feedback: the pause explains everything
            // else, and a duplicate is still a duplicate even when the queue is full — announcing
            // "queue full" in that case would give the player a false reason.
            if (_paused)
            {
                return EnqueueResult.RejectedGamePaused;
            }

            if (direction == LastKnownDirection())
            {
                return EnqueueResult.RejectedDuplicate;
            }

            if (_queue.Count >= _depth)
            {
                return EnqueueResult.RejectedQueueFull;
            }

            _queue.Enqueue(direction);
            return EnqueueResult.Accepted;
        }

        /// <summary>
        /// Advances one tick: dequeues <b>one</b> input, validates it against the direction applied
        /// on the previous tick, applies it. With an empty queue the current direction carries over.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The game does not tick while paused. A silent no-op would move the snake one cell during
        /// a pause with nothing to signal it — exactly the class of bug this repository hunts, so we
        /// throw.
        /// </exception>
        public TickResult Tick()
        {
            if (_paused)
            {
                throw new InvalidOperationException(
                    "The game does not tick while paused: call Resume() before ticking.");
            }

            if (_queue.Count == 0)
            {
                return new TickResult(_currentDirection, false, false, _currentDirection);
            }

            Direction requested = _queue.Dequeue();

            if (Directions.IsReversal(_currentDirection, requested))
            {
                // The rejected input is discarded — it does not block the queue — and the tick
                // carries the current direction over (§4.2).
                return new TickResult(_currentDirection, true, true, requested);
            }

            _currentDirection = requested;
            return new TickResult(_currentDirection, true, false, _currentDirection);
        }

        /// <summary>
        /// Entering the pause: the queue is emptied (§4.2).
        /// </summary>
        /// <remarks>
        /// Resuming must restore the state <b>visible on screen</b>, not execute a turn pressed
        /// before the pause: the player has looked at the frozen grid and plays again from what they
        /// see.
        /// </remarks>
        public void Pause()
        {
            _paused = true;
            _queue.Clear();
        }

        /// <summary>Leaving the pause. The queue stays empty: it was purged on entering.</summary>
        public void Resume()
        {
            _paused = false;
        }

        /// <summary>
        /// Death of the snake: the queue is emptied (§4.2), so that no turn pressed during the death
        /// throes is applied to the next game.
        /// </summary>
        public void Die()
        {
            _queue.Clear();
        }

        /// <summary>
        /// New game: empty queue, pause lifted, direction reset to the starting orientation.
        /// </summary>
        public void Reset(Direction initialDirection)
        {
            _queue.Clear();
            _paused = false;
            _currentDirection = initialDirection;
        }

        /// <summary>
        /// The last "known" direction for the duplicate test: the last one in the queue, or the
        /// current direction if the queue is empty (§4.2).
        /// </summary>
        private Direction LastKnownDirection()
        {
            if (_queue.Count == 0)
            {
                return _currentDirection;
            }

            Direction last = _currentDirection;
            foreach (Direction direction in _queue)
            {
                last = direction;
            }

            return last;
        }
    }
}
