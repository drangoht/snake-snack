using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>What one snake step produced (GDD §2: "the head touches the body or a wall").</summary>
    public enum MoveResult
    {
        /// <summary>The snake moved one cell.</summary>
        Moved,

        /// <summary>The head left the grid: death against a wall (§2, edges kill).</summary>
        HitWall,

        /// <summary>The head entered its own body: death by bite (§1, the game's true opponent).</summary>
        BitSelf
    }

    /// <summary>
    /// The snake's body and its only verb: move one cell (GDD §4.1).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A lethal move does not move the snake.</b> The head is never written outside the grid
    /// nor inside its own body: the state stays the one from the last living tick. Without that, the
    /// renderer would draw a head outside the playfield during the frame of death — the player would
    /// see the snake go through the wall, exactly what §2 forbids letting anyone believe.
    ///
    /// <para>⚠ <b>The tail frees its cell on the same tick — except on the tick of an apple.</b>
    /// Entering the cell the tail is leaving is <b>not</b> a bite: it is the normal manoeuvre of a
    /// snake following its own trail, and refusing it would kill on a move the player watches free
    /// itself on screen. But on the tick where the snake eats, the tail <b>does not move</b> (§4.4):
    /// it becomes an obstacle again. So the exception has an exception of its own, and that is
    /// exactly where the off-by-one cell bug hides.</para>
    ///
    /// <para>A stateful class, like <see cref="InputQueue"/>, with no engine dependency whatsoever:
    /// that is the only criterion that matters for <c>Rules/</c>.</para>
    /// </remarks>
    public sealed class Snake
    {
        private readonly List<Cell> _segments = new List<Cell>();

        /// <param name="segments">
        /// Segments from the head (index 0) to the tail — typically
        /// <see cref="Grid.StartingPose"/>.
        /// </param>
        public Snake(IReadOnlyList<Cell> segments)
        {
            Reset(segments);
        }

        /// <summary>Segments, from the head (index 0) to the tail.</summary>
        public IReadOnlyList<Cell> Segments
        {
            get { return _segments; }
        }

        /// <summary>The head cell.</summary>
        public Cell Head
        {
            get { return _segments[0]; }
        }

        /// <summary>Number of segments.</summary>
        public int Length
        {
            get { return _segments.Count; }
        }

        /// <summary>True if a segment occupies this cell (head included).</summary>
        public bool Occupies(Cell cell)
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i] == cell)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Lays the snake down again, typically for a new game.</summary>
        public void Reset(IReadOnlyList<Cell> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            if (segments.Count < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(segments), segments.Count, "A snake has at least one segment.");
            }

            _segments.Clear();
            for (int i = 0; i < segments.Count; i++)
            {
                _segments.Add(segments[i]);
            }
        }

        /// <summary>
        /// Moves one cell in this direction, with no apple on the grid.
        /// </summary>
        /// <remarks>
        /// Convenience overload for the cases where the apple plays no part (wall and bite tests).
        /// The game always calls the full form: under §4.4 there is an apple on the grid <b>at every
        /// instant</b>.
        /// </remarks>
        public MoveResult Advance(Direction direction, Grid grid)
        {
            bool ignored;
            return Advance(direction, grid, null, out ignored);
        }

        /// <summary>
        /// Plays the tick of GDD §4.4: move one cell, eat, grow, or die.
        /// </summary>
        /// <param name="direction">Direction already validated by <see cref="InputQueue.Tick"/>.</param>
        /// <param name="grid">The playfield — its edges kill (§2).</param>
        /// <param name="apple">The apple's cell, or <c>null</c> if there is none.</param>
        /// <param name="ate">
        /// True if the head has just entered the apple's cell. ⚠ <b>Always false when the snake
        /// dies</b>: a lethal step does not eat, even towards the apple's cell.
        /// </param>
        /// <remarks>
        /// The direction is <b>not</b> validated here: reversal is judged by
        /// <see cref="InputQueue.Tick"/>, against the direction actually applied on the previous
        /// tick (§4.2). Duplicating that judgement here would make two truths exist about the same
        /// rule, and it is the second one that would end up drifting.
        ///
        /// <para>⚠ <b>The order of the steps is that of GDD §4.4, to the letter</b>: wall, then
        /// growth, then bite, then move. Testing the bite before knowing whether the snake eats
        /// would lose the tail exclusion — or keep it wrongly. Both mistakes are invisible on
        /// reading and obvious on screen: a death one cell too early, or a snake going through
        /// itself.</para>
        ///
        /// <para>⚠ The snake <b>grows from the head</b>, on the very tick it enters the apple — not
        /// on the next tick, not by a segment appended behind the tail. Length goes from N to N+1
        /// immediately, and always equals <c>3 + score</c> (§4.5).</para>
        /// </remarks>
        public MoveResult Advance(Direction direction, Grid grid, Cell? apple, out bool ate)
        {
            ate = false;

            // 1 and 2 — the target cell, then the wall.
            Cell next = Directions.Advance(Head, direction);

            if (grid.IsOutside(next))
            {
                return MoveResult.HitWall;
            }

            // 3 — eating is decided BEFORE the collision, because it decides the tail's fate.
            bool growing = apple.HasValue && apple.Value == next;

            // 4 — collision: the tail is excluded only if it moves, that is, if we do not eat.
            //     ⚠ Written without assuming an apple never appears on the body: that guarantee is
            //     established at step 6, elsewhere, and a rule must not depend on a guarantee it
            //     does not carry itself.
            int obstacles = growing ? _segments.Count : _segments.Count - 1;
            for (int i = 0; i < obstacles; i++)
            {
                if (_segments[i] == next)
                {
                    return MoveResult.BitSelf;
                }
            }

            // 5 — insert the head; drop the tail only if we are not eating. Duplicating the tail
            //     before the shift is exactly the same as "do not drop it": the loop below is then
            //     identical in both cases, so there is only one path to re-read.
            if (growing)
            {
                _segments.Add(_segments[_segments.Count - 1]);
            }

            for (int i = _segments.Count - 1; i > 0; i--)
            {
                _segments[i] = _segments[i - 1];
            }

            _segments[0] = next;

            ate = growing;
            return MoveResult.Moved;
        }
    }
}
