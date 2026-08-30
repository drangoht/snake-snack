using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// The score and the best score (GDD §4.5).
/// </summary>
public class ScoreTests
{
    [Fact]
    public void AGameStartsAtZero()
    {
        var score = new Score();

        Assert.Equal(0, score.Points);
        Assert.Equal(0, score.Best);
        Assert.False(score.BestBeaten);
    }

    [Fact]
    public void EveryAppleIsWorthExactlyOnePoint()
    {
        var score = new Score();

        for (int i = 1; i <= 5; i++)
        {
            score.CountApple();
            Assert.Equal(i, score.Points);
        }
    }

    /// <summary>
    /// §4.5: "the best score rises during the game, as soon as the current score passes it — not on
    /// death". A best score sitting below the score displayed next to it reads as a bug.
    /// </summary>
    [Fact]
    public void TheBestScoreRisesWithTheScoreAsSoonAsItIsPassed()
    {
        var score = new Score(2);

        score.CountApple();
        Assert.Equal(2, score.Best);

        score.CountApple();
        Assert.Equal(2, score.Best);

        score.CountApple();
        Assert.Equal(3, score.Best);
        Assert.Equal(3, score.Points);
    }

    /// <summary>
    /// The return value of <c>CountApple</c> is the persistent-write signal: it must be true only on
    /// the ticks where the best score really changes value, otherwise the game writes to storage on
    /// every apple of every game.
    /// </summary>
    [Fact]
    public void TheBestScoreSignalIsOnlyTrueOnTheTickItChanges()
    {
        var score = new Score(2);

        Assert.False(score.CountApple());
        Assert.False(score.CountApple());
        Assert.True(score.CountApple());
        Assert.True(score.CountApple());
    }

    [Fact]
    public void TheBestScoreSurvivesANewGame()
    {
        var score = new Score();
        score.CountApple();
        score.CountApple();
        score.CountApple();

        score.NewGame();

        Assert.Equal(0, score.Points);
        Assert.Equal(3, score.Best);
        Assert.False(score.BestBeaten);
    }

    [Fact]
    public void ABadGameDoesNotLowerTheBestScore()
    {
        var score = new Score(5);
        score.NewGame();

        score.CountApple();
        score.CountApple();

        Assert.Equal(2, score.Points);
        Assert.Equal(5, score.Best);
        Assert.False(score.BestBeaten);
    }

    /// <summary>
    /// ⚠ The case that gets lost when <c>BestBeaten</c> is written as <c>Points == Best</c>:
    /// matching your personal best does put both numbers at the same value, but beats nothing.
    /// Showing "new best" here would make the game's only rewarding moment lie.
    /// </summary>
    [Fact]
    public void MatchingTheBestScoreDoesNotBeatIt()
    {
        var score = new Score(2);
        score.NewGame();

        score.CountApple();
        score.CountApple();

        Assert.Equal(score.Best, score.Points);
        Assert.False(score.BestBeaten);
    }

    [Fact]
    public void PassingTheBestScoreByASinglePointBeatsIt()
    {
        var score = new Score(2);
        score.NewGame();

        score.CountApple();
        score.CountApple();
        score.CountApple();

        Assert.True(score.BestBeaten);
        Assert.Equal(3, score.Best);
    }

    /// <summary>
    /// A player's very first game, unknown best score at zero: the first apple already beats the
    /// best. That is intended — otherwise the line would never appear during the game that
    /// discovers the game.
    /// </summary>
    [Fact]
    public void TheFirstAppleOfTheFirstGameBeatsTheBestScore()
    {
        var score = new Score();

        score.CountApple();

        Assert.True(score.BestBeaten);
    }

    /// <summary>
    /// An unreadable best score restarts from zero <b>with no blocking error</b> (§4.5): the game
    /// must never refuse to start over a counter.
    /// </summary>
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(int.MinValue, 0)]
    [InlineData(0, 0)]
    [InlineData(14, 14)]
    public void ADamagedBestScoreRestartsFromZeroWithoutThrowing(int read, int expected)
    {
        Assert.Equal(expected, Score.NormaliseBest(read));
        Assert.Equal(expected, new Score(read).Best);
    }

    /// <summary>
    /// The invariant of §4.5 — <c>length == 3 + score</c> — checked on the real snake rather than
    /// asserted in a comment: it is what justifies NOT displaying length, and it would break
    /// silently the day growth moved to the next tick.
    /// </summary>
    [Fact]
    public void TheSnakeLengthIsAlwaysThreePlusTheScore()
    {
        Grid grid = Grid.Default;
        var snake = new Snake(grid.StartingPose().Segments);
        var score = new Score();

        Assert.Equal(Score.SnakeLength(score.Points), snake.Length);

        // An apple placed straight ahead of the head on every tick: the snake eats every time, and
        // the default grid leaves ten steps eastwards before the wall.
        for (int i = 0; i < 8; i++)
        {
            Cell apple = Directions.Advance(snake.Head, Direction.East);

            bool ate;
            Assert.Equal(MoveResult.Moved, snake.Advance(Direction.East, grid, apple, out ate));
            Assert.True(ate);

            score.CountApple();

            Assert.Equal(Score.SnakeLength(score.Points), snake.Length);
        }

        Assert.Equal(8, score.Points);
        Assert.Equal(11, snake.Length);
    }
}
