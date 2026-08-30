using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Reading a finger's travel as a direction (GDD §3, touch — reopened on 2026-08-30).
/// </summary>
public class SwipeTests
{
    /// <summary>
    /// ⚠ The test that protects the restart. A mobile player has one gesture for "play again": a
    /// tap. A finger landing always jitters by a pixel or two, and if that counted as a swipe the
    /// game would restart AND immediately turn the snake — the player would lose a game they had
    /// not started playing.
    /// </summary>
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(3.0, -2.0)]
    [InlineData(27.9, 27.9)]
    public void TravelUnderTheThresholdIsNotATurn(double dx, double dy)
    {
        Assert.False(Swipe.Read(dx, dy).Recognised);
        Assert.True(Swipe.IsTap(dx, dy));
    }

    /// <summary>
    /// The sign convention, which no other test can catch: Y grows UPWARDS, as everywhere in
    /// <c>Rules/</c>. Getting it backwards gives a game where every swipe turns the snake the
    /// opposite way — it plays, it raises nothing, and it is unusable.
    /// </summary>
    [Fact]
    public void SwipingUpIsNorth()
    {
        Assert.Equal(Direction.North, Swipe.Read(0.0, 40.0).Direction);
    }

    [Theory]
    [InlineData(0.0, -40.0, Direction.South)]
    [InlineData(-40.0, 0.0, Direction.West)]
    [InlineData(40.0, 0.0, Direction.East)]
    public void EachAxisReadsItsDirection(double dx, double dy, Direction expected)
    {
        SwipeReading reading = Swipe.Read(dx, dy);
        Assert.True(reading.Recognised);
        Assert.Equal(expected, reading.Direction);
    }

    /// <summary>
    /// A real swipe is never axis-perfect. What decides is the dominant axis, so a gesture drawn
    /// mostly rightwards while drifting up is East — not "East and North", and not a refusal.
    /// </summary>
    [Fact]
    public void TheDominantAxisDecides()
    {
        Assert.Equal(Direction.East, Swipe.Read(50.0, 30.0).Direction);
        Assert.Equal(Direction.North, Swipe.Read(30.0, 50.0).Direction);
    }

    /// <summary>
    /// A tie has to land somewhere. Leaving it unrecognised would read as "the game missed my
    /// swipe", which the whole of §4.2 is written to avoid.
    /// </summary>
    [Fact]
    public void AnExactTieGoesToTheHorizontal()
    {
        Assert.Equal(Direction.East, Swipe.Read(40.0, 40.0).Direction);
        Assert.Equal(Direction.West, Swipe.Read(-40.0, 40.0).Direction);
    }

    /// <summary>
    /// The threshold is the travel at which the turn fires, not one the gesture has to beat.
    /// </summary>
    [Fact]
    public void ExactlyTheThresholdCounts()
    {
        Assert.True(Swipe.Read(Swipe.DefaultThreshold, 0.0).Recognised);
    }

    /// <summary>
    /// A gesture is a turn or a tap, never both and never neither: the engine branches on exactly
    /// these two, and a gap between them would be a press that does nothing at all.
    /// </summary>
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(28.0, 0.0)]
    [InlineData(-100.0, 5.0)]
    [InlineData(12.0, -60.0)]
    public void TapAndTurnPartitionEveryGesture(double dx, double dy)
    {
        Assert.NotEqual(Swipe.Read(dx, dy).Recognised, Swipe.IsTap(dx, dy));
    }

    /// <summary>
    /// The threshold is a parameter because the caller converts real pixels into reference pixels:
    /// a caller that had to hard-code 28 would be wrong on every panel but one.
    /// </summary>
    [Fact]
    public void TheThresholdCanBeTightened()
    {
        Assert.False(Swipe.Read(10.0, 0.0).Recognised);
        Assert.True(Swipe.Read(10.0, 0.0, threshold: 8.0).Recognised);
    }
}
