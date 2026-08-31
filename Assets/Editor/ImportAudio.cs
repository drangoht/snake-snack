using UnityEditor;
using UnityEngine;

namespace SnakeSnack.EditorTools
{
    /// <summary>
    /// Forces the import settings of the clips in <c>Assets/Resources/Audio/</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ Same reasoning as <see cref="ImportIllustrations"/>, and the same trap: the default import
    /// of an audio file is decided by the project, not by us, and a wrong setting here does not
    /// raise an error — it just delays or swallows a sound.
    ///
    /// <para>⚠ <b><c>DecompressOnLoad</c> matters most on the web build.</b> A short effect left in
    /// <c>CompressedInMemory</c> is decoded at the moment it is played: on WebGL that decode lands
    /// in the same frame as the bite, and the sound arrives after the thing it was meant to
    /// underline. These clips are a few tens of kilobytes — holding them decompressed costs
    /// nothing next to being late.</para>
    ///
    /// <para>⚠ <b>Mono, deliberately.</b> Nothing in this game is positioned in space: a stereo
    /// clip would double the memory to say the same thing twice.</para>
    ///
    /// <para>⚠ Concerns ONLY <c>Resources/Audio/</c> — a global rule gets paid for on files you did
    /// not have in mind when you wrote it.</para>
    /// </remarks>
    public sealed class ImportAudio : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/Audio/";

        /// <summary>
        /// Music, which takes the opposite settings from the effects.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Tested BEFORE <see cref="Folder"/>, which is its prefix.</b> Checking the effects
        /// folder first would match the music too, and silently apply to a 34-second loop the
        /// settings meant for a 200 ms click.
        /// </remarks>
        private const string MusicFolder = "Assets/Resources/Audio/Music/";

        private void OnPreprocessAudio()
        {
            bool music = assetPath.StartsWith(MusicFolder, System.StringComparison.Ordinal);

            if (!music && !assetPath.StartsWith(Folder, System.StringComparison.Ordinal))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;

            importer.forceToMono = true;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            if (music)
            {
                // ⚠ Everything the effects want is wrong here. Decompressing 34 seconds into memory
                // costs several megabytes to play one loop nobody hears twice at once, and PCM
                // would put the whole thing raw into the web build — for a file the player streams
                // from beginning to end anyway. Streaming Vorbis is the opposite trade, and it is
                // the right one at this length.
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                settings.preloadAudioData = false;

                importer.defaultSampleSettings = settings;
                return;
            }

            settings.loadType = AudioClipLoadType.DecompressOnLoad;

            // Loaded with the scene rather than on the first play: the first bite of the first game
            // must sound like the others.
            // ⚠ On the sample settings, not on the importer: `AudioImporter.preloadAudioData` still
            // compiles but is obsolete — and this project treats warnings as errors, so it does not
            // even compile. Preloading is a per-platform setting now.
            settings.preloadAudioData = true;

            // No compression on top: these are short sounds, and Vorbis on a 200 ms clip adds a
            // decode for a saving measured in kilobytes.
            settings.compressionFormat = AudioCompressionFormat.PCM;

            importer.defaultSampleSettings = settings;
        }
    }
}
