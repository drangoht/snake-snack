using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>The snake's pose when a game starts (GDD §4.3).</summary>
    public readonly struct StartPose
    {
        public StartPose(IReadOnlyList<Cell> segments, Direction orientation)
        {
            Segments = segments;
            Orientation = orientation;
        }

        /// <summary>Segments <b>head first</b>: the order carries the geometry of the body.</summary>
        public IReadOnlyList<Cell> Segments { get; }

        /// <summary>Starting orientation. The snake stands still but is oriented (§4.3).</summary>
        public Direction Orientation { get; }

        /// <summary>The head cell.</summary>
        public Cell Head
        {
            get { return Segments[0]; }
        }

        /// <summary>Snake length at the start (§2 and §4.3: 3).</summary>
        public int Length
        {
            get { return Segments.Count; }
        }
    }

    /// <summary>
    /// The playfield of GDD §4.3: dimensions, exact centre cell, starting pose, and the
    /// "cell outside the grid" test that carries the lethal wall of §2.
    /// </summary>
    /// <remarks>
    /// A value type carrying its own dimensions rather than a static class of constants: that is
    /// what makes the grid tunable <b>without recompiling</b> (§4.3). The engine-side caller reads
    /// width and height from a JSON file in <c>StreamingAssets</c> and builds a <see cref="Grid"/>;
    /// the fallback is <see cref="Default"/>.
    /// </remarks>
    public readonly struct Grid
    {
        /// <summary>Default width: 21 cells (§4.3, by judgement).</summary>
        public const int DefaultWidth = 21;

        /// <summary>Default height: 15 cells (§4.3, by judgement). 315 cells in total.</summary>
        public const int DefaultHeight = 15;

        /// <summary>Snake length at the start: 3 segments (§2, repeated in §4.3).</summary>
        public const int InitialLength = 3;

        /// <summary>Starting orientation: east (§4.3).</summary>
        public const Direction InitialOrientation = Direction.East;

        /// <param name="width">Number of columns. <b>Must be odd.</b></param>
        /// <param name="height">Number of rows. <b>Must be odd.</b></param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Even dimension, or too small to lay down the starting snake.
        /// </exception>
        /// <remarks>
        /// ⚠ <b>Rejecting even dimensions is a design rule, not fussiness</b> (§4.3): without an odd
        /// axis there is no exact centre cell, and the snake would appear offset by half a cell —
        /// the "in the centre" of §2 would become false. An even grid raises nothing at runtime: it
        /// merely produces a slightly crooked pose that nobody notices before a screenshot. Hence
        /// failing here, at the earliest possible moment. (It is also what ruled out the 32 × 18
        /// grid, §7.)
        /// </remarks>
        public Grid(int width, int height)
        {
            // Minimum width: the head takes the centre column and the two body segments extend
            // westwards, so (width - 1) / 2 must be at least InitialLength - 1.
            const int minimumWidth = 2 * (InitialLength - 1) + 1;

            if (width < minimumWidth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width), width,
                    "The grid must be wide enough for the starting pose (at least " + minimumWidth + " columns).");
            }

            if (height < 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "The grid needs at least 3 rows for a turn to exist.");
            }

            if (width % 2 == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width), width, "Width must be odd: without it there is no exact centre cell (GDD §4.3).");
            }

            if (height % 2 == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "Height must be odd: without it there is no exact centre cell (GDD §4.3).");
            }

            Width = width;
            Height = height;
        }

        /// <summary>Number of columns.</summary>
        public int Width { get; }

        /// <summary>Number of rows.</summary>
        public int Height { get; }

        /// <summary>The grid of GDD §4.3: 21 × 15.</summary>
        public static Grid Default
        {
            get { return new Grid(DefaultWidth, DefaultHeight); }
        }

        /// <summary>Total number of cells (315 by default).</summary>
        public int CellCount
        {
            get { return Width * Height; }
        }

        /// <summary>
        /// The exact centre cell, zero-indexed: <c>(10, 7)</c> on the default grid (§4.3).
        /// </summary>
        public Cell Centre
        {
            get { return new Cell((Width - 1) / 2, (Height - 1) / 2); }
        }

        /// <summary>True if the cell belongs to the playfield.</summary>
        public bool Contains(Cell cell)
        {
            return cell.X >= 0
                   && cell.X < Width
                   && cell.Y >= 0
                   && cell.Y < Height;
        }

        /// <summary>
        /// True if the cell lies outside the playfield — <b>that is death</b> (§2).
        /// </summary>
        /// <remarks>
        /// ⚠ Edges kill, they do not teleport. No modulo anywhere: the day somebody writes one "to
        /// avoid a negative index", they silently reintroduce the wrapping edges ruled out in §7,
        /// and death stops being attributable to the last turn (§2). A closed grid reads at a
        /// glance; a wrapping edge asks the player to simulate an invisible continuity in their head.
        /// </remarks>
        public bool IsOutside(Cell cell)
        {
            return !Contains(cell);
        }

        /// <summary>
        /// The starting pose (§4.3): head on the centre cell, body lined up behind it towards the
        /// west, length 3, facing east.
        /// </summary>
        /// <remarks>
        /// The body extends <b>opposite the orientation</b>: that is what gives
        /// <c>(10, 7) (9, 7) (8, 7)</c> on the default grid. Laying the body in front of the head
        /// would kill the snake on the first tick, with no symptom other than a game that does not
        /// start.
        /// </remarks>
        public StartPose StartingPose()
        {
            Cell backOneStep = Directions.Step(Directions.Opposite(InitialOrientation));
            Cell[] segments = new Cell[InitialLength];
            segments[0] = Centre;

            for (int i = 1; i < InitialLength; i++)
            {
                segments[i] = segments[i - 1].Plus(backOneStep);
            }

            return new StartPose(segments, InitialOrientation);
        }
    }
}
