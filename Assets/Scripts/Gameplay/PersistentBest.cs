using System;
using SnakeSnack.Rules;
using UnityEngine;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// The best score survives closing the game (GDD §4.5). A storage adapter, nothing else.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Best effort, never blocking.</b> On WebGL, storage is tied to the site origin and can
    /// disappear (private browsing, browser purge); it can also come back damaged, or carry a value
    /// written by something else under the same key. In all those cases the best score restarts from
    /// zero and the game starts: it must <b>never</b> refuse to launch over a counter.
    ///
    /// <para>⚠ <b><see cref="PlayerPrefs.Save"/> is called explicitly</b> on every write. Without it,
    /// the value only lives in memory until a clean exit — meaning a tab closed mid-game, exactly the
    /// case §4.5 wants to cover, would lose the best score. The cost is acceptable because we only
    /// write on the ticks where the best score really changes (<see cref="Score.CountApple"/> signals
    /// it), not on every apple.</para>
    ///
    /// <para>This class lives in <c>Gameplay/</c> and not in <c>Rules/</c>: it touches the engine, so
    /// it is not testable outside Unity. Everything that gets decided — normalising a damaged best
    /// score, comparing, the "best beaten" predicate — belongs to <see cref="Score"/>.</para>
    /// </remarks>
    public static class PersistentBest
    {
        /// <summary>
        /// Storage key. ⚠ <b>Named and stable</b>: changing it would reset every player to zero
        /// without a single test failing — their best score would still exist, under the old name.
        /// It stays in its original spelling for that exact reason, even though the rest of the code
        /// has been translated.
        /// </summary>
        public const string Key = "snakesnack.record";

        /// <summary>The known best score, or zero if it is missing, unreadable or damaged.</summary>
        public static int Read()
        {
            try
            {
                return Score.NormaliseBest(PlayerPrefs.GetInt(Key, 0));
            }
            catch (Exception error)
            {
                // The key exists but carries something other than an integer: PlayerPrefs throws. We
                // restart from zero, log it, and the game starts anyway.
                Debug.LogWarning("[best] could not read, restarting from zero: " + error.Message);
                return 0;
            }
        }

        /// <summary>Writes the best score and pushes it to disk straight away.</summary>
        public static void Write(int best)
        {
            try
            {
                PlayerPrefs.SetInt(Key, Score.NormaliseBest(best));
                PlayerPrefs.Save();
            }
            catch (Exception error)
            {
                // Unavailable storage (private browsing, quota) must not interrupt the current game:
                // the best score will be whatever it is next session.
                Debug.LogWarning("[best] could not write: " + error.Message);
            }
        }
    }
}
