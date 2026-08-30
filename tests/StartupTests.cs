using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// The standing start (GDD §4.1, author's ruling of 2026-08-27).
/// </summary>
public class StartupTests
{
    /// <summary>
    /// §4.1 is explicit: "a player who presses West sees the rejection (§3) and nothing moves".
    /// That case teaches the reversal rule before any danger exists.
    /// </summary>
    [Fact]
    public void PressingWestOnAnEastFacingSnakeDoesNotStartTheGame()
    {
        Assert.Equal(
            StartDecision.RejectedReversal,
            Startup.Decide(Grid.InitialOrientation, Direction.West));
    }

    /// <summary>
    /// ⚠ The case that gets lost when the start is wired to the enqueue result: pressing East on a
    /// snake already facing east yields <c>RejectedDuplicate</c>, which has NO visual feedback
    /// (ART §5.3). A game that refused to start on that would sit frozen showing nothing — the
    /// player would conclude it is broken. §4.1 says "the first press that is not a reversal": a
    /// duplicate is one of those.
    /// </summary>
    [Fact]
    public void PressingTheDirectionAlreadyFollowedStillStartsTheGame()
    {
        Assert.Equal(
            StartDecision.Starts,
            Startup.Decide(Grid.InitialOrientation, Direction.East));
    }

    [Theory]
    [InlineData(Direction.North)]
    [InlineData(Direction.South)]
    public void ATurnStartsTheGame(Direction direction)
    {
        Assert.Equal(
            StartDecision.Starts,
            Startup.Decide(Grid.InitialOrientation, direction));
    }

    /// <summary>
    /// The rule knows nothing about "east" in particular: it follows the starting orientation,
    /// whatever it is. The day §4.3 changes the starting orientation, nothing here needs touching.
    /// </summary>
    [Fact]
    public void OnlyTheOppositeOfTheStartingOrientationIsRejected()
    {
        foreach (Direction pose in Directions.All())
        {
            foreach (Direction requested in Directions.All())
            {
                StartDecision expected = requested == Directions.Opposite(pose)
                    ? StartDecision.RejectedReversal
                    : StartDecision.Starts;

                Assert.Equal(expected, Startup.Decide(pose, requested));
            }
        }
    }
}
