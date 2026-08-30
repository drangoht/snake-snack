#nullable enable
using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// A point on screen, in pixels, origin at the centre of the frame and <b>Y pointing up</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ This type exists for the same reason as <see cref="Cell"/>: <c>Vector2</c> comes from
    /// <c>UnityEngine</c>, and importing it here would make the whole board geometry untestable
    /// outside the engine. Converting to the engine type belongs to the caller.
    ///
    /// <para>Y upwards, like Unity's Y axis and like the North = increasing Y convention of
    /// <see cref="Direction"/>: no inversion anywhere, so no inversion to forget.</para>
    /// </remarks>
    public readonly struct BoardPoint : IEquatable<BoardPoint>
    {
        public BoardPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }

        public bool Equals(BoardPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object? obj)
        {
            return obj is BoardPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }
    }

    /// <summary>
    /// The layout of the playfield (GDD §4.3): cell size derived from the frame, and the on-screen
    /// position of every cell.
    /// </summary>
    /// <remarks>
    /// §4.3 states the arithmetic: "in a 1280×720 web frame with a HUD band of about 60 px, a cell
    /// is <c>min(1280/21, 660/15)</c> = 44 px". That is a <b>numeric formula</b>, so it lives here
    /// and not in a <c>MonoBehaviour</c>: the day the grid becomes 25 × 17, nobody has to redo the
    /// division by hand.
    ///
    /// <para>⚠ <b>Unit: one pixel of the 1280×720 reference frame.</b> The engine wiring sets the
    /// camera so that one world unit is exactly one pixel of that frame
    /// (<c>orthographicSize = 360</c>). Without that equality, every value in GDD §4.3 would become
    /// wrong on screen with nothing to signal it — you would just see a game that is "not quite the
    /// right scale".</para>
    /// </remarks>
    public readonly struct Board
    {
        /// <summary>Reference frame width: 1280 px (GDD §4.3).</summary>
        public const int DefaultFrameWidth = 1280;

        /// <summary>Reference frame height: 720 px (GDD §4.3).</summary>
        public const int DefaultFrameHeight = 720;

        /// <summary>Height of the HUD band: ~60 px (GDD §4.3).</summary>
        public const int DefaultBandHeight = 60;

        /// <summary>
        /// The largest whole cell size that lets the grid fit inside the frame, HUD band deducted
        /// (GDD §4.3).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Rounded down, and a whole number.</b> A fractional size (43.7 px) would place cells
        /// between two pixels: grid lines would become irregular, every other line one pixel
        /// thicker, which raises nothing and reads as a drawing defect. Rounding up would push the
        /// grid outside the frame — the lethal wall of §2 would leave the screen.
        /// </remarks>
        public static int CellSizeFor(
            Grid grid,
            int frameWidth = DefaultFrameWidth,
            int frameHeight = DefaultFrameHeight,
            int bandHeight = DefaultBandHeight)
        {
            if (frameWidth <= 0 || frameHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frameWidth), frameWidth,
                    "The frame must have strictly positive dimensions.");
            }

            if (bandHeight < 0 || bandHeight >= frameHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bandHeight), bandHeight,
                    "The HUD band must leave room for the playfield.");
            }

            double byWidth = (double)frameWidth / grid.Width;
            double byHeight = (double)(frameHeight - bandHeight) / grid.Height;
            int size = (int)Math.Floor(Math.Min(byWidth, byHeight));

            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grid), grid.CellCount,
                    "The grid is too large for the frame: a cell would be under one pixel.");
            }

            return size;
        }

        /// <param name="grid">The logical playfield.</param>
        /// <param name="cellSize">A cell's side, in pixels — from <see cref="CellSizeFor"/>.</param>
        /// <param name="bandHeight">Height of the HUD band, in pixels.</param>
        public Board(Grid grid, int cellSize, int bandHeight = DefaultBandHeight)
        {
            if (cellSize < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellSize), cellSize, "A cell is at least one pixel.");
            }

            if (bandHeight < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bandHeight), bandHeight,
                    "The HUD band cannot have a negative height.");
            }

            Grid = grid;
            CellSize = cellSize;
            BandHeight = bandHeight;
        }

        /// <summary>The logical playfield this layout applies to.</summary>
        public Grid Grid { get; }

        /// <summary>A cell's side, in pixels.</summary>
        public int CellSize { get; }

        /// <summary>Height of the HUD band reserved at the top of the frame, in pixels.</summary>
        public int BandHeight { get; }

        /// <summary>Playfield width, in pixels (924 by default).</summary>
        public int PlayfieldWidth
        {
            get { return Grid.Width * CellSize; }
        }

        /// <summary>Playfield height, in pixels (660 by default).</summary>
        public int PlayfieldHeight
        {
            get { return Grid.Height * CellSize; }
        }

        /// <summary>
        /// Vertical offset of the playfield centre relative to the frame centre.
        /// </summary>
        /// <remarks>
        /// The playfield is centred in what is left of the frame <b>once the band is taken off the
        /// top</b>: the middle of <c>[-H/2 ; H/2 - band]</c> is <c>-band/2</c>, whatever the size of
        /// the playfield. Putting the playfield at the centre of the whole frame would slide it
        /// under the band — the HUD would cover the top row of cells, which raises nothing and kills
        /// the player against a wall they never saw.
        /// </remarks>
        public double PlayfieldVerticalOffset
        {
            get { return -BandHeight / 2.0; }
        }

        /// <summary>Centre of a cell, in pixels, origin at the centre of the frame.</summary>
        public BoardPoint CellCentre(Cell cell)
        {
            double x = ((cell.X + 0.5) * CellSize) - (PlayfieldWidth / 2.0);
            double y = ((cell.Y + 0.5) * CellSize) - (PlayfieldHeight / 2.0) + PlayfieldVerticalOffset;
            return new BoardPoint(x, y);
        }

        /// <summary>
        /// Largest size for the rejection pictogram: half a cell (<c>docs/ART.md</c> §5.4).
        /// </summary>
        public double MaximumPictogramSize
        {
            get { return CellSize / 2.0; }
        }

        /// <summary>
        /// Where to put the rejection pictogram: at the edge of the head cell, on the side of the
        /// rejected direction (<c>docs/ART.md</c> §5.4).
        /// </summary>
        /// <remarks>
        /// The brief says "anchored to the edge of the head cell, on the side of the rejected
        /// direction, offset by about a quarter of a cell (~11 px) so it never covers the cell
        /// itself". The edge is half a cell from the centre, plus a quarter cell of offset: three
        /// quarters of a cell. With a pictogram half a cell at most, it then occupies exactly the
        /// space between the edge of the head cell and the centre of the neighbouring one — never on
        /// the head (that would hide it), never past the neighbour's centre (that would read as an
        /// obstacle placed on the grid).
        /// </remarks>
        public BoardPoint RejectionAnchor(Cell head, Direction rejectedDirection)
        {
            BoardPoint centre = CellCentre(head);
            Cell step = Directions.Step(rejectedDirection);
            double distance = CellSize * 0.75;
            return new BoardPoint(centre.X + (step.X * distance), centre.Y + (step.Y * distance));
        }
    }
}
