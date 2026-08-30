using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Where the on-screen controls sit (GDD §3, touch — reopened on 2026-08-30).
/// </summary>
public class TouchPadTests
{
    private static Board DefaultBoard()
    {
        Grid grid = Grid.Default;
        return new Board(grid, Board.CellSizeFor(grid));
    }

    private static TouchPad DefaultPad()
    {
        return new TouchPad(DefaultBoard());
    }

    /// <summary>
    /// ⚠ <b>The test the whole class exists for.</b> A control drawn over the playfield does two
    /// things at once, both invisible in the editor: it hides cells the player dies against, and it
    /// eats the swipes meant for the game. Every control, gap included, stays strictly to the side
    /// of the playfield.
    /// </summary>
    [Theory]
    [InlineData(TouchTarget.North)]
    [InlineData(TouchTarget.South)]
    [InlineData(TouchTarget.East)]
    [InlineData(TouchTarget.West)]
    [InlineData(TouchTarget.Pause)]
    public void NoControlOverlapsThePlayfield(TouchTarget target)
    {
        Board board = DefaultBoard();
        TouchPad pad = new TouchPad(board);
        double playfieldEdge = board.PlayfieldWidth / 2.0;

        BoardPoint centre = pad.ButtonCentre(target);
        double half = pad.Step / 2.0;
        double nearest = Math.Abs(centre.X) - half;

        Assert.True(
            nearest >= playfieldEdge,
            $"{target} reaches to {nearest:F1} px, the playfield ends at {playfieldEdge:F1} px.");
    }

    /// <summary>Controls drawn off-screen are controls that do not exist.</summary>
    [Theory]
    [InlineData(TouchTarget.North)]
    [InlineData(TouchTarget.South)]
    [InlineData(TouchTarget.East)]
    [InlineData(TouchTarget.West)]
    [InlineData(TouchTarget.Pause)]
    public void EveryControlStaysInsideTheFrame(TouchTarget target)
    {
        TouchPad pad = DefaultPad();
        BoardPoint centre = pad.ButtonCentre(target);
        double half = pad.Step / 2.0;

        Assert.InRange(centre.X - half, -Board.DefaultFrameWidth / 2.0, Board.DefaultFrameWidth / 2.0);
        Assert.InRange(centre.X + half, -Board.DefaultFrameWidth / 2.0, Board.DefaultFrameWidth / 2.0);
        Assert.InRange(centre.Y - half, -Board.DefaultFrameHeight / 2.0, Board.DefaultFrameHeight / 2.0);
        Assert.InRange(centre.Y + half, -Board.DefaultFrameHeight / 2.0, Board.DefaultFrameHeight / 2.0);
    }

    /// <summary>
    /// The other half of the same guarantee, read from the finger's side: a touch anywhere on the
    /// playfield belongs to the swipe reader, never to a button.
    /// </summary>
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(-460.0, -350.0)]
    [InlineData(460.0, 290.0)]
    public void APointOnThePlayfieldHitsNoControl(double x, double y)
    {
        Assert.Equal(TouchTarget.None, DefaultPad().HitTest(x, y));
    }

    [Theory]
    [InlineData(TouchTarget.North)]
    [InlineData(TouchTarget.South)]
    [InlineData(TouchTarget.East)]
    [InlineData(TouchTarget.West)]
    [InlineData(TouchTarget.Pause)]
    public void EachControlIsHitAtItsOwnCentre(TouchTarget target)
    {
        TouchPad pad = DefaultPad();
        BoardPoint centre = pad.ButtonCentre(target);
        Assert.Equal(target, pad.HitTest(centre.X, centre.Y));
    }

    /// <summary>
    /// This game has four directions. The middle of the cross and its corners are deliberately
    /// nothing: a diagonal read as one of its two axes would turn the snake somewhere the player
    /// did not point.
    /// </summary>
    [Fact]
    public void TheCentreAndTheCornersOfTheCrossAreNothing()
    {
        TouchPad pad = DefaultPad();
        BoardPoint c = pad.PadCentre;

        Assert.Equal(TouchTarget.None, pad.HitTest(c.X, c.Y));
        Assert.Equal(TouchTarget.None, pad.HitTest(c.X + pad.Step, c.Y + pad.Step));
        Assert.Equal(TouchTarget.None, pad.HitTest(c.X - pad.Step, c.Y + pad.Step));
        Assert.Equal(TouchTarget.None, pad.HitTest(c.X + pad.Step, c.Y - pad.Step));
        Assert.Equal(TouchTarget.None, pad.HitTest(c.X - pad.Step, c.Y - pad.Step));
    }

    /// <summary>
    /// ⚠ The gaps are drawn, not felt. A thumb landing between two buttons means the button it is
    /// nearest to — treating the gap as "nothing" would give a pad that ignores presses for no
    /// reason the player can see.
    /// </summary>
    [Fact]
    public void TheGapBetweenTwoButtonsStillBelongsToOne()
    {
        TouchPad pad = DefaultPad();
        BoardPoint north = pad.ButtonCentre(TouchTarget.North);

        // Just past the drawn edge of the North button, still inside its lattice cell.
        double justOutside = north.Y + (pad.ButtonSize / 2.0) + 0.5;
        Assert.Equal(TouchTarget.North, pad.HitTest(north.X, justOutside));
    }

    /// <summary>
    /// A point beyond the cross entirely is nothing — the margin is not one big button.
    /// </summary>
    [Fact]
    public void AboveTheCrossIsNothing()
    {
        TouchPad pad = DefaultPad();
        BoardPoint c = pad.PadCentre;
        Assert.Equal(TouchTarget.None, pad.HitTest(c.X, c.Y + (2.0 * pad.Step)));
    }

    /// <summary>
    /// The default 21 × 15 grid leaves 178 px of margin, so the pad keeps its full size. This test
    /// pins the fact the layout relies on: the controls cost the playfield nothing.
    /// </summary>
    [Fact]
    public void TheDefaultGridLeavesRoomForFullSizeControls()
    {
        Assert.Equal(TouchPad.DefaultButtonSize, DefaultPad().ButtonSize);
    }

    /// <summary>
    /// ⚠ The margin is a leftover of rounding, not a promise. On a grid wide enough to eat it, the
    /// pad must refuse rather than draw itself over the playfield — a failure that would raise
    /// nothing and be discovered by a player who cannot steer.
    /// </summary>
    [Fact]
    public void AGridThatEatsTheMarginIsRefused()
    {
        Grid wide = new Grid(31, 15);
        Board board = new Board(wide, Board.CellSizeFor(wide));

        Assert.Throws<ArgumentOutOfRangeException>(() => new TouchPad(board));
    }

    /// <summary>
    /// Between "full size" and "refused" the pad shrinks to the margin it is given, rather than
    /// keeping a size that would overlap.
    /// </summary>
    [Fact]
    public void ATighterMarginShrinksTheControls()
    {
        Grid tighter = new Grid(25, 17);
        Board board = new Board(tighter, Board.CellSizeFor(tighter));
        TouchPad pad = new TouchPad(board);

        Assert.True(pad.ButtonSize < TouchPad.DefaultButtonSize);
        Assert.True(pad.ButtonSize >= TouchPad.MinimumButtonSize);

        double nearest = Math.Abs(pad.ButtonCentre(TouchTarget.West).X) - (pad.Step / 2.0);
        Assert.True(nearest >= board.PlayfieldWidth / 2.0);
    }

    /// <summary>Only the four directional targets name a direction; the pause names none.</summary>
    [Fact]
    public void OnlyTheDirectionalTargetsNameADirection()
    {
        Assert.True(TouchPad.TryDirection(TouchTarget.North, out Direction north));
        Assert.Equal(Direction.North, north);

        Assert.False(TouchPad.TryDirection(TouchTarget.Pause, out _));
        Assert.False(TouchPad.TryDirection(TouchTarget.None, out _));
    }
}
