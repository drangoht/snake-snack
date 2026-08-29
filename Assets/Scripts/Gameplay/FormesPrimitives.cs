using System.Collections.Generic;
using UnityEngine;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// Les sprites du jeu, tous <b>générés en mémoire</b> : un carré blanc net, et des carrés à
    /// coins arrondis pour ce qui doit paraître vivant (<c>docs/art/cartoon.md</c> §3.1).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Aucun asset n'est importé, et c'est délibéré.</b> Un <c>.png</c> déposé dans
    /// <c>Assets/</c> n'obtient son GUID qu'au moment où l'éditeur l'importe : un build en batchmode
    /// lancé avant cet import ne le voit pas, et le jeu s'affiche sans texture <b>sans lever
    /// d'erreur</b>.
    ///
    /// <para>⚠ <b>C'est pourquoi le rounding du brief cartoon est dessiné ici, et non chargé depuis
    /// un PNG.</b> Le brief proposait <c>Resources/Formes/case-arrondie.png</c> en 9-slice, produit
    /// par un générateur Python et forcé en Sprite par le postprocessor. Écart assumé, pour trois
    /// raisons : la forme est triviale à décrire (une SDF de rectangle arrondi tient en cinq
    /// lignes) là où l'illustration du menu ne l'est pas ; le 9-slice imposerait de passer tout le
    /// rendu en <c>SpriteDrawMode.Sliced</c> alors que <c>VuePlateau</c> pose tout par
    /// <c>localScale</c> ; et surtout cela supprime d'un coup le <c>.meta</c>, le
    /// <c>spriteBorder</c> à poser à l'import et le risque de build batchmode ci-dessus. Le brief
    /// laissait explicitement le traitement à trancher à l'implémentation
    /// (<c>docs/art/cartoon.md</c> §7).</para>
    ///
    /// <para>⚠ <c>pixelsPerUnit</c> vaut toujours <b>le côté de la texture</b> : chaque sprite
    /// mesure donc exactement 1 unité, quelle que soit sa définition, et <c>localScale</c> continue
    /// de s'exprimer en pixels du cadre de référence 1280×720 — ce que
    /// <see cref="SnakeSnack.Rules.Plateau"/> suppose partout et que la caméra confirme avec
    /// <c>orthographicSize = 360</c>. Sans cette règle, passer le carré d'1 px à une forme de 128 px
    /// multiplierait par 128 la taille de tout ce qui est dessiné.</para>
    ///
    /// <para>Les textures blanches sont colorées par le <c>SpriteRenderer</c> depuis
    /// <see cref="SnakeSnack.UI.UiPalette"/> : aucune couleur n'est cuite dans un pixel, sinon un
    /// changement de palette n'aurait plus d'effet sur ces formes.</para>
    /// </remarks>
    public static class FormesPrimitives
    {
        /// <summary>
        /// Côté des textures arrondies, en pixels. 128 pour une case affichée à 42 px : assez de
        /// marge pour rester net si la taille de case augmente, assez petit pour être négligeable
        /// en mémoire (64 Ko par forme).
        /// </summary>
        private const int CoteTextureArrondie = 128;

        private static Sprite _carre;

        /// <summary>Les formes arrondies déjà produites, indexées par leur rayon relatif.</summary>
        private static readonly Dictionary<int, Sprite> _arrondis = new Dictionary<int, Sprite>();

        /// <summary>Carré blanc de 1 × 1 px, partagé par tout ce qui doit rester un aplat net.</summary>
        /// <remarks>
        /// ⚠ Reste en <see cref="FilterMode.Point"/> : la bordure et les traits de grille sont des
        /// <b>repères de mesure</b>, pas des personnages (<c>docs/art/cartoon.md</c> §6). Les lisser
        /// rendrait un trait d'1 px flou, donc plus difficile à compter à l'œil.
        /// </remarks>
        public static Sprite Carre()
        {
            // La comparaison à null passe par l'opérateur d'Unity : après un rechargement de domaine
            // ou un changement de scène, la référence est « fausse-nulle » et le sprite est recréé.
            if (_carre != null)
            {
                return _carre;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();

            _carre = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _carre.name = "CarreBlanc";
            return _carre;
        }

        /// <summary>
        /// Carré blanc à coins arrondis, lissé. Le corps et la tête du serpent, la pomme.
        /// </summary>
        /// <param name="rayonRelatif">
        /// Rayon des coins en fraction du côté : <c>0.28</c> pour le serpent — le même ratio que
        /// l'illustration du menu, pour que le personnage de l'affiche et celui de la partie se
        /// reconnaissent comme le même dessin — <c>0.18</c> pour la pomme.
        /// </param>
        public static Sprite CarreArrondi(float rayonRelatif)
        {
            // Clé entière : deux appels au même rayon doivent rendre LE MÊME sprite, sinon chaque
            // segment tirerait sa propre texture et le rendu perdrait son groupage par matériau.
            int cle = Mathf.RoundToInt(rayonRelatif * 1000f);

            Sprite existant;
            if (_arrondis.TryGetValue(cle, out existant) && existant != null)
            {
                return existant;
            }

            var sprite = ConstruireArrondi(rayonRelatif);
            _arrondis[cle] = sprite;
            return sprite;
        }

        /// <summary>
        /// Dessine le rectangle arrondi par sa distance signée, et tire l'anticrénelage de cette
        /// distance.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>L'alpha se calcule, il ne se suréchantillonne pas.</b> La distance au bord donne
        /// directement la couverture du pixel : un seul passage, aucune réduction, et un contour
        /// régulier au pixel près — là où un rendu binaire puis flouté laisserait des marches
        /// visibles sur les diagonales des coins.
        ///
        /// <para>⚠ Le RVB reste blanc <b>partout</b>, y compris là où l'alpha vaut zéro : une
        /// texture transparente noire produit un liseré sombre au filtrage bilinéaire, parce que
        /// l'interpolation mélange aussi les canaux de couleur des pixels invisibles.</para>
        /// </remarks>
        private static Sprite ConstruireArrondi(float rayonRelatif)
        {
            const int cote = CoteTextureArrondie;
            float rayon = Mathf.Clamp(rayonRelatif, 0f, 0.5f) * cote;
            float demi = cote / 2f;

            // Centre des arcs de coin : le carré intérieur dont les coins sont à `rayon` du bord.
            float interieur = demi - rayon;

            var texture = new Texture2D(cote, cote, TextureFormat.RGBA32, false);
            var pixels = new Color32[cote * cote];

            for (int y = 0; y < cote; y++)
            {
                for (int x = 0; x < cote; x++)
                {
                    // Coordonnées du CENTRE du pixel, ramenées au centre de la texture.
                    float dx = Mathf.Abs((x + 0.5f) - demi);
                    float dy = Mathf.Abs((y + 0.5f) - demi);

                    // Distance signée au rectangle arrondi : négative dedans, positive dehors.
                    float ecartX = Mathf.Max(dx - interieur, 0f);
                    float ecartY = Mathf.Max(dy - interieur, 0f);
                    float distance = Mathf.Sqrt((ecartX * ecartX) + (ecartY * ecartY)) - rayon;

                    // Couverture du pixel : plein à -0,5 px, vide à +0,5 px.
                    float couverture = Mathf.Clamp01(0.5f - distance);

                    pixels[(y * cote) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(couverture * 255f));
                }
            }

            texture.SetPixels32(pixels);

            // Bilinear : c'est lui qui donne le lissage une fois la forme redimensionnée à la case.
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();

            var sprite = Sprite.Create(
                texture, new Rect(0f, 0f, cote, cote), new Vector2(0.5f, 0.5f), cote);
            sprite.name = "CarreArrondi" + Mathf.RoundToInt(rayonRelatif * 100f);
            return sprite;
        }

        /// <summary>
        /// Pose un rectangle coloré, enfant de <paramref name="parent"/>, exprimé en pixels.
        /// </summary>
        public static SpriteRenderer Rectangle(Transform parent, string nom, Color couleur, int ordre)
        {
            return Poser(parent, nom, couleur, ordre, Carre());
        }

        /// <summary>Comme <see cref="Rectangle"/>, mais avec des coins arrondis et lissés.</summary>
        public static SpriteRenderer RectangleArrondi(
            Transform parent, string nom, Color couleur, int ordre, float rayonRelatif)
        {
            return Poser(parent, nom, couleur, ordre, CarreArrondi(rayonRelatif));
        }

        private static SpriteRenderer Poser(Transform parent, string nom, Color couleur, int ordre, Sprite sprite)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);

            var rendu = go.AddComponent<SpriteRenderer>();
            rendu.sprite = sprite;
            rendu.color = couleur;
            rendu.sortingOrder = ordre;
            return rendu;
        }
    }
}
