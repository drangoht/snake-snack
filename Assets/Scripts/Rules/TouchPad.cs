using System;

namespace SnakeSnack.Rules
{
    /// <summary>What an on-screen control a finger landed on asks for (GDD §3, touch).</summary>
    public enum TouchTarget
    {
        /// <summary>Nothing: the point is on the playfield, in a gap, or outside the controls.</summary>
        None,
        North,
        South,
        East,
        West,

        /// <summary>The pause button — a mobile player has no Esc key.</summary>
        Pause
    }

    /// <summary>
    /// Where the on-screen controls sit, and what a finger landing at a point asks for
    /// (GDD §3, touch — reopened on 2026-08-30).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Unit: one pixel of the 1280×720 reference frame</b>, origin at the frame centre, Y
    /// upwards — the same frame as <see cref="Board"/>, so the two can be compared without any
    /// conversion.
    ///
    /// <para><b>Why the controls cost the playfield nothing.</b> A 21 × 15 grid at 44 px is 924 px
    /// wide in a 1280 px frame: 178 px of margin are already there, on each side, doing nothing. The
    /// pad lives in the right margin and the pause button in the left one. Shrinking the grid to
    /// make room — the obvious move — would have made every cell smaller for a player whose screen
    /// is already the smallest, and GDD §4.3 sizes the cell to be readable.</para>
    ///
    /// <para>⚠ <b>The margin is not guaranteed.</b> It is what the rounding of
    /// <see cref="Board.CellSizeFor"/> happens to leave: a 25 × 17 grid leaves 165 px, not 178, and
    /// a wider one would leave none. The pad therefore <b>shrinks to the margin it is given</b> and
    /// throws below <see cref="MinimumButtonSize"/> rather than quietly drawing itself over the
    /// playfield, where it would swallow the swipes meant for the game.</para>
    ///
    /// <para><b>Right-handed by construction</b>, and knowingly: the pad is on the right, the pause
    /// on the left. Mirroring it is a settings screen, and GDD §4.6 rules a "Settings" entry out for
    /// now.</para>
    /// </remarks>
    public readonly struct TouchPad
    {
        /// <summary>Side of a control, in reference pixels, when the margin allows it.</summary>
        public const double DefaultButtonSize = 54.0;

        /// <summary>Gap between two controls of the pad, in reference pixels.</summary>
        public const double DefaultGap = 3.0;

        /// <summary>
        /// Below this side, a control is no longer reliably hittable with a thumb: ~36 px of the
        /// reference frame is about 6 mm on a phone held in landscape, the usual floor for a touch
        /// target. Under it the pad refuses to exist rather than existing badly.
        /// </summary>
        public const double MinimumButtonSize = 36.0;

        /// <summary>Distance kept between the pad and the bottom edge of the frame.</summary>
        public const double BottomInset = 40.0;

        /// <summary>Distance kept between the pause button and the HUD band above it.</summary>
        public const double UnderBandInset = 10.0;

        /// <summary>
        /// Whether the margin left by the playfield can hold a thumb-sized pad.
        /// </summary>
        /// <remarks>
        /// ⚠ The caller that draws asks this <b>before</b> building, because the alternative to a pad
        /// is not a crash: it is a game steered by swipes alone, which still plays. The constructor
        /// keeps throwing — a caller that builds without asking has made a mistake, and a silent
        /// half-drawn pad would be worse than an exception.
        /// </remarks>
        public static bool Fits(Board board, int frameWidth = Board.DefaultFrameWidth)
        {
            double margin = (frameWidth / 2.0) - (board.PlayfieldWidth / 2.0);
            double pitch = Math.Min(DefaultButtonSize + DefaultGap, margin / 3.0);
            return pitch - DefaultGap >= MinimumButtonSize;
        }

        /// <param name="board">The playfield layout the controls must not overlap.</param>
        /// <param name="frameWidth">Reference frame width, in pixels.</param>
        /// <param name="frameHeight">Reference frame height, in pixels.</param>
        public TouchPad(
            Board board,
            int frameWidth = Board.DefaultFrameWidth,
            int frameHeight = Board.DefaultFrameHeight)
        {
            double margin = (frameWidth / 2.0) - (board.PlayfieldWidth / 2.0);

            // The lattice is three controls wide: the margin has to hold three pitches, or the pad
            // reaches over the playfield.
            double pitch = Math.Min(DefaultButtonSize + DefaultGap, margin / 3.0);
            double size = pitch - DefaultGap;

            if (size < MinimumButtonSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(board), margin,
                    "The playfield leaves no margin wide enough for a thumb-sized pad: "
                    + "either narrow the grid or move the controls over the playfield deliberately.");
            }

            Step = pitch;
            ButtonSize = size;

            double padCentreX = ((board.PlayfieldWidth / 2.0) + (frameWidth / 2.0)) / 2.0;
            double padCentreY = -(frameHeight / 2.0) + BottomInset + (1.5 * pitch);
            PadCentre = new BoardPoint(padCentreX, padCentreY);

            double pauseCentreY =
                (frameHeight / 2.0) - board.BandHeight - UnderBandInset - (size / 2.0);
            PauseCentre = new BoardPoint(-padCentreX, pauseCentreY);
        }

        /// <summary>Lattice pitch: a control's side plus the gap that separates two of them.</summary>
        public double Step { get; }

        /// <summary>Side of one control, in reference pixels — what gets drawn.</summary>
        public double ButtonSize { get; }

        /// <summary>Centre of the directional pad (the middle of the cross, which is not a button).</summary>
        public BoardPoint PadCentre { get; }

        /// <summary>Centre of the pause button.</summary>
        public BoardPoint PauseCentre { get; }

        /// <summary>Centre of a control, for whoever draws it.</summary>
        public BoardPoint ButtonCentre(TouchTarget target)
        {
            switch (target)
            {
                case TouchTarget.North:
                    return new BoardPoint(PadCentre.X, PadCentre.Y + Step);
                case TouchTarget.South:
                    return new BoardPoint(PadCentre.X, PadCentre.Y - Step);
                case TouchTarget.West:
                    return new BoardPoint(PadCentre.X - Step, PadCentre.Y);
                case TouchTarget.East:
                    return new BoardPoint(PadCentre.X + Step, PadCentre.Y);
                case TouchTarget.Pause:
                    return PauseCentre;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(target), target, "That target has no place on screen.");
            }
        }

        /// <summary>What a finger landing at this point asks for.</summary>
        /// <remarks>
        /// ⚠ The hit area of a control is its <b>whole lattice cell</b>, gap included, not the
        /// square that gets drawn: the gaps exist so the eye can tell the buttons apart, and a
        /// finger landing in one of them means the button next to it, not "nothing". The corners of
        /// the cross and its centre stay <see cref="TouchTarget.None"/> — a diagonal is not a
        /// direction this game has.
        /// </remarks>
        public TouchTarget HitTest(double x, double y)
        {
            if (Inside(x, y, PauseCentre, Step))
            {
                return TouchTarget.Pause;
            }

            double localX = x - PadCentre.X;
            double localY = y - PadCentre.Y;
            double half = 1.5 * Step;

            if (localX < -half || localX > half || localY < -half || localY > half)
            {
                return TouchTarget.None;
            }

            int column = Cell(localX);
            int row = Cell(localY);

            if (column == 0 && row == 1)
            {
                return TouchTarget.North;
            }

            if (column == 0 && row == -1)
            {
                return TouchTarget.South;
            }

            if (column == -1 && row == 0)
            {
                return TouchTarget.West;
            }

            if (column == 1 && row == 0)
            {
                return TouchTarget.East;
            }

            return TouchTarget.None;
        }

        /// <summary>The direction a pad target names, for the caller that feeds the input queue.</summary>
        public static bool TryDirection(TouchTarget target, out Direction direction)
        {
            switch (target)
            {
                case TouchTarget.North:
                    direction = Direction.North;
                    return true;
                case TouchTarget.South:
                    direction = Direction.South;
                    return true;
                case TouchTarget.West:
                    direction = Direction.West;
                    return true;
                case TouchTarget.East:
                    direction = Direction.East;
                    return true;
                default:
                    direction = Direction.East;
                    return false;
            }
        }

        private int Cell(double local)
        {
            int index = (int)Math.Floor((local / Step) + 1.5) - 1;
            return index < -1 ? -1 : (index > 1 ? 1 : index);
        }

        private static bool Inside(double x, double y, BoardPoint centre, double side)
        {
            double half = side / 2.0;
            return x >= centre.X - half && x <= centre.X + half
                && y >= centre.Y - half && y <= centre.Y + half;
        }
    }
}
