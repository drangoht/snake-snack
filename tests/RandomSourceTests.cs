using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// What GDD §4.4 demands of the generator: a <b>reproducible</b> and <b>uniform</b> sequence.
/// </summary>
public class RandomSourceTests
{
    /// <summary>
    /// ⚠ <b>The most important test in the file, and the only one that cannot be rewritten.</b>
    /// These three values are the SplitMix64 reference vector for seed 1. They lock the algorithm
    /// down: the day somebody "improves" the mixing, every other test here would still pass (the
    /// sequence would stay uniform and reproducible) — but every paired bench already recorded would
    /// stop matching, with no symptom to say so. That is the stability contract of §4.4, written
    /// down hard.
    /// </summary>
    [Fact]
    public void TheSequenceIsSplitMix64()
    {
        RandomSource random = new RandomSource(1UL);

        Assert.Equal(0x910A2DEC89025CC1UL, random.Next());
        Assert.Equal(0xBEEB8DA1658EEC67UL, random.Next());
        Assert.Equal(0xF893A2EEFB32555EUL, random.Next());
    }

    [Fact]
    public void TwoGeneratorsSeededAlikeProduceTheSameSequence()
    {
        RandomSource first = new RandomSource(20260827UL);
        RandomSource second = new RandomSource(20260827UL);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(first.Next(), second.Next());
        }
    }

    /// <summary>
    /// Seeding 1 then 2 must not give two lookalike sequences: bench seeds are written by hand and
    /// will follow one another. That is what SplitMix64's golden step guarantees.
    /// </summary>
    [Fact]
    public void NeighbouringSeedsGiveDifferentSequences()
    {
        RandomSource first = new RandomSource(1UL);
        RandomSource second = new RandomSource(2UL);

        int identical = 0;
        for (int i = 0; i < 50; i++)
        {
            if (first.Next() == second.Next())
            {
                identical++;
            }
        }

        Assert.Equal(0, identical);
    }

    /// <summary>
    /// Replaying a game means restarting from the seed — not building a new object somewhere.
    /// Without this, §4.4 would force the caller to keep the seed on its own side.
    /// </summary>
    [Fact]
    public void ResetReplaysTheSameSequence()
    {
        RandomSource random = new RandomSource(7UL);

        ulong[] first = { random.Next(), random.Next(), random.Next() };
        random.Reset();

        Assert.Equal(first[0], random.Next());
        Assert.Equal(first[1], random.Next());
        Assert.Equal(first[2], random.Next());
    }

    [Fact]
    public void TheSeedCanBeReadBack()
    {
        Assert.Equal(42UL, new RandomSource(42UL).Seed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(315)]
    [InlineData(312)]
    public void NextIntStaysWithinItsBounds(int bound)
    {
        RandomSource random = new RandomSource(99UL);

        for (int i = 0; i < 2000; i++)
        {
            int draw = random.NextInt(bound);
            Assert.InRange(draw, 0, bound - 1);
        }
    }

    /// <summary>
    /// A single free cell: the draw has only one possible answer. That is the last tick before the
    /// win of §4.4, and it must neither loop nor go out of bounds.
    /// </summary>
    [Fact]
    public void NextIntOverASingleValueAlwaysReturnsZero()
    {
        RandomSource random = new RandomSource(3UL);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(0, random.NextInt(1));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NextIntRejectsAnEmptyBound(int bound)
    {
        RandomSource random = new RandomSource(1UL);

        // Returning 0 "by default" would put every apple on cell (0, 0), with no error.
        Assert.Throws<System.ArgumentOutOfRangeException>(() => random.NextInt(bound));
    }

    /// <summary>
    /// Coarse uniformity. A <c>% bound</c> written without rejecting the tail would favour small
    /// values — so, in play, the bottom-left corner of the grid: apples that "always fall on the same
    /// side", which no error would report.
    /// </summary>
    [Fact]
    public void NextIntSpreadsValuesUniformly()
    {
        const int buckets = 10;
        const int draws = 200000;

        RandomSource random = new RandomSource(2026UL);
        int[] counts = new int[buckets];

        for (int i = 0; i < draws; i++)
        {
            counts[random.NextInt(buckets)]++;
        }

        // Wide bounds (±5 %): the test must catch a systematic bias, not noise.
        int expected = draws / buckets;
        for (int i = 0; i < buckets; i++)
        {
            Assert.InRange(counts[i], (int)(expected * 0.95), (int)(expected * 1.05));
        }
    }
}
