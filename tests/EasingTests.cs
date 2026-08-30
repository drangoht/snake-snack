using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// The curves of the juice (docs/art/juicy.md §2).
/// </summary>
/// <remarks>
/// What is checked here does not show to the eye over 150 ms: that an envelope comes back exactly to
/// its resting value, that no factor leaves its range on a long frame. Those are precisely the
/// defects that leave a permanent trace without anyone tying them back to an animation.
/// </remarks>
public class EasingTests
{
    private const double Tolerance = 1e-9;

    // --- Progress --------------------------------------------------------------------

    [Fact]
    public void ProgressGoesFromZeroToOneOverTheDuration()
    {
        Assert.Equal(0.0, Easing.Progress(10.0, 0.2, 10.0), 9);
        Assert.Equal(0.5, Easing.Progress(10.0, 0.2, 10.1), 9);
        Assert.Equal(1.0, Easing.Progress(10.0, 0.2, 10.2), 9);
    }

    /// <summary>
    /// ⚠ The case that would throw a segment past its cell: a long frame — the first one after a
    /// WebGL load eats several hundred ms — gives an elapsed time far greater than the tick
    /// duration.
    /// </summary>
    [Fact]
    public void AVeryLongFrameDoesNotPushPastOne()
    {
        Assert.Equal(1.0, Easing.Progress(10.0, 0.125, 12.0), 9);
    }

    /// <summary>A "now" earlier than the start (clock re-read after a pause) returns no negative.</summary>
    [Fact]
    public void ATimeEarlierThanTheStartReturnsZero()
    {
        Assert.Equal(0.0, Easing.Progress(10.0, 0.125, 9.5), 9);
    }

    [Fact]
    public void AZeroOrNegativeDurationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.Progress(0.0, 0.0, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.Progress(0.0, -0.1, 1.0));
    }

    // --- Pulse -----------------------------------------------------------------------

    [Fact]
    public void ThePulseStartsAtZeroPeaksAtOneAndFallsBackToZero()
    {
        Assert.Equal(0.0, Easing.Pulse(0.0), 9);
        Assert.Equal(1.0, Easing.Pulse(0.5), 9);
        Assert.Equal(0.0, Easing.Pulse(1.0), 9);
    }

    [Fact]
    public void ThePulseStaysInRangeAllTheWayThrough()
    {
        for (int i = 0; i <= 100; i++)
        {
            double value = Easing.Pulse(i / 100.0);
            Assert.InRange(value, 0.0, 1.0);
        }
    }

    /// <summary>Symmetric: a round trip, not a rise followed by a sudden drop.</summary>
    [Fact]
    public void ThePulseIsSymmetricAroundItsPeak()
    {
        for (int i = 0; i <= 50; i++)
        {
            double t = i / 100.0;
            Assert.Equal(Easing.Pulse(t), Easing.Pulse(1.0 - t), 9);
        }
    }

    // --- PopIn -----------------------------------------------------------------------

    /// <summary>
    /// ⚠ The test that counts: a segment whose final scale is not exactly 1 stays permanently bigger
    /// than its neighbours, long after the animation has ended.
    /// </summary>
    [Fact]
    public void ThePopEndsExactlyAtOne()
    {
        Assert.Equal(1.0, Easing.PopIn(1.0, 0.12), 9);
        Assert.Equal(1.0, Easing.PopIn(1.5, 0.12), 9);
    }

    [Fact]
    public void ThePopStartsAtZero()
    {
        Assert.Equal(0.0, Easing.PopIn(0.0, 0.12), 9);
        Assert.Equal(0.0, Easing.PopIn(-0.3, 0.12), 9);
    }

    [Fact]
    public void ThePopOvershootsOneBeforeComingBack()
    {
        double maximum = 0.0;
        for (int i = 0; i <= 100; i++)
        {
            maximum = Math.Max(maximum, Easing.PopIn(i / 100.0, 0.12));
        }

        Assert.True(maximum > 1.0, "Without overshoot a pop is just a fade: it does not snap.");
        Assert.True(maximum <= 1.13, $"The overshoot must stay close to the one asked for, measured {maximum:F4}.");
    }

    /// <summary>A zero overshoot gives a plain rise, which never goes above 1.</summary>
    [Fact]
    public void WithoutOvershootThePopNeverExceedsOne()
    {
        for (int i = 0; i <= 100; i++)
        {
            Assert.InRange(Easing.PopIn(i / 100.0, 0.0), 0.0, 1.0);
        }
    }

    [Fact]
    public void ThePopIsIncreasingAtTheStart()
    {
        double previous = Easing.PopIn(0.0, 0.12);
        for (int i = 1; i <= 40; i++)
        {
            double value = Easing.PopIn(i / 100.0, 0.12);
            Assert.True(value > previous, $"A pop that goes backwards at the start reads as a defect (i={i}).");
            previous = value;
        }
    }

    [Fact]
    public void ANegativeOvershootIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.PopIn(0.5, -0.1));
    }

    // --- Gulp ------------------------------------------------------------------------

    [Fact]
    public void TheGulpStartsAndReturnsToOne()
    {
        Assert.Equal(1.0, Easing.Gulp(0.0, 0.15), 9);
        Assert.Equal(1.0, Easing.Gulp(1.0, 0.15), 9);
    }

    [Fact]
    public void TheGulpReachesItsAmplitudeAtThePeak()
    {
        Assert.Equal(1.15, Easing.Gulp(0.5, 0.15), 9);
    }

    /// <summary>
    /// ⚠ Volume is preserved: the squashed axis is the INVERSE of the stretched one, not its mirror.
    /// With 1 − a, the head would lose area at the very moment it must look bigger.
    /// </summary>
    [Fact]
    public void BothGulpAxesPreserveTheArea()
    {
        double stretched = Easing.Gulp(0.5, 0.15);
        double squashed = 1.0 / stretched;

        Assert.Equal(1.0, stretched * squashed, 9);
        Assert.True(squashed > 1.0 - 0.15 + Tolerance,
            "The squashed axis must be 1/(1+a), which is greater than 1−a.");
    }

    [Fact]
    public void ANegativeAmplitudeIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.Gulp(0.5, -0.05));
    }

    // --- Falloff ---------------------------------------------------------------------

    /// <summary>
    /// ⚠ The final zero: this factor multiplies the head's angle. A residue would leave it crooked
    /// for the rest of the game, and a few turns the same way would settle it there.
    /// </summary>
    [Fact]
    public void TheFalloffStartsAtOneAndEndsExactlyAtZero()
    {
        Assert.Equal(1.0, Easing.Falloff(0.0), 9);
        Assert.Equal(0.0, Easing.Falloff(1.0), 9);
    }

    [Fact]
    public void TheFalloffStaysClampedOutsideItsRange()
    {
        Assert.Equal(1.0, Easing.Falloff(-3.0), 9);
        Assert.Equal(0.0, Easing.Falloff(4.0), 9);
    }

    /// <summary>It decreases, never rising again: a head straightening up does not lean back.</summary>
    [Fact]
    public void TheFalloffDecreasesStrictly()
    {
        double previous = Easing.Falloff(0.0);

        for (int i = 1; i <= 50; i++)
        {
            double current = Easing.Falloff(i / 50.0);
            Assert.True(current < previous + Tolerance,
                $"The falloff rose again between {(i - 1) / 50.0} and {i / 50.0}.");
            previous = current;
        }
    }

    /// <summary>
    /// Most of the distance is covered early: halfway through, less than a fifth of the angle is
    /// left. That is what reads as straightening up rather than as a slow drift.
    /// </summary>
    [Fact]
    public void TheFalloffIsFastAtTheStart()
    {
        Assert.True(Easing.Falloff(0.5) < 0.2);
    }

    /// <summary>
    /// It complements the rise of <c>PopIn</c> exactly: a single animation signature for the pop
    /// settling in and for the angle fading out.
    /// </summary>
    [Fact]
    public void TheFalloffIsTheComplementOfThePopRise()
    {
        for (int i = 0; i <= 10; i++)
        {
            double t = i / 10.0;
            Assert.Equal(1.0 - Easing.PopIn(t, 0.0), Easing.Falloff(t), 9);
        }
    }
}
