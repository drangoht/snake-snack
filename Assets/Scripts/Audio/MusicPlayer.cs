using System.Collections;
using UnityEngine;

namespace SnakeSnack.Audio
{
    /// <summary>
    /// The menu loop (<c>docs/gdd/audio.md</c> §4.7). Plays on the menu, and nowhere else.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The menu sings, the game is silent</b> — a design decision, not an omission. A game
    /// lasts from thirty seconds to three minutes and restarts with no transition (GDD §2): a short
    /// loop would run dozens of times in a row, and the four effects would lose the silence they
    /// stand out against.
    ///
    /// <para>⚠ The clip is imported <b>Streaming Vorbis</b>, unlike the effects
    /// (<c>Assets/Editor/ImportAudio.cs</c>): decompressing thirty-four seconds into memory to play
    /// one loop is the wrong trade at this length. That import is also what makes the loading dance
    /// below necessary.</para>
    /// </remarks>
    public sealed class MusicPlayer : MonoBehaviour
    {
        /// <summary>Where the loop is loaded from, under <c>Assets/Resources/</c>.</summary>
        public const string MenuTrack = "Audio/Music/menu";

        private AudioSource _source;
        private float _volume;
        private bool _menuOpen;
        private bool _reported;

        /// <summary>Loads the loop and reports it if it is not there.</summary>
        /// <param name="volume">Master music volume, from <c>settings.json</c>.</param>
        public void Build(double volume)
        {
            _volume = (float)volume;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = _volume;

            AudioClip clip = Resources.Load<AudioClip>(MenuTrack);
            if (clip == null)
            {
                Debug.LogError("[audio] Menu music not found: Assets/Resources/" + MenuTrack
                               + " (.ogg). The menu will be silent, and nothing else says so.");
                return;
            }

            _source.clip = clip;

            AudioMute.Changed += ApplyMute;
            ApplyVolume();
        }

        private void OnDestroy()
        {
            // ⚠ A static event outlives the object subscribed to it: without this, a destroyed
            // MusicPlayer keeps being called and Unity raises a MissingReferenceException at a
            // moment that has nothing to do with the cause.
            AudioMute.Changed -= ApplyMute;
        }

        /// <summary>Told by the game: the menu has the screen, or it does not.</summary>
        public void SetMenuOpen(bool open)
        {
            _menuOpen = open;

            if (open)
            {
                PlayMenu();
            }
            else
            {
                StopMenu();
            }
        }

        /// <summary>The menu takes the screen: the loop starts, once it can.</summary>
        public void PlayMenu()
        {
            if (_source == null || _source.clip == null || AudioMute.Muted || _source.isPlaying)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(PlayWhenLoaded());
        }

        /// <summary>
        /// Waits for the streamed clip to be loaded, then plays it.
        /// </summary>
        /// <remarks>
        /// ⚠⚠ <b>A <c>Streaming</c> clip with <c>preloadAudioData = false</c> NEVER loads on its
        /// own, and <c>Play()</c> on it does nothing</b> — no false return, no log, no exception.
        /// <c>Resources.Load</c> hands back a perfectly valid <c>AudioClip</c> whose
        /// <c>loadState</c> stays <c>Unloaded</c> for good.
        ///
        /// <para><b>Measured both ways on 2026-08-31</b>, because the first diagnosis was wrong and
        /// a plausible story is not a cause: with the explicit load, menu music RMS <c>0.12060</c>;
        /// with that one line disabled and everything else identical, <c>loadState</c> still
        /// <c>Unloaded</c> after 2.5 s and RMS <c>0.00000</c>. The RMS is what settles it — it is
        /// the only thing that looks downstream of the intention.</para>
        ///
        /// <para><c>LoadAudioData()</c> is <b>asynchronous</b>: calling it and playing on the next
        /// line would reproduce the same silence. Hence the wait.</para>
        /// </remarks>
        private IEnumerator PlayWhenLoaded()
        {
            AudioClip clip = _source.clip;

            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }

            while (clip.loadState == AudioDataLoadState.Loading)
            {
                yield return null;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogError("[audio] Menu music could not be loaded (state " + clip.loadState
                               + "): the menu is silent.");
                yield break;
            }

            // Re-checked after the wait: the player may have muted, or started a game, while the
            // clip was loading.
            if (AudioMute.Muted || !_menuOpen)
            {
                yield break;
            }

            _source.Play();

            if (!_reported)
            {
                _reported = true;
                Debug.Log("[audio] menu music: " + clip.name + ", " + clip.length.ToString("F1")
                          + " s, playing " + _source.isPlaying);
            }
        }

        /// <summary>A game starts: the loop stops, and starts over from the top next time.</summary>
        /// <remarks>
        /// Stopped rather than paused: the menu is a place one comes back to, not a track one
        /// resumes mid-phrase.
        /// </remarks>
        public void StopMenu()
        {
            StopAllCoroutines();

            if (_source != null)
            {
                _source.Stop();
            }
        }

        private void ApplyMute()
        {
            ApplyVolume();

            if (AudioMute.Muted)
            {
                // Volume rather than Stop: coming back from the mute in the middle of the menu
                // should not restart the loop from the beginning.
                return;
            }

            if (_menuOpen)
            {
                PlayMenu();
            }
        }

        private void ApplyVolume()
        {
            if (_source != null)
            {
                _source.volume = AudioMute.Muted ? 0f : _volume;
            }
        }
    }
}
