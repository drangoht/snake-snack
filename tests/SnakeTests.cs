using System.Collections.Generic;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>What the design demands of the snake's body (GDD §2: the wall and the body kill).</summary>
public class SnakeTests
{
    private static Snake StartingPose()
    {
        return new Snake(Grid.Default.StartingPose().Segments);
    }

    [Fact]
    public void TheStartingSnakeIsTheOneFromTheGdd()
    {
        Snake snake = StartingPose();

        Assert.Equal(3, snake.Length);
        Assert.Equal(new Cell(10, 7), snake.Segments[0]);
        Assert.Equal(new Cell(9, 7), snake.Segments[1]);
        Assert.Equal(new Cell(8, 7), snake.Segments[2]);
    }

    [Fact]
    public void AdvancingMovesTheHeadAndDragsTheBodyBehindIt()
    {
        Snake snake = StartingPose();

        MoveResult result = snake.Advance(Direction.North, Grid.Default);

        Assert.Equal(MoveResult.Moved, result);
        Assert.Equal(new Cell(10, 8), snake.Segments[0]);
        Assert.Equal(new Cell(10, 7), snake.Segments[1]);
        Assert.Equal(new Cell(9, 7), snake.Segments[2]);
        Assert.Equal(3, snake.Length);
    }

    /// <summary>
    /// All four walls kill (§2, "edges kill, they do not teleport"). The test attacks all four
    /// edges: a modulo slipped in somewhere "to avoid a negative index" would reintroduce the
    /// wrapping edges ruled out in §7, and only one of the four edges would show it.
    /// </summary>
    [Theory]
    [InlineData(Direction.East)]
    [InlineData(Direction.West)]
    [InlineData(Direction.North)]
    [InlineData(Direction.South)]
    public void LeavingTheGridKills(Direction direction)
    {
        Grid grid = Grid.Default;
        Cell edge = direction switch
        {
            Direction.East => new Cell(grid.Width - 1, 7),
            Direction.West => new Cell(0, 7),
            Direction.North => new Cell(10, grid.Height - 1),
            _ => new Cell(10, 0)
        };

        Snake snake = new Snake(new[] { edge });

        Assert.Equal(MoveResult.HitWall, snake.Advance(direction, grid));
    }

    /// <summary>
    /// ⚠ A lethal move must NOT move the snake: the renderer would draw a head outside the playfield
    /// on the frame of death, and the player would see the snake go through the wall — which §2
    /// forbids letting anyone believe.
    /// </summary>
    [Fact]
    public void DeathDoesNotMoveTheSnake()
    {
        Grid grid = Grid.Default;
        Snake snake = new Snake(new[] { new Cell(20, 7), new Cell(19, 7), new Cell(18, 7) });

        snake.Advance(Direction.East, grid);

        Assert.Equal(new Cell(20, 7), snake.Segments[0]);
        Assert.Equal(new Cell(19, 7), snake.Segments[1]);
        Assert.Equal(new Cell(18, 7), snake.Segments[2]);
    }

    /// <summary>
    /// The body kills (§1: "until its own body leaves no way through"). From the apple onwards, this
    /// case is the normal end of a game: the snake itself is the opponent.
    /// </summary>
    [Fact]
    public void BitingYourselfKills()
    {
        // Coiled snake: the head at (2,2) looks east, and (3,2) is a body segment.
        Snake snake = new Snake(new[]
        {
            new Cell(2, 2), new Cell(2, 3), new Cell(3, 3), new Cell(3, 2), new Cell(3, 1)
        });

        Assert.Equal(MoveResult.BitSelf, snake.Advance(Direction.East, Grid.Default));
    }

    /// <summary>
    /// ⚠ The tail's cell frees up on the SAME tick: entering it is not a bite. Counting the tail
    /// among the obstacles produces a death on a cell the player watched empty out — inexplicable,
    /// therefore not attributable to a turn (§2).
    /// </summary>
    [Fact]
    public void EnteringTheCellTheTailFreesIsNotABite()
    {
        Snake snake = new Snake(new[]
        {
            new Cell(2, 1), new Cell(1, 1), new Cell(1, 0), new Cell(2, 0)
        });

        MoveResult result = snake.Advance(Direction.South, Grid.Default);

        Assert.Equal(MoveResult.Moved, result);
        Assert.Equal(new Cell(2, 0), snake.Head);
    }

    /// <summary>
    /// At three segments, biting yourself is geometrically impossible: until the player has eaten
    /// their first apple, only the wall can kill them. The test proves it instead of assuming it —
    /// it replays every non-reversal trajectory, far from the walls.
    /// </summary>
    [Fact]
    public void AtThreeSegmentsNoTrajectoryProducesABite()
    {
        Grid grid = Grid.Default;

        foreach (Direction first in Directions.All())
        {
            foreach (Direction second in Directions.All())
            {
                Snake snake = StartingPose();
                Direction current = Grid.InitialOrientation;
                List<Direction> plan = new List<Direction> { first, second, first, second };

                foreach (Direction wanted in plan)
                {
                    // A reversal never reaches the snake: the queue rejects it at the tick (§4.2).
                    Direction applied = Directions.IsReversal(current, wanted) ? current : wanted;
                    MoveResult result = snake.Advance(applied, grid);
                    current = applied;

                    Assert.NotEqual(MoveResult.BitSelf, result);

                    if (result != MoveResult.Moved)
                    {
                        break; // Left the grid: the rest of the plan no longer means anything.
                    }
                }
            }
        }
    }

    // ---- The apple (GDD §4.4) ----------------------------------------------------------------

    /// <summary>
    /// ⚠ <b>The snake grows from the HEAD, on the very tick it enters the apple</b> (§4.4) — not on
    /// the next tick, not by a segment appended behind the tail. It is the <b>tail that does not
    /// move</b> during that single tick: this test therefore checks that the last segment stayed
    /// exactly where it was. Adding the segment at the tail would give the same length and a shape
    /// wrong by one cell, invisible on reading.
    /// </summary>
    [Fact]
    public void EatingGrowsFromTheHeadAndLeavesTheTailInPlace()
    {
        Snake snake = StartingPose();
        Cell apple = new Cell(11, 7);

        bool ate;
        MoveResult result = snake.Advance(Direction.East, Grid.Default, apple, out ate);

        Assert.Equal(MoveResult.Moved, result);
        Assert.True(ate);
        Assert.Equal(4, snake.Length);
        Assert.Equal(new Cell(11, 7), snake.Segments[0]);
        Assert.Equal(new Cell(10, 7), snake.Segments[1]);
        Assert.Equal(new Cell(9, 7), snake.Segments[2]);
        Assert.Equal(new Cell(8, 7), snake.Segments[3]);
    }

    /// <summary>Going past the apple does not eat and does not grow.</summary>
    [Fact]
    public void GoingPastTheAppleDoesNotEat()
    {
        Snake snake = StartingPose();

        bool ate;
        snake.Advance(Direction.North, Grid.Default, new Cell(11, 7), out ate);

        Assert.False(ate);
        Assert.Equal(3, snake.Length);
    }

    /// <summary>Length is always <c>3 + score</c> (§4.5): five apples, eight segments.</summary>
    [Fact]
    public void LengthIsThreePlusTheNumberOfApples()
    {
        Grid grid = Grid.Default;
        Snake snake = StartingPose();

        for (int i = 1; i <= 5; i++)
        {
            // An apple placed right in front of the head, five times in a row.
            Cell apple = Directions.Advance(snake.Head, Direction.East);

            bool ate;
            snake.Advance(Direction.East, grid, apple, out ate);

            Assert.True(ate);
            Assert.Equal(Grid.InitialLength + i, snake.Length);
        }
    }

    /// <summary>
    /// ⚠ <b>The case that separates a correct implementation from one that "works".</b> Outside
    /// growth, the head may enter the cell the tail frees (test above). On the tick of an apple, the
    /// tail does not move: that same cell becomes an obstacle again, and entering it kills. Treating
    /// the tail the same way in both cases gives, take your pick, a snake that goes through itself
    /// or a death on a cell that looks free — and no error to say so.
    ///
    /// <para>The situation is artificial: §4.4 guarantees an apple never appears on the body. The
    /// rule is written without leaning on that guarantee, which is established elsewhere — and so is
    /// the test.</para>
    /// </summary>
    [Fact]
    public void EnteringTheTailCellWhileEatingIsABite()
    {
        Cell tail = new Cell(2, 0);
        Snake snake = new Snake(new[]
        {
            new Cell(2, 1), new Cell(1, 1), new Cell(1, 0), tail
        });

        bool ate;
        MoveResult result = snake.Advance(Direction.South, Grid.Default, tail, out ate);

        Assert.Equal(MoveResult.BitSelf, result);
        Assert.False(ate);
        Assert.Equal(4, snake.Length);
    }

    /// <summary>
    /// A lethal step does not eat. Without that, the score would rise by one on the tick of death,
    /// and the end screen would display a number the player never saw on screen.
    /// </summary>
    [Fact]
    public void DyingAgainstAWallDoesNotEat()
    {
        Grid grid = Grid.Default;
        Snake snake = new Snake(new[] { new Cell(20, 7), new Cell(19, 7) });

        bool ate;
        MoveResult result = snake.Advance(Direction.East, grid, new Cell(20, 7), out ate);

        Assert.Equal(MoveResult.HitWall, result);
        Assert.False(ate);
        Assert.Equal(2, snake.Length);
    }

    /// <summary>With no apple on the grid, the tick is the earlier one: nothing changes.</summary>
    [Fact]
    public void WithNoAppleTheTickIsAPlainMove()
    {
        Snake snake = StartingPose();

        bool ate;
        MoveResult result = snake.Advance(Direction.East, Grid.Default, null, out ate);

        Assert.Equal(MoveResult.Moved, result);
        Assert.False(ate);
        Assert.Equal(3, snake.Length);
    }
}
