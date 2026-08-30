using UnityEditor;
using UnityEngine;

namespace SnakeSnack.EditorTools
{
    /// <summary>
    /// Forces the import settings of the illustrations in <c>Assets/Resources/Illustrations/</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Without this class, the menu illustration would not display — with no error at all.</b>
    /// <c>ProjectSettings/EditorSettings.asset</c> carries <c>m_DefaultBehaviorMode: 0</c> (3D mode):
    /// a <c>.png</c> imported there becomes a <b>texture</b>, not a sprite.
    /// <c>Resources.Load&lt;Sprite&gt;</c> then returns <c>null</c>, the menu's <c>Image</c> stays
    /// empty, and nothing is logged. That is exactly the trap of
    /// <c>docs/pitfalls/assets-import.md</c>, made worse by the fact that the PNG is produced by a
    /// script and imported in batchmode: nobody opens the inspector to notice the setting.
    ///
    /// <para>An <c>AssetPostprocessor</c> rather than a hand-written <c>.meta</c>: the <c>.meta</c>
    /// would be rewritten on the first reimport and the rule would not survive the PNG being
    /// regenerated. Here the rule is in the repository, in plain sight, and applies to every future
    /// illustration.</para>
    ///
    /// <para>⚠ Concerns ONLY <c>Resources/Illustrations/</c>. The rest of the assets keep the
    /// project's default settings — a global import rule gets paid for on files you did not have in
    /// mind when you wrote it.</para>
    /// </remarks>
    public sealed class ImportIllustrations : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/Illustrations/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder, System.StringComparison.Ordinal))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;

            // ⚠ 1 pixel per unit, like the white square of PrimitiveShapes: one world unit is exactly
            // one pixel of the 1280x720 frame throughout the project. The menu illustration sits in a
            // Canvas, where this setting has no effect — but an illustration placed one day in the
            // scene would display, with the default of 100, a hundred times too small, and the first
            // reflex would be to fix the scale rather than the import.
            importer.spritePixelsPerUnit = 1f;

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;

            // Bilinear, unlike the renderer's white square (which is Point): the illustration is drawn
            // with antialiased edges by the Python generator, and Point filtering would make them
            // jagged as soon as the itch page resizes the frame.
            importer.filterMode = FilterMode.Bilinear;

            // Uncompressed: the drawing is made of flat palette areas, and DXT5 bleeds the edges
            // between two neighbouring areas. 512x512 in RGBA32 weighs 1 MB in the binary, which is
            // nothing next to the rest of the web build.
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
        }
    }
}
