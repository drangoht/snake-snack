using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Where to put the apple (GDD §4.4). Only one on the grid, never on the snake.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This class answers "where" and "how many", never "when"</b> (GDD §4.4): resolving the
    /// tick — the exact order between wall, bite, growth and the next draw — belongs to
    /// <see cref="Snake"/>. Two places deciding the same sequence is an off-by-one-cell bug that
    /// only shows up on screen.
    ///
    /// <para>⚠ <b>The draw enumerates, it does not reject.</b> "Draw a random cell and start over
    /// while it is occupied" is the trap of this system: on a nearly full grid the expected number
    /// of draws tends to infinity and the game <b>freezes without raising the slightest error</b> —
    /// no exception, no log, just a frame that never comes back. And the flaw only appears at the
    /// end of a long game, which is to say never during testing. Here the cost is <b>bounded</b>:
    /// a single pass over the grid, whatever the fill level.</para>
    /// </remarks>
    public static class Apple
    {
        /// <summary>
        /// Number of cells the apple can land on.
        /// </summary>
        /// <remarks>
        /// The snake occupies <b>exactly</b> <paramref name="snakeLength"/> distinct cells: two
        /// overlapping segments would mean it has bitten itself, so that the game is over
        /// (GDD §4.4). The subtraction is therefore correct without walking the body.
        /// </remarks>
        public static int FreeCellCount(Grid grid, int snakeLength)
        {
            if (snakeLength < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(snakeLength), snakeLength, "A snake does not have a negative length.");
            }

            if (snakeLength > grid.CellCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(snakeLength), snakeLength,
                    "The snake cannot occupy more cells than the grid contains.");
            }

            return grid.CellCount - snakeLength;
        }

        /// <summary>
        /// True if the snake fills the grid: <b>that is the win</b> (GDD §4.4), not an error.
        /// </summary>
        /// <remarks>
        /// ⚠ To be tested <b>before</b> <see cref="Draw"/>, never after: with no free cell the draw
        /// has no value to return. This state is out of human reach (312 apples on the default grid)
        /// and must be written all the same — it is exactly the kind of branch nobody writes
        /// "because it will never happen", and that breaks the day an automated bench plays a
        /// perfect game.
        /// </remarks>
        public static bool GridIsFull(Grid grid, int snakeLength)
        {
            return FreeCellCount(grid, snakeLength) == 0;
        }

        /// <summary>
        /// The <paramref name="rank"/>-th free cell, walking <b>increasing X within increasing
        /// Y</b> (GDD §4.4).
        /// </summary>
        /// <param name="grid">The playfield.</param>
        /// <param name="occupiedCells">The snake's segments — their order is irrelevant here.</param>
        /// <param name="rank">Rank of the wanted cell, in <c>[0, FreeCellCount)</c>.</param>
        /// <remarks>
        /// ⚠ <b>The walk order is part of the contract</b>, not an implementation detail: it is what
        /// gives the same game on all three targets for a given seed. Swapping it (Y within X) would
        /// break no uniformity test and would break every bench pairing.
        ///
        /// <para>This method is <b>kept separate from the draw</b> so it can be tested without a
        /// generator: give it a rank, it returns a cell, and the assertion is on an exact value.</para>
        /// </remarks>
        public static Cell FreeCellAtRank(Grid grid, IReadOnlyList<Cell> occupiedCells, int rank)
        {
            if (occupiedCells == null)
            {
                throw new ArgumentNullException(nameof(occupiedCells));
            }

            int free = FreeCellCount(grid, occupiedCells.Count);

            if (rank < 0 || rank >= free)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rank), rank,
                    "There are only " + free + " free cell(s): the rank must fall inside that.");
            }

            int remaining = rank;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Cell candidate = new Cell(x, y);

                    if (IsOccupied(occupiedCells, candidate))
                    {
                        continue;
                    }

                    if (remaining == 0)
                    {
                        return candidate;
                    }

                    remaining--;
                }
            }

            // Unreachable as long as `occupiedCells` fits inside the grid and holds no duplicate —
            // the two conditions of §4.4. Thrown rather than returned silently: an apple placed
            // "somewhere" would be undetectable on reading and obvious on screen.
            throw new InvalidOperationException(
                "Occupied cells overflow the grid or contain a duplicate: the free-cell count is wrong.");
        }

        /// <summary>
        /// Draws the cell of the next apple (GDD §4.4).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The grid is full. The caller must have handled the win with <see cref="GridIsFull"/>
        /// before reaching this point.
        /// </exception>
        /// <remarks>
        /// ⚠ <b>No placement constraint whatsoever</b> (§4.4): no minimum distance from the head, no
        /// ban on the cell straight ahead of it. Eating is never compulsory, so no position can do
        /// harm — constraining would only take <i>favourable</i> apples away from the player, while
        /// making every bench harder to describe.
        /// </remarks>
        public static Cell Draw(Grid grid, IReadOnlyList<Cell> occupiedCells, RandomSource random)
        {
            if (occupiedCells == null)
            {
                throw new ArgumentNullException(nameof(occupiedCells));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            int free = FreeCellCount(grid, occupiedCells.Count);

            if (free == 0)
            {
                throw new InvalidOperationException(
                    "No free cell: a full grid is a win (GDD §4.4), and it is handled before the draw.");
            }

            return FreeCellAtRank(grid, occupiedCells, random.NextInt(free));
        }

        /// <summary>
        /// Linear scan, with no allocation and no intermediate table.
        /// </summary>
        /// <remarks>
        /// The cost of a full draw is therefore at worst <c>CellCount × length</c> integer
        /// comparisons — 315 × 315 on the default grid, and only in the position where the snake
        /// fills everything. That cost is paid <b>only on ticks where an apple is eaten</b>, never
        /// on the others. Building a <c>HashSet</c> here would cost one allocation per apple, so a
        /// regular garbage collection — visible in WebGL as micro-stutters, and a stutter shifts the
        /// reading of a turn.
        /// </remarks>
        private static bool IsOccupied(IReadOnlyList<Cell> occupiedCells, Cell candidate)
        {
            for (int i = 0; i < occupiedCells.Count; i++)
            {
                if (occupiedCells[i] == candidate)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
