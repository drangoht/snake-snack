using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// What the design FORBIDS in the input queue (GDD §4.2). Every test names the rule it locks down —
/// not the line of code it happens to cross.
/// </summary>
public class InputQueueTests
{
    /// <summary>
    /// THE test of §4.2, written first because it is what drives the whole design: snake heading
    /// east, the player presses North then South within the same tick.
    ///
    /// Neither North nor South is a reversal of EAST. Validated on press, both would go through and
    /// the next tick would apply South to a snake that had gone north: it bites its own neck.
    /// Validated at the tick against the direction ACTUALLY applied on the previous tick, South is
    /// compared with North, recognised as a reversal, rejected.
    /// </summary>
    [Fact]
    public void NorthThenSouthInTheSameTick_NorthGoesThroughAndSouthIsRejectedNextTick()
    {
        var queue = new InputQueue(Direction.East);

        Assert.Equal(EnqueueResult.Accepted, queue.Enqueue(Direction.North));
        Assert.Equal(EnqueueResult.Accepted, queue.Enqueue(Direction.South));

        TickResult first = queue.Tick();
        Assert.Equal(Direction.North, first.AppliedDirection);
        Assert.False(first.ReversalRejected);

        TickResult second = queue.Tick();
        Assert.True(second.ReversalRejected);
        Assert.Equal(Direction.South, second.RejectedDirection);

        // The tick carries the current direction over: the snake keeps going north, it does not turn
        // around and it does not stop either.
        Assert.Equal(Direction.North, second.AppliedDirection);
        Assert.Equal(Direction.North, queue.CurrentDirection);
    }

    /// <summary>
    /// Corollary of the previous test: the press is NEVER where a reversal is judged. Rejecting West
    /// on press while the snake heads east looks harmless — it is exactly the design that loses the
    /// North/South counter-example as soon as a turn slips in between.
    /// </summary>
    [Fact]
    public void ReversalIsNotJudgedOnPress()
    {
        var queue = new InputQueue(Direction.East);

        Assert.Equal(EnqueueResult.Accepted, queue.Enqueue(Direction.West));
        Assert.Equal(1, queue.PendingCount);

        TickResult tick = queue.Tick();
        Assert.True(tick.ReversalRejected);
        Assert.Equal(Direction.East, tick.AppliedDirection);
    }

    /// <summary>
    /// A reversal becomes legitimate as soon as a turn slips in: East then North applied, West is no
    /// longer a reversal. The rejection must be relative to the previous tick, not to the game's
    /// starting direction.
    /// </summary>
    [Fact]
    public void AfterATurn_ReversalRelativeToTheInitialDirectionIsLegitimate()
    {
        var queue = new InputQueue(Direction.East);

        queue.Enqueue(Direction.North);
        Assert.Equal(Direction.North, queue.Tick().AppliedDirection);

        queue.Enqueue(Direction.West);
        TickResult tick = queue.Tick();

        Assert.False(tick.ReversalRejected);
        Assert.Equal(Direction.West, tick.AppliedDirection);
    }

    /// <summary>
    /// One turn per tick, whatever happens: that is what guarantees the trajectory read on screen is
    /// the one the player pressed, in the order they pressed it.
    /// </summary>
    [Fact]
    public void ATickConsumesOnlyOneInput()
    {
        var queue = new InputQueue(Direction.East);

        queue.Enqueue(Direction.North);
        queue.Enqueue(Direction.West);
        Assert.Equal(2, queue.PendingCount);

        Assert.Equal(Direction.North, queue.Tick().AppliedDirection);
        Assert.Equal(1, queue.PendingCount);

        Assert.Equal(Direction.West, queue.Tick().AppliedDirection);
        Assert.Equal(0, queue.PendingCount);
    }

    /// <summary>
    /// Queue full: the new key is ignored, the oldest is NOT overwritten. Overwriting would silently
    /// cancel a turn that has already left the player's fingers — the snake would miss a turn the
    /// player genuinely pressed (§4.2).
    /// </summary>
    [Fact]
    public void QueueFull_TheNewKeyIsIgnoredAndTheOldestSurvives()
    {
        var queue = new InputQueue(Direction.East);

        Assert.Equal(EnqueueResult.Accepted, queue.Enqueue(Direction.North));
        Assert.Equal(EnqueueResult.Accepted, queue.Enqueue(Direction.West));
        Assert.Equal(EnqueueResult.RejectedQueueFull, queue.Enqueue(Direction.South));

        // The queue has not grown and its contents are intact, in order.
        Assert.Equal(2, queue.PendingCount);
        Assert.Equal(Direction.North, queue.Tick().AppliedDirection);
        Assert.Equal(Direction.West, queue.Tick().AppliedDirection);
    }

    /// <summary>
    /// Overflow must be OBSERVABLE: "invisible reads as non-existent" (§3). A press ignored with no
    /// on-screen feedback is read as a press the game missed.
    /// </summary>
    [Fact]
    public void EveryRejectionCarriesItsReason()
    {
        var queue = new InputQueue(Direction.East);

        Assert.Equal(EnqueueResult.RejectedDuplicate, queue.Enqueue(Direction.East));

        queue.Enqueue(Direction.North);
        queue.Enqueue(Direction.West);
        Assert.Equal(EnqueueResult.RejectedQueueFull, queue.Enqueue(Direction.South));

        queue.Pause();
        Assert.Equal(EnqueueResult.RejectedGamePaused, queue.Enqueue(Direction.North));
    }

    /// <summary>
    /// A press identical to the current direction (empty queue) uses no slot: otherwise hammering
    /// the key you are already following would fill the queue and make the next turn be missed.
    /// </summary>
    [Fact]
    public void EmptyQueue_APressIdenticalToTheCurrentDirectionIsRejected()
    {
        var queue = new InputQueue(Direction.East);

        Assert.Equal(EnqueueResult.RejectedDuplicate, queue.Enqueue(Direction.East));
        Assert.Equal(0, queue.PendingCount);
    }

    /// <summary>
    /// Same against the last direction ALREADY QUEUED: that is the case of the player hammering
    /// while the turn waits for its tick.
    /// </summary>
    [Fact]
    public void APressIdenticalToTheLastQueuedDirectionIsRejected()
    {
        var queue = new InputQueue(Direction.East);

        Assert.Equal(EnqueueResult.Accepted, queue.Enqueue(Direction.North));
        Assert.Equal(EnqueueResult.RejectedDuplicate, queue.Enqueue(Direction.North));
        Assert.Equal(1, queue.PendingCount);

        // ... and the slot left free serves the next turn, which does change something.
        Assert.Equal(EnqueueResult.Accepted, queue.Enqueue(Direction.West));
    }

    /// <summary>
    /// A duplicate stays a duplicate when the queue is full: announcing "queue full" would give the
    /// player a false reason for the rejection, and the UI would show the wrong feedback.
    /// </summary>
    [Fact]
    public void QueueFull_ADuplicateIsAnnouncedAsDuplicateNotAsOverflow()
    {
        var queue = new InputQueue(Direction.East);

        queue.Enqueue(Direction.North);
        queue.Enqueue(Direction.West);

        Assert.Equal(EnqueueResult.RejectedDuplicate, queue.Enqueue(Direction.West));
    }

    /// <summary>
    /// The rejected input is DISCARDED: it does not block the queue. Without that, a reversal
    /// pressed by mistake would freeze every following turn and the player would read "the game has
    /// stopped responding".
    /// </summary>
    [Fact]
    public void ARejectedInputDoesNotBlockTheOnesBehindIt()
    {
        var queue = new InputQueue(Direction.East);

        queue.Enqueue(Direction.West); // reversal, will be rejected at the tick
        queue.Enqueue(Direction.North);

        TickResult rejected = queue.Tick();
        Assert.True(rejected.ReversalRejected);
        Assert.Equal(Direction.East, rejected.AppliedDirection);

        Assert.Equal(Direction.North, queue.Tick().AppliedDirection);
    }

    /// <summary>
    /// Purge on pause (§4.2): resuming must restore the state VISIBLE on screen, not execute a turn
    /// pressed before the pause. The player looked at the frozen grid and plays what they see.
    /// </summary>
    [Fact]
    public void PauseEmptiesTheQueueAndResumeCarriesTheCurrentDirection()
    {
        var queue = new InputQueue(Direction.East);

        queue.Enqueue(Direction.North);
        queue.Enqueue(Direction.West);

        queue.Pause();
        Assert.Equal(0, queue.PendingCount);

        // A direction pressed during the pause is not queued (§3).
        Assert.Equal(EnqueueResult.RejectedGamePaused, queue.Enqueue(Direction.North));
        Assert.Equal(0, queue.PendingCount);

        queue.Resume();
        Assert.Equal(0, queue.PendingCount);
        Assert.Equal(Direction.East, queue.Tick().AppliedDirection);
    }

    /// <summary>
    /// The game does not tick while paused. A silent no-op would move the snake one cell during the
    /// pause with nothing to signal it: we throw, so it is seen at the right moment.
    /// </summary>
    [Fact]
    public void TickingWhilePausedIsACallerError()
    {
        var queue = new InputQueue(Direction.East);
        queue.Pause();

        Assert.Throws<InvalidOperationException>(() => queue.Tick());
    }

    /// <summary>
    /// Purge on death (§4.2): no turn pressed during the death throes must apply to the next game —
    /// restarting costs one key and zero waiting (§2), it must not inherit a panic gesture.
    /// </summary>
    [Fact]
    public void DeathEmptiesTheQueue()
    {
        var queue = new InputQueue(Direction.East);

        queue.Enqueue(Direction.North);
        queue.Enqueue(Direction.West);

        queue.Die();
        Assert.Equal(0, queue.PendingCount);

        queue.Reset(Grid.InitialOrientation);
        Assert.Equal(0, queue.PendingCount);
        Assert.False(queue.IsPaused);
        Assert.Equal(Direction.East, queue.CurrentDirection);
    }

    /// <summary>
    /// A depth of 1 would lose the second half of any S-bend pressed in under one tick: that is the
    /// usual origin of "this Snake misses my turns", ruled out in §7. This test shows what depth 2
    /// buys, and will fail if somebody brings the queue back to 1.
    /// </summary>
    [Fact]
    public void TheDefaultDepthAbsorbsAnLTurnPressedWithinOneTick()
    {
        Assert.Equal(2, InputQueue.DefaultDepth);

        var queue = new InputQueue(Direction.East);

        // The full "go up then go left" gesture, pressed faster than the rate.
        Assert.Equal(EnqueueResult.Accepted, queue.Enqueue(Direction.North));
        Assert.Equal(EnqueueResult.Accepted, queue.Enqueue(Direction.West));

        Assert.Equal(Direction.North, queue.Tick().AppliedDirection);
        Assert.Equal(Direction.West, queue.Tick().AppliedDirection);
    }
}
