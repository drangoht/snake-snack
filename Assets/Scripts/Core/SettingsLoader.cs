using System.Collections.Generic;
using System.IO;
using SnakeSnack.Rules;
using UnityEngine;

namespace SnakeSnack.Core
{
    /// <summary>
    /// Reads <c>StreamingAssets/settings.json</c> — the tuning that is settable <b>without
    /// recompiling</b> (CLAUDE.md, GDD §4.1 and §4.3).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A missing or unreadable file is never a blocking error.</b> The game must start on the
    /// GDD values whatever happens: refusing to launch because a convenience file is absent would
    /// turn a setting into a hard dependency.
    ///
    /// <para>⚠ <b>On WebGL this loader reads nothing</b> and returns the defaults:
    /// <c>Application.streamingAssetsPath</c> is a <i>URL</i> there, not a file path, and
    /// <c>File.Exists</c> answers false without raising anything. Tuning the game online would need
    /// an asynchronous <c>UnityWebRequest</c> — not done, and beside the point as long as tuning
    /// happens on the desktop build.</para>
    /// </remarks>
    public static class SettingsLoader
    {
        /// <summary>Name of the tuning file, inside <c>Assets/StreamingAssets/</c>.</summary>
        public const string FileName = "settings.json";

        /// <summary>Loads, validates and logs. Always returns a usable set of settings.</summary>
        public static GameSettings Load()
        {
            GameSettings read = Read();

            IList<string> issues;
            GameSettings validated = read.Validate(out issues);

            // ⚠ Never corrected in silence: otherwise the player edits their JSON, sees nothing
            // change, and has no way to know their value was refused.
            for (int i = 0; i < issues.Count; i++)
            {
                Debug.LogWarning("[settings] " + issues[i]);
            }

            return validated;
        }

        private static GameSettings Read()
        {
            string path = Path.Combine(Application.streamingAssetsPath, FileName);

            if (!File.Exists(path))
            {
                Debug.Log("[settings] " + FileName + " missing: falling back to the GDD defaults.");
                return GameSettings.Default();
            }

            try
            {
                string json = File.ReadAllText(path);
                GameSettings read = JsonUtility.FromJson<GameSettings>(json);

                if (read == null)
                {
                    Debug.LogWarning("[settings] " + FileName + " unreadable: falling back to defaults.");
                    return GameSettings.Default();
                }

                Debug.Log("[settings] loaded from " + path);
                return read;
            }
            catch (IOException error)
            {
                Debug.LogWarning("[settings] could not read (" + error.Message + "): falling back to defaults.");
                return GameSettings.Default();
            }
        }
    }
}
