using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>What the design demands of the playfield layout (GDD §4.3, ART §5.4).</summary>
public class BoardTests
{
    /// <summary>
    /// §4.3 states the number itself: "a cell is min(1280/21, 660/15) = 44 px". If this test fails,
    /// either the formula or the reference frame has moved — in both cases every readability value
    /// in the GDD needs re-reading.
    /// </summary>
    [Fact]
    public void TheGddCellIsFortyFourPixels()
    {
        Assert.Equal(44, Board.CellSizeFor(Grid.Default));
    }

    /// <summary>
    /// §4.3 concludes: "the grid takes 924 px of width and leaves ~178 px of margin on each side —
    /// enough to put score and best score outside the playfield". Those margins are the reason the
    /// 32 × 18 grid was ruled out (§7): losing them would reopen that debate without saying so.
    /// </summary>
    [Fact]
    public void ThePlayfieldLeavesSideMarginsForTheScore()
    {
        Board board = new Board(Grid.Default, Board.CellSizeFor(Grid.Default));

        Assert.Equal(924, board.PlayfieldWidth);
        Assert.Equal(660, board.PlayfieldHeight);

        int marginPerSide = (Board.DefaultFrameWidth - board.PlayfieldWidth) / 2;
        Assert.Equal(178, marginPerSide);
    }

    /// <summary>
    /// The playfield must sit BELOW the HUD band, never "below" in the sense of covered. A HUD
    /// eating the top row raises nothing: it only kills the player against a wall they never saw.
    /// </summary>
    [Fact]
    public void ThePlayfieldNeverSlidesUnderTheHudBand()
    {
        Board board = new Board(Grid.Default, Board.CellSizeFor(Grid.Default));

        double topOfPlayfield = board.PlayfieldVerticalOffset + (board.PlayfieldHeight / 2.0);
        double bottomOfBand = (Board.DefaultFrameHeight / 2.0) - Board.DefaultBandHeight;

        Assert.True(topOfPlayfield <= bottomOfBand,
            $"The top of the playfield ({topOfPlayfield}) reaches under the band ({bottomOfBand}).");
    }

    /// <summary>
    /// The centre cell is the anchor of the starting pose (§4.3): it must land on the frame's
    /// vertical axis, failing which "in the centre" (§2) becomes false on screen while staying true
    /// in logic.
    /// </summary>
    [Fact]
    public void TheCentreCellLandsOnTheFrameAxis()
    {
        Board board = new Board(Grid.Default, Board.CellSizeFor(Grid.Default));

        BoardPoint centre = board.CellCentre(Grid.Default.Centre);

        Assert.Equal(0.0, centre.X, 9);
        Assert.Equal(board.PlayfieldVerticalOffset, centre.Y, 9);
    }

    /// <summary>
    /// North = increasing Y (the <c>Direction</c> convention): moving north must go UP on screen. An
    /// inversion here gives a game that answers "backwards" without raising the slightest error.
    /// </summary>
    [Fact]
    public void MovingNorthGoesUpOnScreen()
    {
        Board board = new Board(Grid.Default, Board.CellSizeFor(Grid.Default));
        Cell start = Grid.Default.Centre;

        BoardPoint before = board.CellCentre(start);
        BoardPoint after = board.CellCentre(Directions.Advance(start, Direction.North));

        Assert.True(after.Y > before.Y);
        Assert.Equal(board.CellSize, after.Y - before.Y, 9);
    }

    /// <summary>Two neighbouring cells are exactly one cell apart, on both axes.</summary>
    [Theory]
    [InlineData(Direction.North)]
    [InlineData(Direction.South)]
    [InlineData(Direction.East)]
    [InlineData(Direction.West)]
    public void TwoNeighbouringCellsAreOneCellApart(Direction direction)
    {
        Board board = new Board(Grid.Default, Board.CellSizeFor(Grid.Default));
        Cell start = Grid.Default.Centre;

        BoardPoint before = board.CellCentre(start);
        BoardPoint after = board.CellCentre(Directions.Advance(start, direction));

        double distance = Math.Abs(after.X - before.X) + Math.Abs(after.Y - before.Y);
        Assert.Equal(board.CellSize, distance, 9);
    }

    /// <summary>
    /// ART §5.4: the pictogram is "anchored to the edge of the head cell […] so it never covers the
    /// cell itself", and "never spills onto the neighbouring cell and reads as an obstacle". The
    /// test checks those two bounds, not the formula that produces them.
    /// </summary>
    [Theory]
    [InlineData(Direction.North)]
    [InlineData(Direction.South)]
    [InlineData(Direction.East)]
    [InlineData(Direction.West)]
    public void TheRejectionPictogramCoversNeitherTheHeadNorTheNeighbourCentre(Direction rejected)
    {
        Board board = new Board(Grid.Default, Board.CellSizeFor(Grid.Default));
        Cell head = Grid.Default.Centre;

        BoardPoint headCentre = board.CellCentre(head);
        BoardPoint anchor = board.RejectionAnchor(head, rejected);
        BoardPoint neighbourCentre = board.CellCentre(Directions.Advance(head, rejected));

        // Distance along the axis of the rejected direction: the other two components are zero.
        double anchorDistance = Distance(headCentre, anchor);
        double neighbourDistance = Distance(headCentre, neighbourCentre);
        double halfPictogram = board.MaximumPictogramSize / 2.0;

        double nearEdge = anchorDistance - halfPictogram;
        double farEdge = anchorDistance + halfPictogram;

        Assert.True(nearEdge >= board.CellSize / 2.0,
            $"The pictogram bites into the head cell (near edge at {nearEdge}).");
        Assert.True(farEdge <= neighbourDistance,
            $"The pictogram goes past the neighbour's centre (far edge at {farEdge}).");
    }

    /// <summary>The pictogram starts on the right side: the one of the rejected direction.</summary>
    [Fact]
    public void ThePictogramSitsOnTheSideOfTheRejectedDirection()
    {
        Board board = new Board(Grid.Default, Board.CellSizeFor(Grid.Default));
        Cell head = Grid.Default.Centre;
        BoardPoint centre = board.CellCentre(head);

        Assert.True(board.RejectionAnchor(head, Direction.West).X < centre.X);
        Assert.True(board.RejectionAnchor(head, Direction.East).X > centre.X);
        Assert.True(board.RejectionAnchor(head, Direction.North).Y > centre.Y);
        Assert.True(board.RejectionAnchor(head, Direction.South).Y < centre.Y);
    }

    /// <summary>
    /// Cell size follows the grid: §4.3 makes dimensions tunable without recompiling, so the layout
    /// must recompute rather than stay frozen at 44 px.
    /// </summary>
    [Fact]
    public void ALargerGridGivesSmallerCells()
    {
        int small = Board.CellSizeFor(Grid.Default);
        int large = Board.CellSizeFor(new Grid(31, 21));

        Assert.True(large < small);
    }

    /// <summary>
    /// The playfield must stay INSIDE the frame, whatever the grid: a wall off screen cannot be
    /// seen, and §2 requires every death to be attributable to a turn.
    /// </summary>
    [Theory]
    [InlineData(5, 3)]
    [InlineData(21, 15)]
    [InlineData(31, 21)]
    [InlineData(51, 35)]
    public void ThePlayfieldAlwaysFitsInsideTheFrame(int width, int height)
    {
        Grid grid = new Grid(width, height);
        Board board = new Board(grid, Board.CellSizeFor(grid));

        Assert.True(board.PlayfieldWidth <= Board.DefaultFrameWidth);
        Assert.True(board.PlayfieldHeight <= Board.DefaultFrameHeight - Board.DefaultBandHeight);
    }

    private static double Distance(BoardPoint a, BoardPoint b)
    {
        return Math.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));
    }
}
