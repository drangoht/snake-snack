using System.Collections.Generic;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Tuning that is settable without recompiling (GDD §4.1, §4.3) — and what it refuses to let
/// through.
/// </summary>
public class GameSettingsTests
{
    /// <summary>
    /// A missing or empty file must give exactly the set from the GDD: it is the fallback, and it
    /// must never drift from what the document describes.
    /// </summary>
    [Fact]
    public void TheDefaultSettingsAreTheOnesFromTheGdd()
    {
        GameSettings settings = GameSettings.Default();

        Assert.Equal(8.0, settings.ticksPerSecond);
        Assert.Equal(1, settings.catchUpCap);
        Assert.Equal(21, settings.gridWidth);
        Assert.Equal(15, settings.gridHeight);
        Assert.Equal(2, settings.queueDepth);
    }

    [Fact]
    public void ValidSettingsProduceNoIssue()
    {
        GameSettings safe = GameSettings.Default().Validate(out IList<string> issues);

        Assert.Empty(issues);
        Assert.Equal(8.0, safe.ticksPerSecond);
        Assert.Equal(21, safe.gridWidth);
    }

    /// <summary>
    /// ⚠ The case that motivates this whole validation: an even dimension raises nothing at runtime,
    /// it merely offsets the starting pose by half a cell (§4.3) — a defect nobody notices before a
    /// screenshot. The fallback must be the COMPLETE grid from the GDD, not some patched-up
    /// neighbouring width: 20 × 15 does not become 21 × 15 by accident, it goes back to the grid
    /// somebody decided on.
    /// </summary>
    [Fact]
    public void AnEvenGridFallsBackToTheGddOneAndSaysSo()
    {
        GameSettings raw = GameSettings.Default();
        raw.gridWidth = 20;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.Equal(21, safe.gridWidth);
        Assert.Equal(15, safe.gridHeight);
        Assert.NotEmpty(issues);
    }

    /// <summary>
    /// A zero rate freezes the game without raising anything: the snake stops moving, and the player
    /// thinks it crashed.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-4.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AnImpossibleRateFallsBackToTheGddOne(double rate)
    {
        GameSettings raw = GameSettings.Default();
        raw.ticksPerSecond = rate;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.Equal(Cadence.DefaultTicksPerSecond, safe.ticksPerSecond);
        Assert.NotEmpty(issues);
    }

    /// <summary>
    /// ⚠ The suggested 6–10 ticks/s range (§4.1) is ADVICE, not a bound: going outside it is exactly
    /// what we want to be able to try without recompiling. The value is therefore kept — but
    /// reported, so that a test session at 20 ticks/s does not think it is at nominal settings.
    /// </summary>
    [Fact]
    public void ARateOutsideTheRangeIsReportedButKept()
    {
        GameSettings raw = GameSettings.Default();
        raw.ticksPerSecond = 20.0;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.Equal(20.0, safe.ticksPerSecond);
        Assert.NotEmpty(issues);
    }

    /// <summary>
    /// A zero catch-up cap would freeze the snake (see <c>Cadence.TickCount</c>, which throws in
    /// that case): the fallback avoids crashing the game on the first clumsy setting.
    /// </summary>
    [Fact]
    public void AZeroCatchUpCapFallsBackToOne()
    {
        GameSettings raw = GameSettings.Default();
        raw.catchUpCap = 0;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.Equal(1, safe.catchUpCap);
        Assert.NotEmpty(issues);
    }

    /// <summary>
    /// An extension cap shorter than the display duration would put the pictogram out before it had
    /// been read — the exact opposite of what ART §5.5 expects. The settings coming out of here must
    /// be directly acceptable to <see cref="TimedFeedback"/>.
    /// </summary>
    [Fact]
    public void ARejectionCapShorterThanTheDisplayIsStraightened()
    {
        GameSettings raw = GameSettings.Default();
        raw.rejectionDisplaySeconds = 0.4;
        raw.rejectionExtensionCapSeconds = 0.1;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.True(safe.rejectionExtensionCapSeconds >= safe.rejectionDisplaySeconds);
        Assert.NotEmpty(issues);
    }

    /// <summary>
    /// The contract that really matters: whatever the JSON holds, the validated settings build a
    /// game that starts. Hand-edited tuning must never be able to throw at launch — at worst it must
    /// be ignored, noisily.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(20, 14)]
    [InlineData(3, 3)]
    [InlineData(-7, 15)]
    [InlineData(1001, 1001)]
    public void AbsurdSettingsStayConstructible(int width, int height)
    {
        GameSettings raw = GameSettings.Default();
        raw.gridWidth = width;
        raw.gridHeight = height;
        raw.ticksPerSecond = 0.0;
        raw.queueDepth = 0;
        raw.rejectionDisplaySeconds = -1.0;
        raw.rejectionFadeSeconds = 0.0;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.NotEmpty(issues);

        Grid grid = new Grid(safe.gridWidth, safe.gridHeight);
        InputQueue queue = new InputQueue(Grid.InitialOrientation, safe.queueDepth);
        Snake snake = new Snake(grid.StartingPose().Segments);
        TimedFeedback feedback = new TimedFeedback(
            safe.rejectionDisplaySeconds,
            safe.rejectionExtensionCapSeconds,
            safe.rejectionFadeSeconds);
        Board board = new Board(grid, Board.CellSizeFor(grid));

        Assert.Equal(MoveResult.Moved, snake.Advance(queue.Tick().AppliedDirection, grid));
        Assert.True(Cadence.TickCount(1.0, Cadence.TickDurationSeconds(safe.ticksPerSecond), out _, safe.catchUpCap) >= 0);
        Assert.False(feedback.IsVisible(0.0));
        Assert.True(board.CellSize >= 1);
    }

    /// <summary>
    /// ⚠ <b>Zero volume is an intent, not an error.</b> Every duration in this file falls back to
    /// its default when it is not strictly positive; applying that rule to the volume would give a
    /// player who asked for silence a game that keeps talking, and no way to understand why.
    /// </summary>
    [Fact]
    public void SilenceIsAValidVolume()
    {
        GameSettings raw = GameSettings.Default();
        raw.sfxVolume = 0.0;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.Equal(0.0, safe.sfxVolume);
        Assert.Empty(issues);
    }

    /// <summary>
    /// A volume out of range is <b>clamped</b>, not sent back to the default: someone writing 2
    /// wants the loudest the game has, and 0.8 would answer the opposite of their intent. Only a
    /// value that is not a number has no readable intent behind it.
    /// </summary>
    [Theory]
    [InlineData(2.0, 1.0)]
    [InlineData(-1.0, 0.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(0.35, 0.35)]
    public void AnOutOfRangeVolumeIsClamped(double written, double expected)
    {
        GameSettings raw = GameSettings.Default();
        raw.sfxVolume = written;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.Equal(expected, safe.sfxVolume);
    }

    /// <summary>
    /// The music volume follows exactly the same rules as the effects volume. Written down because
    /// the two are validated by separate calls: adding a third volume and forgetting its call would
    /// leave it unvalidated, and nothing else would say so.
    /// </summary>
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(3.0, 1.0)]
    [InlineData(-0.5, 0.0)]
    public void TheMusicVolumeIsValidatedLikeTheEffects(double written, double expected)
    {
        GameSettings raw = GameSettings.Default();
        raw.musicVolume = written;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.Equal(expected, safe.musicVolume);
    }

    /// <summary>A volume that is not a number has no intent to honour: back to the default.</summary>
    [Fact]
    public void AVolumeThatIsNotANumberFallsBack()
    {
        GameSettings raw = GameSettings.Default();
        raw.sfxVolume = double.NaN;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.Equal(GameSettings.Default().sfxVolume, safe.sfxVolume);
        Assert.NotEmpty(issues);
    }

    /// <summary>
    /// ⚠ A mute correction is worse than no correction: the player edits their JSON, sees no change,
    /// and has no way to know why. Every issue must be an actionable sentence, not a code.
    /// </summary>
    [Fact]
    public void EveryIssueIsAReadableSentence()
    {
        GameSettings raw = GameSettings.Default();
        raw.gridWidth = 20;
        raw.ticksPerSecond = -1.0;

        raw.Validate(out IList<string> issues);

        Assert.All(issues, sentence => Assert.True(sentence.Length > 20, $"Issue too terse: \"{sentence}\""));
    }

    /// <summary>
    /// The seed goes through validation <b>uncorrected and unreported</b>: the whole range of
    /// <c>long</c> names a legitimate sequence (GDD §4.4). A "corrected" seed would replay a
    /// different game from the one we meant to replay, which is exactly the opposite of the point.
    /// </summary>
    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-20260827L)]
    [InlineData(long.MaxValue)]
    public void TheSeedIsNeverCorrected(long seed)
    {
        GameSettings raw = GameSettings.Default();
        raw.seed = seed;

        GameSettings safe = raw.Validate(out IList<string> issues);

        Assert.Equal(seed, safe.seed);
        Assert.Empty(issues);
    }

    /// <summary>By default, every game gets a fresh seed (§4.4).</summary>
    [Fact]
    public void ByDefaultNoSeedIsFixed()
    {
        Assert.Equal(GameSettings.ClockSeed, GameSettings.Default().seed);
    }
}
