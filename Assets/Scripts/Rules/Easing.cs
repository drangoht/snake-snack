using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// The curves of the juice (<c>docs/art/juicy.md</c> §2): the shape of a feedback over time.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>No engine dependency, and that is what makes the juice verifiable.</b> An animation is
    /// the thing you notice least when it is wrong: an overshoot that does not come back exactly to
    /// 1, a pulse that does not fall back to 0, a progress value that exceeds 1 on a long frame —
    /// none of that raises anything, and none of it shows to the eye over 150 ms. Here every curve
    /// is a pure function of <c>(start, duration, now)</c>, so it is replayed in microseconds by
    /// <c>dotnet test</c> with no build and no engine.
    ///
    /// <para>⚠ <b>These functions decide nothing.</b> They return a factor; it is the presentation
    /// layer (<c>BoardView</c>, <c>GameHud</c>) that chooses what to do with it — a scale, an
    /// opacity, a camera size. No value read by a game rule (collision, pictogram anchor) goes
    /// through here: the juice observes the state, it never feeds it
    /// (<c>docs/art/juicy.md</c> §11).</para>
    /// </remarks>
    public static class Easing
    {
        /// <summary>
        /// Linear progress in <c>[0, 1]</c> of an envelope started at <paramref name="start"/>.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Clamped at both ends.</b> A long frame — the very first one after a WebGL load
        /// easily eats several hundred milliseconds — would otherwise push the factor out of range
        /// and throw a segment beyond its target cell. A <c>now</c> earlier than the start (clock
        /// re-read after a pause) would return a negative.
        /// </remarks>
        public static double Progress(double start, double duration, double now)
        {
            if (duration <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration), duration,
                    "An envelope of zero duration has no frame in which to be seen: it would be invisible, therefore non-existent (docs/art/juicy.md §11).");
            }

            double t = (now - start) / duration;

            if (t <= 0.0)
            {
                return 0.0;
            }

            return t >= 1.0 ? 1.0 : t;
        }

        /// <summary>
        /// Round trip: <c>0 → 1 → 0</c>, peaking halfway. The flash of the offending cell, the score
        /// bump, the micro-zoom of death (<c>juicy.md</c> §5, §6, §8).
        /// </summary>
        /// <remarks>
        /// A sine rather than two straight segments: a triangle marks a sharp angle at the peak and
        /// at both ends, which the eye reads as a jolt — exactly what a piece of juice must avoid.
        /// </remarks>
        public static double Pulse(double t)
        {
            if (t <= 0.0 || t >= 1.0)
            {
                return 0.0;
            }

            return Math.Sin(t * Math.PI);
        }

        /// <summary>
        /// Appearance with overshoot: <c>0 → past 1 → 1</c>. The pop of the new tail segment and of
        /// the apple (<c>juicy.md</c> §5, §7).
        /// </summary>
        /// <param name="t">Progress in <c>[0, 1]</c>, as returned by <see cref="Progress"/>.</param>
        /// <param name="overshoot">
        /// Height of the overshoot, as a fraction: <c>0.12</c> for a peak at 1.12. Zero gives a
        /// plain rise, with no bounce.
        /// </param>
        /// <remarks>
        /// ⚠ <b>Coming back to exactly 1 at the end of the envelope is not negotiable</b>: this
        /// factor multiplies the size of a segment that then stays on screen for the whole game. An
        /// error of 1 % would leave a segment permanently bigger than its neighbours — a lasting
        /// defect born of a 140 ms animation, and one nobody would think to look for there.
        /// </remarks>
        public static double PopIn(double t, double overshoot)
        {
            if (overshoot < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(overshoot), overshoot,
                    "A negative overshoot would shrink the object before growing it: that is not a pop, it is a defect.");
            }

            if (t <= 0.0)
            {
                return 0.0;
            }

            if (t >= 1.0)
            {
                return 1.0;
            }

            // Ease-out rise (most of the distance is covered early: that is what gives the "snap"),
            // then a bump that cancels itself at both ends for the overshoot.
            double rise = 1.0 - Math.Pow(1.0 - t, 3.0);
            return rise + (overshoot * Math.Sin(t * Math.PI));
        }

        /// <summary>
        /// Falloff: <c>1 → 0</c>, fast first then gently. The head tilt returning to level after a
        /// turn (<c>juicy.md</c> §9).
        /// </summary>
        /// <param name="t">Progress in <c>[0, 1]</c>, as returned by <see cref="Progress"/>.</param>
        /// <remarks>
        /// Exactly the complement of <see cref="PopIn"/>'s rise: the angle therefore fades at the
        /// same rate as the pop settles, which gives the game a single animation signature rather
        /// than two neighbouring but different curves.
        ///
        /// <para>⚠ <b>Reaching exactly 0 matters as much as <see cref="PopIn"/> reaching 1</b>: this
        /// factor multiplies an angle, and a residue would leave the head permanently crooked —
        /// after a few turns the same way, crooked for good.</para>
        /// </remarks>
        public static double Falloff(double t)
        {
            if (t <= 0.0)
            {
                return 1.0;
            }

            if (t >= 1.0)
            {
                return 0.0;
            }

            double remaining = 1.0 - t;
            return remaining * remaining * remaining;
        }

        /// <summary>
        /// Squash then return: gives the squash factor of the head's "gulp" (<c>juicy.md</c> §5), to
        /// be applied to one axis and its inverse to the other.
        /// </summary>
        /// <param name="t">Progress in <c>[0, 1]</c>.</param>
        /// <param name="amplitude">Maximum deviation, as a fraction: <c>0.15</c> for 1.15 / 0.85.</param>
        /// <remarks>
        /// ⚠ Volume is preserved: the stretched axis is <c>1 + a</c> while the squashed axis is
        /// <c>1 / (1 + a)</c>, not <c>1 - a</c>. Two symmetric factors would make the head lose area
        /// at the very moment it must look bigger — it would swallow by shrinking.
        /// </remarks>
        public static double Gulp(double t, double amplitude)
        {
            if (amplitude < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amplitude), amplitude,
                    "A negative amplitude would invert the squash: the head would stretch along its heading instead of swelling.");
            }

            return 1.0 + (amplitude * Pulse(t));
        }
    }
}
