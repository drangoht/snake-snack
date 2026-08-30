using System;
using System.Collections.Generic;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// What the art brief demands of the feedback for a rejected input (<c>docs/ART.md</c> §5).
/// </summary>
public class RejectionRoutingTests
{
    /// <summary>
    /// ART §5.7: "Never any feedback for <c>RejectedDuplicate</c>". It is the ban most easily undone
    /// by accident while "unifying" the routing — hence a test of its own.
    /// </summary>
    [Fact]
    public void ADuplicateGetsNoFeedback()
    {
        Assert.Equal(FeedbackChannel.None, RejectionRouting.Channel(RejectionReason.Duplicate));
    }

    /// <summary>
    /// ART §5.2: reversal and queue-full share the SAME pictogram. At 125 ms per tick, nothing can
    /// teach the nuance; what must read is that the press did not count.
    /// </summary>
    [Fact]
    public void ReversalAndQueueFullShareTheSamePictogram()
    {
        Assert.Equal(FeedbackChannel.Pictogram, RejectionRouting.Channel(RejectionReason.Reversal));
        Assert.Equal(FeedbackChannel.Pictogram, RejectionRouting.Channel(RejectionReason.QueueFull));
    }

    [Fact]
    public void ADirectionPressedWhilePausedGoesToThePauseScreen()
    {
        Assert.Equal(FeedbackChannel.PauseText, RejectionRouting.Channel(RejectionReason.GamePaused));
    }

    /// <summary>
    /// ⚠ The test that guards against mute rejection: every reason must have a DECIDED channel.
    /// Adding a value to the enum without routing it makes this test fail rather than produce an
    /// invisible rejection — therefore, for the player, a non-existent one (GDD §3).
    /// </summary>
    [Fact]
    public void EveryRejectionReasonHasADecidedChannel()
    {
        foreach (RejectionReason reason in Enum.GetValues(typeof(RejectionReason)))
        {
            RejectionRouting.Channel(reason);
        }
    }

    /// <summary>
    /// The same guard rail on the source side: every enqueue result must translate, or clearly say
    /// there is nothing to translate.
    /// </summary>
    [Fact]
    public void EveryEnqueueResultTranslatesIntoAFeedbackReason()
    {
        foreach (EnqueueResult result in Enum.GetValues(typeof(EnqueueResult)))
        {
            bool rejected = RejectionRouting.FromEnqueue(result, out RejectionReason reason);

            Assert.Equal(result != EnqueueResult.Accepted, rejected);

            if (rejected)
            {
                RejectionRouting.Channel(reason);
            }
        }
    }

    /// <summary>
    /// ⚠ The trap the corrected brief points out: <c>EnqueueResult</c> does NOT contain reversal,
    /// because reversal is judged at the tick (GDD §4.2). A UI wired to <c>Enqueue()</c> alone would
    /// therefore never show the rejection §3 requires to be made visible. This test locks the
    /// consequence down: no enqueue result produces the reversal reason.
    /// </summary>
    [Fact]
    public void NoEnqueueResultProducesTheReversalReason()
    {
        foreach (EnqueueResult result in Enum.GetValues(typeof(EnqueueResult)))
        {
            if (RejectionRouting.FromEnqueue(result, out RejectionReason reason))
            {
                Assert.NotEqual(RejectionReason.Reversal, reason);
            }
        }
    }
}

/// <summary>
/// The anti-repeat of the feedback (<c>docs/ART.md</c> §5.5) — the part that handles hammering.
/// </summary>
public class TimedFeedbackTests
{
    private const double Display = 0.25;   // ART §5.5: 250 ms
    private const double Cap = 0.5;        // ART §5.5: 500 ms
    private const double Fade = 0.06;

    private static TimedFeedback Fresh()
    {
        return new TimedFeedback(Display, Cap, Fade);
    }

    [Fact]
    public void ANotificationMakesTheFeedbackVisible()
    {
        TimedFeedback state = Fresh();

        Assert.True(state.Notify(0.0));
        Assert.True(state.IsVisible(0.1));
        Assert.False(state.IsVisible(1.0));
    }

    /// <summary>
    /// ART §5.5: "a notification received while the feedback is already visible EXTENDS the
    /// deadline, WITHOUT restarting the appearance animation". The returned <c>false</c> is the
    /// signal telling the caller to replay nothing.
    /// </summary>
    [Fact]
    public void ASecondNotificationExtendsWithoutRestartingTheAppearance()
    {
        TimedFeedback state = Fresh();
        state.Notify(0.0);

        Assert.False(state.Notify(0.2));

        // Extended: past the initial 0.25 s deadline the feedback is still there...
        Assert.True(state.IsVisible(0.4));
        // ... and it is at full opacity, not replaying its fade-in.
        Assert.Equal(1.0, state.Opacity(0.4), 9);
    }

    /// <summary>
    /// ⚠ The ban of ART §5.7: "never a feedback that exceeds its continuous-extension cap without
    /// going out at least once". The test replays the worst possible hammering — a notification on
    /// EVERY frame — in the engine's real order (handle inputs, then draw), and demands that the
    /// drawn opacity fall back to zero.
    ///
    /// <para>⚠ The measurement is on the <b>DURATION</b> for which opacity stays zero, not on the
    /// fact that it reaches zero. The first version of this test only checked "opacity falls back to
    /// zero": <b>it went green on an implementation with no protection at all</b>, because a
    /// notification restarting the state necessarily makes opacity zero at the instant of restart.
    /// The moment of going out exists in both cases; what distinguishes a visible extinction from a
    /// non-extinction is that it lasts.
    ///
    /// <para>The current version was seen RED before being kept: removing the dead time from
    /// <c>Notify</c> drops the longest zero-opacity stretch to one frame and the test fails.</para>
    /// </summary>
    [Fact]
    public void UnderContinuousHammeringTheFeedbackStaysOutLongEnoughToBeSeen()
    {
        TimedFeedback state = Fresh();
        double outSince = -1.0;
        double longestExtinction = 0.0;

        for (int i = 0; i <= 2000; i++)
        {
            double now = i / 1000.0;
            state.Notify(now);

            if (state.Opacity(now) <= 1e-9)
            {
                if (outSince < 0.0)
                {
                    outSince = now;
                }

                double duration = now - outSince;
                if (duration > longestExtinction)
                {
                    longestExtinction = duration;
                }
            }
            else
            {
                outSince = -1.0;
            }
        }

        Assert.True(longestExtinction >= Fade - 0.002,
            $"The longest extinction lasts {longestExtinction} s: too short to be seen, the cap caps nothing.");
    }

    /// <summary>
    /// Continuous visibility never exceeds the cap, fade-out included: the same ban, measured in
    /// numbers.
    /// </summary>
    [Fact]
    public void ContinuousVisibilityNeverExceedsTheCap()
    {
        TimedFeedback state = Fresh();
        double visibleSince = -1.0;
        double worstDuration = 0.0;

        for (int i = 0; i <= 4000; i++)
        {
            double now = i / 1000.0;
            state.Notify(now);

            if (state.Opacity(now) > 1e-9)
            {
                if (visibleSince < 0.0)
                {
                    visibleSince = now;
                }

                double duration = now - visibleSince;
                if (duration > worstDuration)
                {
                    worstDuration = duration;
                }
            }
            else
            {
                visibleSince = -1.0;
            }
        }

        // The cap plus the fade-out, which comes after the deadline by construction.
        Assert.True(worstDuration <= Cap + Fade + 0.002,
            $"Continuous visibility of {worstDuration} s for a cap of {Cap} s.");
    }

    /// <summary>
    /// ART §5.7: "a single fade-in/fade-out envelope per trigger", never a strobe. The test checks
    /// that an opacity which has started coming down NEVER rises again without passing through zero
    /// first — that is the operational definition of "no re-flash".
    /// </summary>
    [Fact]
    public void OpacityNeverRisesAgainWithoutPassingThroughZero()
    {
        TimedFeedback state = Fresh();
        double previous = 0.0;
        bool falling = false;

        for (int i = 0; i <= 3000; i++)
        {
            double now = i / 1000.0;
            state.Notify(now);
            double opacity = state.Opacity(now);

            if (opacity < previous - 1e-9)
            {
                falling = true;
            }
            else if (opacity > previous + 1e-9)
            {
                Assert.False(falling && previous > 1e-9,
                    $"Re-flash at t={now}: opacity rises from {previous} to {opacity} without passing through zero.");
                falling = false;
            }

            previous = opacity;
        }
    }

    /// <summary>
    /// A notification well after the extinction is a NEW trigger: the caller must reposition the
    /// pictogram (the head has moved) and replay the envelope.
    /// </summary>
    [Fact]
    public void AfterFullExtinctionANotificationRestartsTheAppearance()
    {
        TimedFeedback state = Fresh();
        state.Notify(0.0);

        Assert.True(state.Notify(5.0));
        Assert.Equal(0.0, state.Opacity(5.0), 9);
        Assert.Equal(1.0, state.Opacity(5.0 + Fade), 9);
    }

    [Fact]
    public void ClearCutsTheFeedbackAtOnce()
    {
        TimedFeedback state = Fresh();
        state.Notify(0.0);

        state.Clear();

        Assert.False(state.IsVisible(0.1));
        Assert.Equal(0.0, state.Opacity(0.1), 9);
    }

    /// <summary>
    /// A cap shorter than the display duration would put the feedback out before it was read: that
    /// is an inconsistent setting, it must fail at construction rather than produce a flicker nobody
    /// can explain.
    /// </summary>
    [Fact]
    public void ACapShorterThanTheDisplayIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimedFeedback(0.25, 0.1, 0.01));
    }

    [Fact]
    public void AFadeThatDoesNotFitTwiceInTheDisplayIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimedFeedback(0.25, 0.5, 0.2));
    }

    /// <summary>
    /// The pause-screen text uses the same mechanism with its own durations (ART §5.5: 1.5 s, not
    /// tied to the tick). It must hold that second and a half without flickering.
    /// </summary>
    [Fact]
    public void ThePauseTextChannelSupportsItsOwnDurations()
    {
        TimedFeedback text = new TimedFeedback(1.5, 3.0, 0.1);
        List<double> opacities = new List<double>();

        text.Notify(0.0);
        // From 0.2 s to 1.4 s: the whole hold, before the 1.5 s deadline.
        for (int i = 0; i <= 12; i++)
        {
            opacities.Add(text.Opacity(0.2 + (i / 10.0)));
        }

        Assert.All(opacities, o => Assert.True(o > 0.0));
    }
}
