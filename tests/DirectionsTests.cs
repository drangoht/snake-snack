using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// The pure operations on directions — here the sign of a turn (docs/art/juicy.md §9).
/// </summary>
/// <remarks>
/// A tilt on the wrong side raises nothing and does not show either: at 8° over 125 ms, nobody will
/// say "it leans the wrong way", the game will merely feel slightly odd. Hence these tests, which
/// pin the sign down once and for all.
/// </remarks>
public class DirectionsTests
{
    /// <summary>
    /// The sign follows Unity's convention: increasing Z turns counter-clockwise, so a left turn
    /// returns +1.
    /// </summary>
    [Theory]
    [InlineData(Direction.North, Direction.West)]
    [InlineData(Direction.West, Direction.South)]
    [InlineData(Direction.South, Direction.East)]
    [InlineData(Direction.East, Direction.North)]
    public void ALeftTurnReturnsPlusOne(Direction before, Direction after)
    {
        Assert.Equal(1, Directions.TurnSign(before, after));
    }

    [Theory]
    [InlineData(Direction.North, Direction.East)]
    [InlineData(Direction.East, Direction.South)]
    [InlineData(Direction.South, Direction.West)]
    [InlineData(Direction.West, Direction.North)]
    public void ARightTurnReturnsMinusOne(Direction before, Direction after)
    {
        Assert.Equal(-1, Directions.TurnSign(before, after));
    }

    [Fact]
    public void GoingStraightOnIsNotATurn()
    {
        foreach (Direction direction in Directions.All())
        {
            Assert.Equal(0, Directions.TurnSign(direction, direction));
        }
    }

    /// <summary>
    /// ⚠ A reversal does not happen in play (the queue rejects it at the tick, GDD §4.2) — but if it
    /// did, leaning one way rather than the other would be an invention: both quarter turns are
    /// equally wrong. Zero means "nothing to show".
    /// </summary>
    [Fact]
    public void AReversalLeansNeitherWay()
    {
        foreach (Direction direction in Directions.All())
        {
            Assert.Equal(0, Directions.TurnSign(direction, Directions.Opposite(direction)));
        }
    }

    /// <summary>The reverse turn leans the other way, exactly — never an asymmetry.</summary>
    [Fact]
    public void TheReverseTurnReturnsTheOppositeSign()
    {
        foreach (Direction before in Directions.All())
        {
            foreach (Direction after in Directions.All())
            {
                Assert.Equal(
                    -Directions.TurnSign(before, after),
                    Directions.TurnSign(after, before));
            }
        }
    }
}
