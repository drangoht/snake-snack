# Pitfalls — Audio


**⚠ An entry missing from the lookup table is SILENT.** [inherited] Fourteen weapons were, with
nothing to say so. Write an audit that compares the content list with the sound table.

**⚠ The browser lets no sound start before a user gesture.** Unity opens its audio context suspended:
without the wake-up placed in the WebGL template, music only starts on a chance click.

**⚠ A `PlayOneShot` log proves an intention, not a sound.** To prove audio comes out of the mixer:
`AudioListener.GetOutputData(buffer, 0)` and log the RMS.

**⚠⚠ A scene built from code has NO `AudioListener` unless somebody adds one.** Found on 2026-08-31
in our own `SceneBuilder.BuildCamera`, which had been creating the camera without one since the
project started. Every clip loads, every `PlayOneShot` returns, the mixer has nobody to deliver to,
and Unity says so once, in a warning drowned among the startup lines. It cost nothing only because
there was no sound yet. **Check for the listener FIRST**: its absence makes every other audio check
moot.

**⚠ `AudioImporter.preloadAudioData` is obsolete** (Unity 6): preloading moved into
`AudioImporterSampleSettings`, per platform. This project treats warnings as errors, so the old form
does not even compile — which is the good case. `AudioClipLoadType` and `AudioCompressionFormat` live
in `UnityEngine`, not `UnityEditor`: an import script with only `using UnityEditor;` fails to find
them, with an error that names the type rather than the missing using.

**⚠⚠ A `Streaming` clip with `preloadAudioData = false` never loads on its own, and `Play()` on it
does nothing.** No false return, no log, no exception. `Resources.Load` hands back a perfectly valid
`AudioClip` whose `loadState` stays `Unloaded` for good, `AudioSource.isPlaying` can even report
`true`, and not a sample leaves the mixer. Ask for `clip.LoadAudioData()` explicitly and **wait**:
the call is asynchronous, so playing on the next line reproduces the same silence. Measured both
ways on 2026-08-31 — RMS `0.12060` with the explicit load, `0.00000` without, everything else
identical.

**⚠ These import settings are the opposite of the effects', and the folder decides which apply.**
Music wants Streaming Vorbis; a 200 ms effect wants decompressed PCM. `ImportAudio` therefore tests
`Resources/Audio/Music/` **before** `Resources/Audio/`, which is its prefix — the other order applies
the effects' settings to the music without a word.

**⚠ A measurement taken too early proves nothing, and looks exactly like a defect.** The music's RMS
read at 0.5 s came back at zero while the streamed clip was merely still loading; the first
diagnosis blamed the wrong thing. Measure the music on its own timer, well after startup, and never
read a "before" sample as a verdict on something that starts asynchronously.
