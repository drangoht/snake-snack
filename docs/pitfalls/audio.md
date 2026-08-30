# Pitfalls — Audio


**⚠ An entry missing from the lookup table is SILENT.** [inherited] Fourteen weapons were, with
nothing to say so. Write an audit that compares the content list with the sound table.

**⚠ The browser lets no sound start before a user gesture.** Unity opens its audio context suspended:
without the wake-up placed in the WebGL template, music only starts on a chance click.

**⚠ A `PlayOneShot` log proves an intention, not a sound.** To prove audio comes out of the mixer:
`AudioListener.GetOutputData(buffer, 0)` and log the RMS.
