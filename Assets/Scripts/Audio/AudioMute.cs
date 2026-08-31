using System;
using UnityEngine;

namespace SnakeSnack.Audio
{
    /// <summary>
    /// Whether the player asked for silence, and the memory of that choice between two sessions.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This exists because <c>settings.json</c> does not work on the web.</b>
    /// <c>SettingsLoader</c> reads nothing under WebGL — <c>streamingAssetsPath</c> is a URL there —
    /// so <c>sfxVolume</c> has no effect on itch.io, which is the only channel the game is published
    /// on. Without an in-game toggle, a visitor who wants silence has exactly one way to get it:
    /// close the tab.
    ///
    /// <para>It mutes <b>everything</b>, music and effects alike, rather than the music only: a
    /// player reaching for the mute is trying to silence a tab, not to balance a mix.</para>
    ///
    /// <para>⚠ Best effort, like <c>PersistentBest</c>, and for the same reasons: web storage is
    /// tied to the origin and can vanish or come back damaged. An unreadable preference means "not
    /// muted" and the game starts — it must never refuse to launch over a boolean. <c>Save()</c> is
    /// explicit, otherwise a closed tab forgets the choice, which is precisely when it matters.</para>
    /// </remarks>
    public static class AudioMute
    {
        /// <summary>
        /// Storage key. ⚠ Named and stable: changing it silently resets every player's choice —
        /// their preference would still exist, under the old name.
        /// </summary>
        public const string Key = "snakesnack.muted";

        private static bool _muted;
        private static bool _loaded;

        /// <summary>True when the player has asked for silence.</summary>
        public static bool Muted
        {
            get
            {
                if (!_loaded)
                {
                    _muted = Read();
                    _loaded = true;
                }

                return _muted;
            }
        }

        /// <summary>Raised when the choice changes, so the players and the button can follow.</summary>
        public static event Action Changed;

        /// <summary>Flips the choice and remembers it.</summary>
        public static void Toggle()
        {
            Set(!Muted);
        }

        /// <summary>Sets the choice and remembers it.</summary>
        public static void Set(bool muted)
        {
            _muted = muted;
            _loaded = true;

            try
            {
                PlayerPrefs.SetInt(Key, muted ? 1 : 0);
                PlayerPrefs.Save();
            }
            catch (Exception error)
            {
                // Unavailable storage must not interrupt anything: the choice holds for this
                // session and is simply forgotten by the next one.
                Debug.LogWarning("[audio] could not remember the mute choice: " + error.Message);
            }

            if (Changed != null)
            {
                Changed();
            }
        }

        private static bool Read()
        {
            try
            {
                return PlayerPrefs.GetInt(Key, 0) != 0;
            }
            catch (Exception error)
            {
                Debug.LogWarning("[audio] could not read the mute choice, starting unmuted: " + error.Message);
                return false;
            }
        }
    }
}
