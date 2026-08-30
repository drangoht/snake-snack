using System.Collections.Generic;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>Where the apple is allowed to land, and at what cost (GDD §4.4).</summary>
public class AppleTests
{
    /// <summary>Every cell of a grid except the listed ones, in walk order.</summary>
    private static List<Cell> AllBut(Grid grid, params Cell[] free)
    {
        List<Cell> occupied = new List<Cell>();

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                Cell candidate = new Cell(x, y);
                if (!System.Array.Exists(free, one => one == candidate))
                {
                    occupied.Add(candidate);
                }
            }
        }

        return occupied;
    }

    [Fact]
    public void TheStartingPoseLeaves312FreeCells()
    {
        Grid grid = Grid.Default;

        // 315 cells, 3 segments: the number from §4.4 ("312 apples on the default grid").
        Assert.Equal(312, Apple.FreeCellCount(grid, Grid.InitialLength));
        Assert.False(Apple.GridIsFull(grid, Grid.InitialLength));
    }

    /// <summary>
    /// ⚠ The walk order — <b>increasing X within increasing Y</b> — is part of the §4.4 contract:
    /// it is what gives the same game on all three targets for a given seed. Swapping it (Y within
    /// X) would break no uniformity test, and would break every bench pairing.
    /// </summary>
    [Fact]
    public void TheWalkGoesAlongXThenY()
    {
        Grid grid = Grid.Default;
        List<Cell> none = new List<Cell>();

        Assert.Equal(new Cell(0, 0), Apple.FreeCellAtRank(grid, none, 0));
        Assert.Equal(new Cell(1, 0), Apple.FreeCellAtRank(grid, none, 1));

        // A row is 21 cells: rank 21 is therefore the first cell of the next row.
        Assert.Equal(new Cell(20, 0), Apple.FreeCellAtRank(grid, none, 20));
        Assert.Equal(new Cell(0, 1), Apple.FreeCellAtRank(grid, none, 21));
    }

    /// <summary>The body is skipped, it does not simply shift the rank.</summary>
    [Fact]
    public void TheWalkSkipsTheBodyCells()
    {
        Grid grid = new Grid(5, 3);
        List<Cell> body = new List<Cell> { new Cell(1, 0), new Cell(2, 0) };

        Assert.Equal(new Cell(0, 0), Apple.FreeCellAtRank(grid, body, 0));
        Assert.Equal(new Cell(3, 0), Apple.FreeCellAtRank(grid, body, 1));
        Assert.Equal(new Cell(4, 0), Apple.FreeCellAtRank(grid, body, 2));
        Assert.Equal(new Cell(0, 1), Apple.FreeCellAtRank(grid, body, 3));
    }

    /// <summary>
    /// Every rank names a distinct free cell, and together they cover exactly the free cells. That
    /// is the property which makes the draw uniform <b>without</b> ever rejecting a cell.
    /// </summary>
    [Fact]
    public void RanksEnumerateExactlyTheFreeCells()
    {
        Grid grid = new Grid(5, 3);
        List<Cell> body = new List<Cell> { new Cell(2, 1), new Cell(1, 1), new Cell(0, 1) };

        int free = Apple.FreeCellCount(grid, body.Count);
        HashSet<Cell> seen = new HashSet<Cell>();

        for (int rank = 0; rank < free; rank++)
        {
            Cell drawn = Apple.FreeCellAtRank(grid, body, rank);

            Assert.DoesNotContain(drawn, body);
            Assert.True(grid.Contains(drawn));
            Assert.True(seen.Add(drawn), "Cell " + drawn + " is returned twice.");
        }

        Assert.Equal(free, seen.Count);
    }

    [Fact]
    public void AnOutOfBoundsRankIsRejected()
    {
        Grid grid = new Grid(5, 3);
        List<Cell> body = new List<Cell> { new Cell(0, 0) };

        Assert.Throws<System.ArgumentOutOfRangeException>(() => Apple.FreeCellAtRank(grid, body, -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => Apple.FreeCellAtRank(grid, body, 14));
    }

    /// <summary>
    /// ⚠ <b>The tick before the win.</b> A single free cell: the draw must return it, straight away.
    /// This is the position where "draw at random and start over if it is occupied" would take 15
    /// rounds on average here, and infinitely many in expectation on the real grid (§4.4).
    /// </summary>
    [Fact]
    public void TheLastFreeCellIsDrawnWithoutDetour()
    {
        Grid grid = new Grid(5, 3);
        Cell onlyFree = new Cell(3, 1);
        List<Cell> body = AllBut(grid, onlyFree);

        Assert.Equal(14, body.Count);
        Assert.Equal(1, Apple.FreeCellCount(grid, body.Count));
        Assert.Equal(onlyFree, Apple.FreeCellAtRank(grid, body, 0));
        Assert.Equal(onlyFree, Apple.Draw(grid, body, new RandomSource(1UL)));
    }

    /// <summary>
    /// ⚠ <b>The draw consumes exactly ONE number from the generator</b>, whatever the fill level of
    /// the grid. That is what makes a paired bench possible: two games with the same seed and the
    /// same sequence of presses stay aligned, apple after apple. An implementation that rejected
    /// occupied cells would consume a variable count and make the pairing diverge — without any
    /// other test here failing.
    /// </summary>
    [Fact]
    public void TheDrawConsumesASingleNumber()
    {
        Grid grid = Grid.Default;
        List<Cell> body = new List<Cell>(Grid.Default.StartingPose().Segments);

        RandomSource player = new RandomSource(123UL);
        RandomSource witness = new RandomSource(123UL);

        Cell drawn = Apple.Draw(grid, body, player);

        int rank = witness.NextInt(Apple.FreeCellCount(grid, body.Count));
        Assert.Equal(Apple.FreeCellAtRank(grid, body, rank), drawn);

        // Both generators are at the same point: the draw did not consume one more.
        Assert.Equal(witness.Next(), player.Next());
    }

    /// <summary>
    /// The apple never lands on the snake — and not "almost never": the guarantee comes from the
    /// walk, not from luck. A thousand draws on a very full grid show it.
    /// </summary>
    [Fact]
    public void TheAppleNeverLandsOnTheSnake()
    {
        Grid grid = new Grid(5, 3);
        List<Cell> body = new List<Cell>
        {
            new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(3, 0),
            new Cell(4, 0), new Cell(4, 1), new Cell(3, 1)
        };

        RandomSource random = new RandomSource(2026UL);

        for (int i = 0; i < 1000; i++)
        {
            Cell apple = Apple.Draw(grid, body, random);

            Assert.True(grid.Contains(apple));
            Assert.DoesNotContain(apple, body);
        }
    }

    /// <summary>
    /// Full grid = win (§4.4), to be handled <b>before</b> the draw. This state is out of human
    /// reach and must be written all the same: without it, the draw runs on an empty interval.
    /// </summary>
    [Fact]
    public void AFullGridIsDetectedAndTheDrawRefusesIt()
    {
        Grid grid = new Grid(5, 3);
        List<Cell> everywhere = AllBut(grid);

        Assert.Equal(15, everywhere.Count);
        Assert.True(Apple.GridIsFull(grid, everywhere.Count));
        Assert.Equal(0, Apple.FreeCellCount(grid, everywhere.Count));

        // Thrown, never a cell returned "somewhere": the caller must have seen the win.
        Assert.Throws<System.InvalidOperationException>(() => Apple.Draw(grid, everywhere, new RandomSource(1UL)));
    }

    [Fact]
    public void ASnakeLargerThanTheGridIsRejected()
    {
        Grid grid = new Grid(5, 3);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => Apple.FreeCellCount(grid, 16));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => Apple.FreeCellCount(grid, -1));
    }
}
