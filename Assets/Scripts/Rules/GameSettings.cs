using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// The game's tuning values, exactly as written in
    /// <c>Assets/StreamingAssets/settings.json</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Public fields in camelCase, and that is deliberate</b>: <c>JsonUtility</c> matches JSON
    /// keys to <i>fields</i> by their exact name. Renaming a field to PascalCase (the project
    /// convention) would silently fall back to its default — the settings file would have no effect
    /// at all, without a single line of error. This is the only breach of the repository's naming
    /// convention, and it is confined to this type.
    ///
    /// <para>⚠ <b>No value is applied without going through <see cref="Validate"/></b>: a tuning
    /// file is hand-edited, so sooner or later it holds an even width or a rate of zero. A zero that
    /// gets through freezes the game; an even dimension offsets the starting pose by half a cell
    /// (§4.3). Neither raises anything at runtime.</para>
    ///
    /// <para>This type has no engine dependency: <c>[Serializable]</c> comes from <c>System</c>, not
    /// from <c>UnityEngine</c>. It is therefore readable by <c>JsonUtility</c> on the Unity side and
    /// testable by <c>dotnet test</c> on the pure-logic side.</para>
    /// </remarks>
    [Serializable]
    public sealed class GameSettings
    {
        /// <summary>Game rate, in ticks per second (GDD §4.1: 8, range 6–10).</summary>
        public double ticksPerSecond = Cadence.DefaultTicksPerSecond;

        /// <summary>Maximum ticks played in one frame (GDD §4.1: 1, the backlog is discarded).</summary>
        public int catchUpCap = Cadence.DefaultCatchUpCap;

        /// <summary>Grid columns — <b>must be odd</b> (GDD §4.3).</summary>
        public int gridWidth = Grid.DefaultWidth;

        /// <summary>Grid rows — <b>must be odd</b> (GDD §4.3).</summary>
        public int gridHeight = Grid.DefaultHeight;

        /// <summary>Depth of the input queue (GDD §4.2: 2, tied to the rate).</summary>
        public int queueDepth = InputQueue.DefaultDepth;

        /// <summary>
        /// Seed of the apple draw (GDD §4.4). <b><see cref="ClockSeed"/> (0) = draw a fresh seed for
        /// every game.</b>
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Zero is a sentinel, not a seed</b>: <c>JsonUtility</c> cannot tell a missing key
        /// from a key set to zero (it falls back to the field's value in both cases), so "no seed"
        /// and "seed 0" are indistinguishable. The accepted price is one value lost out of 2^64; in
        /// exchange, setting a seed takes a single number in the JSON, with no second "fixedSeed"
        /// field that one would forget to set to <c>true</c>.
        ///
        /// <para>Setting a non-zero seed replays <b>exactly</b> the same apple sequence every game:
        /// that is the bench mode of §4.4, not a game mode. The seed actually used is logged at the
        /// start of every game, including when it comes from the clock — without that, a remarkable
        /// game would not be replayable.</para>
        /// </remarks>
        public long seed = ClockSeed;

        /// <summary>The <see cref="seed"/> value meaning "a fresh seed for every game".</summary>
        public const long ClockSeed = 0L;

        /// <summary>Display duration of the rejection pictogram (ART §5.5: 250 ms, by judgement).</summary>
        public double rejectionDisplaySeconds = 0.25;

        /// <summary>Continuous-extension cap of the pictogram (ART §5.5: 500 ms, by judgement).</summary>
        public double rejectionExtensionCapSeconds = 0.5;

        /// <summary>Display duration of the line of text on the pause screen (ART §5.5: 1.5 s).</summary>
        public double pauseTextSeconds = 1.5;

        /// <summary>
        /// Duration of the fade in and out of the rejection feedback.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>This value does not come from the brief.</b> ART §5.5 requires the feedback to "go
        /// out once" when it reaches its cap, and §5.7 speaks of "a single fade-in/fade-out envelope
        /// per trigger" — but no duration is given for those fades. 60 ms is set here <b>by the
        /// developer's judgement</b> (about half a tick at 8 ticks/s): long enough for the extinction
        /// forced by the cap to be visible, short enough not to delay reading the signal. To be
        /// ruled on by the game tester, like the other three durations.
        /// </remarks>
        public double rejectionFadeSeconds = 0.06;

        /// <summary>Master volume of the sound effects, from 0 (mute) to 1.</summary>
        /// <remarks>
        /// ⚠ <b>Zero is a legitimate value here</b>, unlike every duration above: a player who wants
        /// silence must be able to write 0 and get silence, not a value "corrected" back to the
        /// default. Hence <see cref="ValidateVolume"/> rather than <c>ValidateDouble</c>.
        ///
        /// <para>0.8 rather than 1: the sounds are mixed to sit under the game, not over it. It is
        /// the value to try first, and to retune from — that is what this file is for.</para>
        /// </remarks>
        public double sfxVolume = 0.8;

        /// <summary>Master volume of the menu music, from 0 (mute) to 1.</summary>
        /// <remarks>
        /// Lower than the effects on purpose: the loop plays under a screen one reads, and it must
        /// never be the reason a player turns the sound off. ⚠ Like <see cref="sfxVolume"/>, zero is
        /// an intent and is honoured; and like it, this setting has <b>no effect on the web</b>,
        /// where <c>SettingsLoader</c> reads nothing — which is why the in-game mute exists
        /// (<c>Audio/AudioMute.cs</c>).
        /// </remarks>
        public double musicVolume = 0.5;

        /// <summary>A set of values identical to the GDD constants.</summary>
        public static GameSettings Default()
        {
            return new GameSettings();
        }

        /// <summary>
        /// Returns a safe set of settings, and the list of what had to be corrected.
        /// </summary>
        /// <param name="issues">
        /// What was wrong, in plain words. ⚠ <b>Never silently empty</b>: a mute correction would
        /// give a player who edits their JSON, sees no change, and has no way to know why. The
        /// engine-side caller must log them.
        /// </param>
        /// <remarks>
        /// The correction rule is always the same: <b>fall back to the GDD default</b> rather than
        /// to some patched-up neighbouring value. A 20-column grid does not become 21 — it goes back
        /// to 21 × 15, because a partial correction would give a playfield nobody decided on.
        ///
        /// <para>The suggested 6–10 ticks/s range (§4.1) is reported but <b>not corrected</b>: it is
        /// design advice, and going outside it is precisely what we want to be able to try without
        /// recompiling.</para>
        /// </remarks>
        public GameSettings Validate(out IList<string> issues)
        {
            List<string> found = new List<string>();
            GameSettings safe = new GameSettings();

            safe.ticksPerSecond = ValidateDouble(
                ticksPerSecond, Cadence.DefaultTicksPerSecond, "ticksPerSecond", found);

            if (!Cadence.IsWithinSuggestedRange(safe.ticksPerSecond))
            {
                found.Add("ticksPerSecond = " + safe.ticksPerSecond + " is outside the suggested range "
                          + Cadence.SuggestedMinimumRate + "–" + Cadence.SuggestedMaximumRate
                          + " (GDD §4.1) — value kept, this is advice, not a bound.");
            }

            if (catchUpCap < 1)
            {
                found.Add("catchUpCap = " + catchUpCap
                          + ": a frame must be able to play at least one tick, otherwise the snake never moves. Falling back to "
                          + Cadence.DefaultCatchUpCap + ".");
                safe.catchUpCap = Cadence.DefaultCatchUpCap;
            }
            else
            {
                safe.catchUpCap = catchUpCap;
            }

            try
            {
                Grid attempt = new Grid(gridWidth, gridHeight);

                // ⚠ A second guard, and it is NOT redundant: GDD §4.3 only gives lower bounds
                // (width >= 5, height >= 3, imposed by the starting pose). The upper bound comes
                // from the frame: a 1001-column grid is perfectly valid for `Grid` and fits in no
                // screen — its cells would be under a pixel. Without this attempt, the game would
                // throw on first launch, inside the renderer, over a setting the logic had just
                // accepted.
                Board.CellSizeFor(attempt);

                safe.gridWidth = attempt.Width;
                safe.gridHeight = attempt.Height;
            }
            catch (ArgumentOutOfRangeException error)
            {
                found.Add("Grid " + gridWidth + " × " + gridHeight + " rejected (" + error.Message
                          + ") — falling back to " + Grid.DefaultWidth + " × " + Grid.DefaultHeight + ".");
                safe.gridWidth = Grid.DefaultWidth;
                safe.gridHeight = Grid.DefaultHeight;
            }

            if (queueDepth < 1)
            {
                found.Add("queueDepth = " + queueDepth
                          + ": the queue must hold at least one input. Falling back to "
                          + InputQueue.DefaultDepth + ".");
                safe.queueDepth = InputQueue.DefaultDepth;
            }
            else
            {
                safe.queueDepth = queueDepth;
            }

            // No seed value is invalid: the whole range of `long` names a legitimate sequence, and 0
            // names its absence. Nothing to correct, therefore nothing to report.
            safe.seed = seed;

            GameSettings defaults = new GameSettings();

            safe.rejectionDisplaySeconds = ValidateDouble(
                rejectionDisplaySeconds, defaults.rejectionDisplaySeconds, "rejectionDisplaySeconds", found);

            safe.rejectionExtensionCapSeconds = ValidateDouble(
                rejectionExtensionCapSeconds, defaults.rejectionExtensionCapSeconds, "rejectionExtensionCapSeconds", found);

            safe.pauseTextSeconds = ValidateDouble(
                pauseTextSeconds, defaults.pauseTextSeconds, "pauseTextSeconds", found);

            // ⚠ Strictly positive: a zero fade would make the extension cap useless. The extinction
            // it forces would have no duration, so nothing for the player to see, and the pictogram
            // would stay lit permanently under hammering (ART §5.7).
            safe.rejectionFadeSeconds = ValidateDouble(
                rejectionFadeSeconds, defaults.rejectionFadeSeconds, "rejectionFadeSeconds", found);

            safe.sfxVolume = ValidateVolume(sfxVolume, defaults.sfxVolume, "sfxVolume", found);
            safe.musicVolume = ValidateVolume(musicVolume, defaults.musicVolume, "musicVolume", found);

            // A cap shorter than the display duration would put the feedback out before it had been
            // read — the exact opposite of what ART §5.5 expects from it.
            if (safe.rejectionExtensionCapSeconds < safe.rejectionDisplaySeconds)
            {
                found.Add("rejectionExtensionCapSeconds (" + safe.rejectionExtensionCapSeconds
                          + ") is shorter than rejectionDisplaySeconds (" + safe.rejectionDisplaySeconds
                          + "): the feedback would go out before being read. Aligned with the display duration.");
                safe.rejectionExtensionCapSeconds = safe.rejectionDisplaySeconds;
            }

            // Two fades must fit inside the display duration, failing which the pictogram never
            // reaches full opacity: the player sees a flicker, not a sign.
            double maximumFade = safe.rejectionDisplaySeconds / 2.0;
            if (safe.rejectionFadeSeconds > maximumFade)
            {
                found.Add("rejectionFadeSeconds (" + safe.rejectionFadeSeconds
                          + ") does not fit twice inside rejectionDisplaySeconds ("
                          + safe.rejectionDisplaySeconds + "): the pictogram would never reach full opacity. Brought back to "
                          + maximumFade + ".");
                safe.rejectionFadeSeconds = maximumFade;
            }

            issues = found;
            return safe;
        }

        /// <summary>A duration must be a finite, strictly positive number, else we fall back to the default.</summary>
        private static double ValidateDouble(double value, double fallback, string name, ICollection<string> found)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
            {
                found.Add(name + " = " + value + " is not a finite, strictly positive number. Falling back to " + fallback + ".");
                return fallback;
            }

            return value;
        }

        /// <summary>A volume is a finite number in [0, 1]; out of range it is clamped, not refused.</summary>
        /// <remarks>
        /// ⚠ Clamped rather than sent back to the default, unlike the durations: someone writing 2
        /// wants the loudest sound the game has, and giving them 0.8 back would answer the opposite
        /// of their intent. Only a value that is not a number has no readable intent behind it.
        /// </remarks>
        private static double ValidateVolume(double value, double fallback, string name, ICollection<string> found)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                found.Add(name + " = " + value + " is not a finite number. Falling back to " + fallback + ".");
                return fallback;
            }

            if (value < 0.0)
            {
                found.Add(name + " = " + value + " is below 0. Clamped to 0 (silence).");
                return 0.0;
            }

            if (value > 1.0)
            {
                found.Add(name + " = " + value + " is above 1. Clamped to 1.");
                return 1.0;
            }

            return value;
        }
    }
}
