namespace SnakeSnack.Rules
{
    /// <summary>The snake's four directions (GDD §3: "Turn (4 directions)").</summary>
    /// <remarks>
    /// Axis convention: <b>North = increasing Y</b>, like Unity's Y axis which points up on screen.
    /// The renderer therefore has no inversion to perform between the logical grid and the scene —
    /// an inversion forgotten somewhere would translate into a game that answers "backwards"
    /// without raising the slightest error.
    /// </remarks>
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    /// <summary>Pure operations on <see cref="Direction"/> — no engine dependency.</summary>
    public static class Directions
    {
        /// <summary>Every direction, in enum order (useful to tests and to the UI).</summary>
        public static Direction[] All()
        {
            // A fresh array on every call: a shared static array would be mutable by the caller, and
            // the direction table has no reason to be global state.
            return new[] { Direction.North, Direction.East, Direction.South, Direction.West };
        }

        /// <summary>The opposite direction (North to South, East to West).</summary>
        public static Direction Opposite(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return Direction.South;
                case Direction.South: return Direction.North;
                case Direction.East: return Direction.West;
                default: return Direction.East;
            }
        }

        /// <summary>
        /// True if going from <paramref name="applied"/> to <paramref name="requested"/> is an
        /// instant reversal — the snake would bite its own neck (GDD §3).
        /// </summary>
        /// <remarks>
        /// ⚠ <paramref name="applied"/> must be the direction <b>actually applied on the previous
        /// tick</b>, never the last key pressed: that is the whole point of the North/South
        /// counter-example in GDD §4.2. This rule is exposed publicly so the caller can show a
        /// rejection (§3) without reimplementing the comparison on its own side.
        /// </remarks>
        public static bool IsReversal(Direction applied, Direction requested)
        {
            return requested == Opposite(applied);
        }

        /// <summary>
        /// Sign of the turn between two successive directions: <c>+1</c> to the left
        /// (counter-clockwise), <c>-1</c> to the right (clockwise), <c>0</c> if the snake carries
        /// straight on.
        /// </summary>
        /// <remarks>
        /// The sign follows Unity's angle convention — increasing Z turns counter-clockwise — which
        /// lets the caller multiply straight by an angle in degrees (<c>docs/art/juicy.md</c> §9)
        /// without reinventing the orientation.
        ///
        /// <para>⚠ <b>A reversal returns 0, not some arbitrary side.</b> It cannot happen in play —
        /// the queue rejects it at the tick (GDD §4.2) — but if it did, picking left or right would
        /// be an invention: both quarter turns are equally wrong. Zero means "no turn to show", and
        /// it is the only honest answer here.</para>
        ///
        /// <para>⚠ <b>Presentation only.</b> No rule reads this sign: the trajectory is already
        /// decided by the direction applied at the tick (<c>juicy.md</c> §11).</para>
        /// </remarks>
        public static int TurnSign(Direction before, Direction after)
        {
            // The enum is ordered clockwise (North, East, South, West): one step forward in that
            // list is a right turn, one step back is a left turn.
            int quarters = (((int)after - (int)before) + 4) % 4;

            switch (quarters)
            {
                case 1: return -1;
                case 3: return 1;
                default: return 0;
            }
        }

        /// <summary>One-cell step in this direction.</summary>
        public static Cell Step(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return new Cell(0, 1);
                case Direction.South: return new Cell(0, -1);
                case Direction.East: return new Cell(1, 0);
                default: return new Cell(-1, 0);
            }
        }

        /// <summary>The cell reached from <paramref name="start"/> after one step in this direction.</summary>
        public static Cell Advance(Cell start, Direction direction)
        {
            return start.Plus(Step(direction));
        }
    }
}
