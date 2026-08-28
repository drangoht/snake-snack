using UnityEditor;
using UnityEngine;

namespace SnakeSnack.EditorTools
{
    /// <summary>
    /// Impose les réglages d'import des illustrations de <c>Assets/Resources/Illustrations/</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Sans cette classe, l'illustration du menu ne s'afficherait pas — sans la moindre
    /// erreur.</b> <c>ProjectSettings/EditorSettings.asset</c> porte <c>m_DefaultBehaviorMode: 0</c>
    /// (mode 3D) : un <c>.png</c> importé y devient une <b>texture</b>, pas un sprite.
    /// <c>Resources.Load&lt;Sprite&gt;</c> rend alors <c>null</c>, l'<c>Image</c> du menu reste vide,
    /// et rien n'est journalisé. C'est exactement le piège de <c>docs/pitfalls/assets-import.md</c>,
    /// aggravé par le fait que le PNG est produit par un script et importé en batchmode : personne
    /// n'ouvre l'inspecteur pour s'apercevoir du réglage.
    ///
    /// <para>Un <c>AssetPostprocessor</c> plutôt qu'un <c>.meta</c> écrit à la main : le
    /// <c>.meta</c> serait réécrit au premier réimport et la règle ne survivrait pas à la
    /// régénération du PNG. Ici, la règle est dans le dépôt, en clair, et s'applique à toute
    /// illustration future.</para>
    ///
    /// <para>⚠ Ne concerne QUE <c>Resources/Illustrations/</c>. Le reste des assets garde les
    /// réglages par défaut du projet — une règle d'import globale se paie sur des fichiers qu'on
    /// n'avait pas en tête au moment de l'écrire.</para>
    /// </remarks>
    public sealed class ImportIllustrations : AssetPostprocessor
    {
        private const string Dossier = "Assets/Resources/Illustrations/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dossier, System.StringComparison.Ordinal))
            {
                return;
            }

            var importateur = (TextureImporter)assetImporter;

            importateur.textureType = TextureImporterType.Sprite;
            importateur.spriteImportMode = SpriteImportMode.Single;

            // ⚠ 1 pixel par unité, comme le carré blanc de FormesPrimitives : une unité monde vaut
            // exactement un pixel du cadre 1280x720 dans tout le projet. L'illustration du menu est
            // posée dans un Canvas, où ce réglage n'a aucun effet — mais une illustration posée un
            // jour dans la scène s'afficherait, avec la valeur par défaut de 100, cent fois trop
            // petite, et le premier réflexe serait de corriger l'échelle au lieu de l'import.
            importateur.spritePixelsPerUnit = 1f;

            importateur.alphaIsTransparency = true;
            importateur.mipmapEnabled = false;
            importateur.wrapMode = TextureWrapMode.Clamp;

            // Bilinéaire, contrairement au carré blanc du rendu (qui est en Point) : l'illustration
            // est dessinée avec des bords anticrénelés par le générateur Python, et un filtrage en
            // Point les rendrait crénelés dès que la page itch redimensionne le cadre.
            importateur.filterMode = FilterMode.Bilinear;

            // Non compressée : le dessin est fait d'aplats de la palette, et DXT5 fait baver les
            // bords entre deux aplats voisins. 512x512 en RGBA32 pèse 1 Mo dans le binaire, ce qui
            // reste sans commune mesure avec le reste du build web.
            importateur.textureCompression = TextureImporterCompression.Uncompressed;
            importateur.maxTextureSize = 1024;
        }
    }
}
