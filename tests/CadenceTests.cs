using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>What the design demands of the time step (GDD §4.1, and the rejection in §7).</summary>
public class CadenceTests
{
    [Fact]
    public void EightTicksPerSecondMakeA125MillisecondTick()
    {
        Assert.Equal(8.0, Cadence.DefaultTicksPerSecond);
        Assert.Equal(0.125, Cadence.DefaultTickDurationSeconds, 12);
        Assert.Equal(0.125, Cadence.TickDurationSeconds(), 12);
    }

    /// <summary>
    /// The rate is THE value that will be retried most often (§4.1): it must be settable from the
    /// caller, without recompiling. The constant is only a fallback.
    /// </summary>
    [Fact]
    public void TheRateIsOverriddenFromTheCaller()
    {
        Assert.Equal(1.0 / 6.0, Cadence.TickDurationSeconds(6.0), 12);
        Assert.Equal(0.1, Cadence.TickDurationSeconds(10.0), 12);
    }

    /// <summary>
    /// The default value is set BY JUDGEMENT, inside a 6–10 range to be tried in play: it must
    /// therefore fall inside it. If somebody moves the default outside the range, then the range has
    /// moved too — and that is discussed in the GDD, not here.
    /// </summary>
    [Fact]
    public void TheDefaultValueFallsInsideTheRangeToTry()
    {
        Assert.True(Cadence.IsWithinSuggestedRange(Cadence.DefaultTicksPerSecond));
        Assert.True(Cadence.IsWithinSuggestedRange(6.0));
        Assert.True(Cadence.IsWithinSuggestedRange(10.0));
        Assert.False(Cadence.IsWithinSuggestedRange(5.9));
        Assert.False(Cadence.IsWithinSuggestedRange(10.1));
    }

    /// <summary>
    /// Outside the range is still PLAYABLE: that is exactly what we want to be able to try. The
    /// range warns, it does not refuse — otherwise setting the rate would become coding work again.
    /// </summary>
    [Fact]
    public void ARateOutsideTheRangeIsStillComputable()
    {
        Assert.Equal(1.0 / 20.0, Cadence.TickDurationSeconds(20.0), 12);
        Assert.Equal(1.0 / 2.0, Cadence.TickDurationSeconds(2.0), 12);
    }

    /// <summary>
    /// No silent clamping: a mistyped tuning file must show itself at once, not produce a frozen
    /// game or an infinitely long tick that nobody could explain.
    /// </summary>
    [Fact]
    public void AnAbsurdRateThrowsInsteadOfBeingTrimmed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cadence.TickDurationSeconds(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cadence.TickDurationSeconds(-8.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cadence.TickDurationSeconds(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cadence.TickDurationSeconds(double.PositiveInfinity));
    }

    /// <summary>
    /// THE decision of §4.1, ruled by the author on 2026-08-27 against Nokia canon: a CONSTANT rate
    /// for the whole game. Speeding up with length is a multiplier, not a named rule; it blurs the
    /// attribution of death (§2) and makes the tick variable, so two runs incomparable on a bench
    /// (§7).
    ///
    /// This test is the guard rail of that decision: if it is ever reopened, it is reopened in the
    /// GDD.
    /// </summary>
    [Fact]
    public void TheRateDependsOnNeitherSnakeLengthNorPlayTime()
    {
        double reference = Cadence.EffectiveRate(Cadence.DefaultTicksPerSecond, Grid.InitialLength);

        for (int length = Grid.InitialLength; length <= Grid.Default.CellCount; length++)
        {
            Assert.Equal(reference, Cadence.EffectiveRate(Cadence.DefaultTicksPerSecond, length), 12);
        }
    }

    /// <summary>
    /// The snake moves one cell PER TICK, never between two ticks (§4.1): a frame step shorter than
    /// a tick produces no movement, and time accumulates.
    /// </summary>
    [Fact]
    public void AFrameStepShorterThanATickDoesNotMoveTheSnake()
    {
        double leftover;
        Assert.Equal(0, Cadence.TickCount(1.0 / 60.0, Cadence.DefaultTickDurationSeconds, out leftover));
        Assert.Equal(1.0 / 60.0, leftover, 12);
    }

    /// <summary>
    /// The leftover is CARRIED, not discarded. Zeroing the accumulator at every tick drifts the real
    /// rate downwards as soon as the frame step does not divide the tick duration — at 60 Hz, 125 ms
    /// falls between two frames. The drift raises nothing: it merely skews any measurement of run
    /// length, hence any future balancing bench (§6).
    ///
    /// Ten simulated seconds at 60 Hz must give 80 ticks give or take one, never 75.
    /// </summary>
    [Fact]
    public void TheLeftoverIsCarriedSoTheRateDoesNotDrift()
    {
        const double frameStep = 1.0 / 60.0;
        double tickDuration = Cadence.TickDurationSeconds();

        double accumulator = 0.0;
        int ticks = 0;

        for (int frame = 0; frame < 600; frame++)
        {
            accumulator += frameStep;
            ticks += Cadence.TickCount(accumulator, tickDuration, out accumulator);
        }

        Assert.InRange(ticks, 79, 80);

        // The remaining leftover is always strictly under one tick: without that, a "late" tick
        // would be lost instead of being caught up on the next frame.
        Assert.True(accumulator >= 0.0 && accumulator < tickDuration,
            $"Leftover out of bounds: {accumulator} (tick duration: {tickDuration}).");
    }

    /// <summary>
    /// A rate backlog IS NOT CAUGHT UP (§4.1, author's ruling of 2026-08-27): one frame moves the
    /// snake by one tick only. Without that cap, a one-second freeze covers eight cells at once,
    /// invisibly, and the death that follows is attributable to no turn — which §2 forbids.
    /// </summary>
    [Fact]
    public void AFrozenFrameOnlyMovesTheSnakeByOneTick()
    {
        double leftover;
        int ticks = Cadence.TickCount(0.9, Cadence.DefaultTickDurationSeconds, out leftover);

        Assert.Equal(1, ticks);
        Assert.Equal(Cadence.DefaultCatchUpCap, ticks);
    }

    /// <summary>
    /// THE trap of this rule: the backlog must be DISCARDED, not carried. If it were kept in the
    /// leftover, the cap would serve no purpose — the eight cells of the freeze would go through
    /// over eight successive frames instead of one, the player would watch them scroll past
    /// helplessly, and the defect the cap fixes would simply be spread out over time.
    ///
    /// This test fails if somebody returns the full backlog in the leftover: the ten frames after
    /// the freeze would play ten ticks instead of one.
    /// </summary>
    [Fact]
    public void TheDiscardedBacklogDoesNotComeBackOnLaterFrames()
    {
        const double frameStep = 1.0 / 60.0;
        double tickDuration = Cadence.TickDurationSeconds();

        // A freeze of about a second: seven ticks due, one played, six discarded.
        double accumulator = 0.9;
        Assert.Equal(1, Cadence.TickCount(accumulator, tickDuration, out accumulator));

        // The leftover now carries only the sub-tick fraction, never the backlog.
        Assert.True(accumulator < tickDuration, $"The backlog was carried: leftover {accumulator}.");

        int ticksAfter = 0;
        for (int frame = 0; frame < 10; frame++)
        {
            accumulator += frameStep;
            ticksAfter += Cadence.TickCount(accumulator, tickDuration, out accumulator);
        }

        // 10 frames at 60 Hz = 167 ms: a single tick, the one the normal rate puts there.
        Assert.Equal(1, ticksAfter);
    }

    /// <summary>
    /// The cap is tuning like everything else: somebody will want to try it at 2 without
    /// recompiling.
    /// </summary>
    [Fact]
    public void TheCatchUpCapIsTunable()
    {
        double leftover;

        Assert.Equal(2, Cadence.TickCount(0.9, Cadence.DefaultTickDurationSeconds, out leftover, 2));
        Assert.Equal(7, Cadence.TickCount(0.9, Cadence.DefaultTickDurationSeconds, out leftover, 8));

        // The cap does not CREATE ticks: below the cap, we play what is due, no more.
        Assert.Equal(1, Cadence.TickCount(0.2, Cadence.DefaultTickDurationSeconds, out leftover, 8));
    }

    /// <summary>
    /// A zero cap would freeze the snake without raising anything: that is the class of bug this
    /// repository hunts, so we throw when the setting is read.
    /// </summary>
    [Fact]
    public void ACapBelowOneTickIsRejected()
    {
        double leftover;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cadence.TickCount(0.5, Cadence.DefaultTickDurationSeconds, out leftover, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cadence.TickCount(0.5, Cadence.DefaultTickDurationSeconds, out leftover, -1));
    }

    /// <summary>
    /// The input window for a turn is exactly one tick (§4.1). It must stay shorter than a simple
    /// visual reaction time (200–250 ms, an accepted order of magnitude, NOT measured here): that is
    /// what forces the player to decide one cell ahead rather than react to the incoming wall. If
    /// the default value went above it, the skill §4.1 aims at would change in nature — and that is
    /// re-discussed in the GDD.
    /// </summary>
    [Fact]
    public void TheTurnWindowStaysBelowVisualReactionTime()
    {
        Assert.True(Cadence.TickDurationSeconds() < 0.200,
            "The turn window exceeds visual reaction time: §4.1 becomes false.");
    }
}
