using System.Collections.Generic;
using UnityEngine;

namespace SnakeSnack.Audio
{
    /// <summary>
    /// Plays the game's sound effects, and says out loud what it cannot play.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Audio fails silently by construction</b>, which is what makes it expensive to debug: a
    /// missing clip, a missing listener, a suspended context and a volume at zero all produce the
    /// same thing — nothing — and none of them raises an error. This class therefore audits
    /// <b>once</b>, at startup, and names each cause separately.
    ///
    /// <para>The audit is not a nicety: <c>docs/pitfalls/audio.md</c> opens on fourteen weapons that
    /// were silent in another project, with nothing to say so.</para>
    ///
    /// <para>⚠ Nothing is logged when <see cref="Play"/> is called: the bite fires up to eight times
    /// a second, and a per-call log would bury the startup audit under its own noise.</para>
    /// </remarks>
    public sealed class SfxPlayer : MonoBehaviour
    {
        private readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();

        private AudioSource _source;
        private float _masterVolume = 1f;

        /// <summary>Loads every declared sound, then reports what is missing.</summary>
        /// <param name="masterVolume">From <c>settings.json</c>; 0 is a legitimate value.</param>
        public void Build(double masterVolume)
        {
            _masterVolume = (float)masterVolume;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            // The per-sound volume is passed to PlayOneShot, where it multiplies this one. Keeping
            // the source at 1 leaves exactly one place where the master volume applies.
            _source.volume = 1f;

            // ⚠ 2D, explicitly: an AudioSource defaults to spatialBlend = 0 today, but a sound
            // panned because it sits at x = -300 in a 2D game is the kind of defect one looks for
            // in the mixer for an hour.
            _source.spatialBlend = 0f;

            Load();
            Audit();
        }

        /// <summary>Plays a sound, or does nothing if it has none — the audit has already said so.</summary>
        public void Play(Sfx sound)
        {
            if (_source == null || _masterVolume <= 0f)
            {
                return;
            }

            AudioClip clip;
            if (!_clips.TryGetValue(sound, out clip) || clip == null)
            {
                return;
            }

            _source.PlayOneShot(clip, SfxCatalog.Volume(sound) * _masterVolume);
        }

        /// <summary>
        /// The RMS of what actually leaves the mixer — the only proof that a sound came out.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>A <c>PlayOneShot</c> log proves an intention, not a sound</b>
        /// (<c>docs/pitfalls/audio.md</c>). This reads the output buffer of the listener, downstream
        /// of the volume, the listener and the audio context: it is what a test must assert on.
        /// Returns 0 when there is no listener, which is precisely one of the cases to catch.
        /// </remarks>
        public static float OutputRms(int sampleCount = 1024)
        {
            float[] buffer = new float[sampleCount];
            AudioListener.GetOutputData(buffer, 0);

            double sum = 0.0;
            for (int i = 0; i < buffer.Length; i++)
            {
                sum += buffer[i] * buffer[i];
            }

            return Mathf.Sqrt((float)(sum / buffer.Length));
        }

        private void Load()
        {
            Sfx[] all = SfxCatalog.All();

            for (int i = 0; i < all.Length; i++)
            {
                string file = SfxCatalog.FileName(all[i]);
                if (file == null)
                {
                    continue;
                }

                _clips[all[i]] = Resources.Load<AudioClip>(SfxCatalog.ResourceFolder + file);
            }
        }

        /// <summary>Turns every possible silence into a sentence naming its cause.</summary>
        private void Audit()
        {
            // ⚠ Checked FIRST, because it makes every other check moot: with no listener in the
            // scene, every clip loads, every Play succeeds, and not one sound comes out. Unity logs
            // a warning about it, drowned among the startup lines — this one names the fix.
            if (Object.FindFirstObjectByType<AudioListener>() == null)
            {
                Debug.LogError("[audio] No AudioListener in the scene: NOTHING will be heard, whatever "
                               + "the clips and the volume say. It is added to the camera by "
                               + "SceneBuilder.BuildCamera.");
            }

            Sfx[] all = SfxCatalog.All();
            List<string> missing = new List<string>();
            List<string> unbound = new List<string>();

            for (int i = 0; i < all.Length; i++)
            {
                string file = SfxCatalog.FileName(all[i]);

                if (file == null)
                {
                    unbound.Add(all[i].ToString());
                    continue;
                }

                AudioClip clip;
                if (!_clips.TryGetValue(all[i], out clip) || clip == null)
                {
                    missing.Add(all[i] + " → Assets/Resources/" + SfxCatalog.ResourceFolder + file + ".wav");
                }
            }

            if (unbound.Count > 0)
            {
                Debug.LogError("[audio] Declared in the Sfx enum but bound to no file in SfxCatalog: "
                               + string.Join(", ", unbound) + ". These moments are silent.");
            }

            if (missing.Count > 0)
            {
                Debug.LogError("[audio] Clip file not found — these moments are silent: "
                               + string.Join(" · ", missing));
            }

            if (_masterVolume <= 0f)
            {
                Debug.Log("[audio] sfxVolume is 0 in settings.json: the game is deliberately mute.");
            }

            if (unbound.Count == 0 && missing.Count == 0)
            {
                Debug.Log("[audio] " + all.Length + " sounds loaded.");
            }
        }
    }
}
