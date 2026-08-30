using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// The game's pseudo-random generator: seeded by an integer, <b>reproducible everywhere</b>
    /// (GDD §4.4, "Reproducible randomness").
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This type exists because no platform generator will do</b>, and the reason is not a
    /// matter of style:
    /// <list type="bullet">
    /// <item><c>UnityEngine.Random</c> is <b>shared global state</b> — any visual effect drawing
    /// from it would shift the apple sequence — and it is unavailable in <c>Rules/</c> anyway, which
    /// depends on no engine.</item>
    /// <item><c>System.Random</c>'s sequence is <b>not contractually stable</b> across runtimes:
    /// .NET Core 2.0 and then .NET 6 both changed the algorithm. A bench whose apples differ between
    /// <c>dotnet test</c>, the desktop build (Mono/IL2CPP) and the WebGL build no longer compares
    /// anything — and it would raise no error to say so.</item>
    /// </list>
    ///
    /// <para>The algorithm is <b>SplitMix64</b>: four lines of unsigned 64-bit arithmetic whose
    /// result depends only on the language — <c>unchecked</c>, logical shifts and multiplication
    /// modulo 2^64 are defined identically by every C# implementation. That is what makes the
    /// sequence identical on all three targets. It does not have to be cryptographic: it has to be
    /// <i>uniform</i> and <i>replayable</i>, nothing more.</para>
    ///
    /// <para>⚠ <b>Nothing but the apple draws from the game instance</b> (GDD §4.4). A cosmetic or
    /// audio need takes its <b>own</b> instance: drawing from this one would shift the whole apple
    /// sequence without a single test failing.</para>
    /// </remarks>
    public sealed class RandomSource
    {
        // SplitMix64 constants (Steele, Lea & Flood, 2014). The golden step 2^64/φ spreads
        // neighbouring seeds apart: seeding 1 then 2 does not yield two lookalike sequences — which
        // matters, given that bench seeds will be written by hand and follow one another.
        private const ulong GoldenStep = 0x9E3779B97F4A7C15UL;
        private const ulong Mix1 = 0xBF58476D1CE4E5B9UL;
        private const ulong Mix2 = 0x94D049BB133111EBUL;

        private ulong _state;

        /// <param name="seed">
        /// Seed of the sequence. Two instances seeded with the same value produce <b>exactly</b> the
        /// same sequence, on any target.
        /// </param>
        public RandomSource(ulong seed)
        {
            Seed = seed;
            _state = seed;
        }

        /// <summary>The seed received at construction — log it to replay the game.</summary>
        public ulong Seed { get; }

        /// <summary>Restarts from the original seed: the sequence replays identically.</summary>
        public void Reset()
        {
            _state = Seed;
        }

        /// <summary>The next 64-bit integer of the sequence, uniform over the whole range.</summary>
        public ulong Next()
        {
            unchecked
            {
                _state += GoldenStep;
                ulong z = _state;
                z = (z ^ (z >> 30)) * Mix1;
                z = (z ^ (z >> 27)) * Mix2;
                return z ^ (z >> 31);
            }
        }

        /// <summary>
        /// A uniform integer in <c>[0, exclusiveBound)</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="exclusiveBound"/> is not strictly positive — there is then no value to
        /// return, and returning 0 "by default" would put the apple on cell (0, 0).
        /// </exception>
        /// <remarks>
        /// ⚠ <b>This rejection is NOT the one the GDD §4.4 forbids.</b> The trap ruled out there is
        /// "draw a cell at random, redraw while it is occupied": on a nearly full grid its
        /// expectation tends to infinity and the game freezes silently. Here we reject the
        /// <b>non-divisible tail</b> of 2^64, whose size is at most <c>exclusiveBound</c>: for a
        /// 315-cell grid the probability of looping again is under 2·10⁻¹⁷ per draw. Without this
        /// rejection, a plain <c>% bound</c> would favour small values — so, here, the top-left
        /// corner of the grid.
        /// </remarks>
        public int NextInt(int exclusiveBound)
        {
            if (exclusiveBound <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveBound), exclusiveBound,
                    "There must be at least one possible value to draw one.");
            }

            ulong bound = (ulong)exclusiveBound;

            // 2^64 mod bound, written without ever representing 2^64: this is the size of the tail
            // that overflows past the last whole multiple of `bound`.
            ulong threshold = (ulong.MaxValue - bound + 1) % bound;

            ulong draw;
            do
            {
                draw = Next();
            }
            while (draw < threshold);

            return (int)(draw % bound);
        }
    }
}
