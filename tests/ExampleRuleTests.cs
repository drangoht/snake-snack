using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// TEMPLATE — copy it, then delete it along with the rule it goes with.
///
/// A test does not aim at line coverage: it locks down what the DESIGN forbids. A test that
/// paraphrases the implementation breaks at every refactor without ever catching anything; one that
/// says "the curve must never double from one level to the next" catches the real regression.
/// </summary>
public class ExampleRuleTests
{
    [Fact]
    public void TheFirstLevelCostsNothing()
    {
        Assert.Equal(0, ExampleRule.LevelThreshold(1));
        Assert.Equal(0, ExampleRule.LevelThreshold(0));
    }

    [Fact]
    public void TheCurveIsStrictlyIncreasing()
    {
        for (int level = 1; level < 50; level++)
        {
            Assert.True(ExampleRule.LevelThreshold(level + 1) > ExampleRule.LevelThreshold(level),
                $"Level {level + 1} does not cost more than level {level}.");
        }
    }

    [Fact]
    public void NoStepDoublesThePreviousOne()
    {
        // The design intent: progression slows down, but never all at once. That sentence is what
        // the test protects, not the formula.
        for (int level = 2; level < 50; level++)
        {
            int previous = ExampleRule.LevelThreshold(level) - ExampleRule.LevelThreshold(level - 1);
            int current = ExampleRule.LevelThreshold(level + 1) - ExampleRule.LevelThreshold(level);
            Assert.True(current < previous * 2, $"The step doubles at level {level + 1}.");
        }
    }
}
