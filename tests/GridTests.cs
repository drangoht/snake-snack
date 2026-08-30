using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>What the design demands of the playfield (GDD §4.3, and the lethal wall of §2).</summary>
public class GridTests
{
    [Fact]
    public void TheDefaultGridIsTheOneFromTheGdd()
    {
        Grid grid = Grid.Default;

        Assert.Equal(21, grid.Width);
        Assert.Equal(15, grid.Height);
        Assert.Equal(315, grid.CellCount);
    }

    /// <summary>
    /// The centre cell must be EXACT: that is the condition for the snake to appear "in the centre"
    /// (§2) without a half-cell offset. The test does not check the formula but the property: as
    /// many columns to the left as to the right, as many rows below as above.
    /// </summary>
    [Fact]
    public void TheCentreCellLeavesAsManyCellsOnEachSide()
    {
        foreach (Grid grid in new[] { Grid.Default, new Grid(5, 3), new Grid(31, 21) })
        {
            Cell centre = grid.Centre;
            Assert.Equal(centre.X, grid.Width - 1 - centre.X);
            Assert.Equal(centre.Y, grid.Height - 1 - centre.Y);
        }
    }

    [Fact]
    public void TheCentreCellOfTheDefaultGridIsTenSeven()
    {
        Assert.Equal(new Cell(10, 7), Grid.Default.Centre);
    }

    /// <summary>
    /// An even dimension raises nothing at runtime: it produces a pose offset by half a cell that
    /// nobody sees before a screenshot. Hence failing at construction. It is also what ruled out the
    /// 32 × 18 grid (§7).
    /// </summary>
    [Fact]
    public void AnEvenDimensionIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(20, 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(21, 14));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(32, 18));
    }

    /// <summary>A grid too narrow could not carry the starting pose.</summary>
    [Fact]
    public void AGridTooSmallForTheStartingPoseIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(3, 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(21, 1));
    }

    /// <summary>Dimensions are tunable without recompiling: any valid odd grid holds.</summary>
    [Fact]
    public void DimensionsAreTunable()
    {
        Grid grid = new Grid(31, 21);

        Assert.Equal(651, grid.CellCount);
        Assert.Equal(new Cell(15, 10), grid.Centre);
    }

    /// <summary>
    /// The exact pose of §4.3: head (10, 7), body (9, 7) and (8, 7), length 3, facing east.
    /// </summary>
    [Fact]
    public void TheStartingPoseIsTheOneFromTheGdd()
    {
        StartPose pose = Grid.Default.StartingPose();

        Assert.Equal(3, pose.Length);
        Assert.Equal(Direction.East, pose.Orientation);
        Assert.Equal(new Cell(10, 7), pose.Head);
        Assert.Equal(new Cell(9, 7), pose.Segments[1]);
        Assert.Equal(new Cell(8, 7), pose.Segments[2]);
    }

    /// <summary>
    /// The body extends BEHIND the head. Laid in front, the snake would eat itself on the first
    /// tick: a game that never starts, with no error at all to explain it.
    /// </summary>
    [Fact]
    public void TheBodyIsBehindTheHeadRelativeToTheOrientation()
    {
        StartPose pose = Grid.Default.StartingPose();
        Cell aheadOfTheHead = Directions.Advance(pose.Head, pose.Orientation);

        foreach (Cell segment in pose.Segments)
        {
            Assert.NotEqual(aheadOfTheHead, segment);
        }
    }

    /// <summary>The starting pose fits entirely inside the playfield, whatever the grid.</summary>
    [Fact]
    public void TheStartingPoseFitsInsideTheGrid()
    {
        foreach (Grid grid in new[] { Grid.Default, new Grid(5, 3), new Grid(31, 21) })
        {
            StartPose pose = grid.StartingPose();
            foreach (Cell segment in pose.Segments)
            {
                Assert.True(grid.Contains(segment), $"Segment {segment} outside grid {grid.Width}x{grid.Height}.");
            }
        }
    }

    /// <summary>
    /// THE lethal wall of §2: one more step from each of the four edges leaves the grid. Edges kill,
    /// they do not teleport (§7).
    /// </summary>
    [Fact]
    public void OneStepPastEachEdgeIsOutsideTheGrid()
    {
        Grid grid = Grid.Default;

        Cell onTheEastEdge = new Cell(grid.Width - 1, grid.Centre.Y);
        Cell onTheWestEdge = new Cell(0, grid.Centre.Y);
        Cell onTheNorthEdge = new Cell(grid.Centre.X, grid.Height - 1);
        Cell onTheSouthEdge = new Cell(grid.Centre.X, 0);

        Assert.True(grid.Contains(onTheEastEdge));
        Assert.True(grid.Contains(onTheWestEdge));
        Assert.True(grid.Contains(onTheNorthEdge));
        Assert.True(grid.Contains(onTheSouthEdge));

        Assert.True(grid.IsOutside(Directions.Advance(onTheEastEdge, Direction.East)));
        Assert.True(grid.IsOutside(Directions.Advance(onTheWestEdge, Direction.West)));
        Assert.True(grid.IsOutside(Directions.Advance(onTheNorthEdge, Direction.North)));
        Assert.True(grid.IsOutside(Directions.Advance(onTheSouthEdge, Direction.South)));
    }

    /// <summary>
    /// The four corners belong to the grid, and the four diagonal cells just outside do not: a bound
    /// test written with one &lt;= too many passes every edge test and fails here.
    /// </summary>
    [Fact]
    public void TheFourCornersAreInsideButNotTheirDiagonalNeighbours()
    {
        Grid grid = Grid.Default;
        int xMax = grid.Width - 1;
        int yMax = grid.Height - 1;

        Assert.True(grid.Contains(new Cell(0, 0)));
        Assert.True(grid.Contains(new Cell(xMax, 0)));
        Assert.True(grid.Contains(new Cell(0, yMax)));
        Assert.True(grid.Contains(new Cell(xMax, yMax)));

        Assert.True(grid.IsOutside(new Cell(-1, -1)));
        Assert.True(grid.IsOutside(new Cell(xMax + 1, -1)));
        Assert.True(grid.IsOutside(new Cell(-1, yMax + 1)));
        Assert.True(grid.IsOutside(new Cell(xMax + 1, yMax + 1)));
    }

    /// <summary>
    /// No modulo anywhere: leaving the grid stays leaving the grid, even far out. The day somebody
    /// "fixes" a negative index with a modulo, they silently reintroduce the wrapping edges ruled
    /// out in §7 and death stops being attributable (§2).
    /// </summary>
    [Fact]
    public void LeavingTheGridIsNeverBroughtBackByAModulo()
    {
        Grid grid = Grid.Default;

        Assert.True(grid.IsOutside(new Cell(grid.Width, 7)));
        Assert.True(grid.IsOutside(new Cell(-1, 7)));
        Assert.True(grid.IsOutside(new Cell(10, grid.Height)));
        Assert.True(grid.IsOutside(new Cell(10, -1)));
    }

    /// <summary>
    /// The snake's body is tested against the head on every tick: two cells with the same
    /// coordinates must be equal and land in the same hash bucket, otherwise the collision with
    /// oneself — the game's only opponent (§1) — never fires.
    /// </summary>
    [Fact]
    public void TwoCellsWithTheSameCoordinatesAreTheSameCell()
    {
        Assert.Equal(new Cell(4, 9), new Cell(4, 9));
        Assert.True(new Cell(4, 9) == new Cell(4, 9));
        Assert.True(new Cell(4, 9) != new Cell(9, 4));
        Assert.Equal(new Cell(4, 9).GetHashCode(), new Cell(4, 9).GetHashCode());
    }
}
