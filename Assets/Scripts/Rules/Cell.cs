#nullable enable
using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Integer coordinate of one grid cell, zero-indexed (GDD §4.3).
    /// </summary>
    /// <remarks>
    /// ⚠ This type exists <b>because <c>Vector2Int</c> comes from <c>UnityEngine</c></b>: using it
    /// here would make all of <c>Rules/</c> impossible to compile outside the engine, and therefore
    /// impossible to test in a few milliseconds with <c>dotnet test</c>. Converting to the engine
    /// type is the caller's job.
    ///
    /// <para>Integer rather than floating point: the snake "moves one cell per tick, never between
    /// two ticks" (§4.1). A floating position would allow an off-grid state and would make
    /// collision — and therefore death — depend on an epsilon.</para>
    /// </remarks>
    public readonly struct Cell : IEquatable<Cell>
    {
        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        /// <summary>Component-wise sum (a cell plus a step).</summary>
        public Cell Plus(Cell other)
        {
            return new Cell(X + other.X, Y + other.Y);
        }

        public bool Equals(Cell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object? obj)
        {
            return obj is Cell other && Equals(other);
        }

        public override int GetHashCode()
        {
            // Good enough for a grid of a few hundred cells: the head is tested against the body
            // every tick, so through a HashSet on the caller's side.
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(Cell left, Cell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Cell left, Cell right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }
    }
}
