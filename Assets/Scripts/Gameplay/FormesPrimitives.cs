using UnityEngine;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// Le seul sprite du jeu : un carré blanc d'un pixel, étiré par l'échelle de chaque objet.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Aucun asset n'est importé, et c'est délibéré.</b> Un <c>.png</c> déposé dans
    /// <c>Assets/</c> n'obtient son GUID qu'au moment où l'éditeur l'importe : un build en batchmode
    /// lancé avant cet import ne le voit pas, et le jeu s'affiche sans texture <b>sans lever
    /// d'erreur</b>. Un carré blanc coloré par le <c>SpriteRenderer</c> suffit à tout ce que le jeu
    /// dessine aujourd'hui — aire, traits de grille, segments, chevron barré.
    ///
    /// <para>⚠ <c>pixelsPerUnit = 1</c> : une unité monde vaut alors exactement un pixel du cadre de
    /// référence 1280×720, ce que <see cref="SnakeSnack.Rules.Plateau"/> suppose partout et que la
    /// caméra confirme avec <c>orthographicSize = 360</c>. Toute autre valeur décalerait chaque
    /// position du GDD §4.3 sans que rien ne le signale.</para>
    /// </remarks>
    public static class FormesPrimitives
    {
        private static Sprite _carre;

        /// <summary>Carré blanc de 1 × 1 px, partagé par tout le rendu.</summary>
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
        /// Pose un rectangle coloré, enfant de <paramref name="parent"/>, exprimé en pixels.
        /// </summary>
        public static SpriteRenderer Rectangle(Transform parent, string nom, Color couleur, int ordre)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);

            var rendu = go.AddComponent<SpriteRenderer>();
            rendu.sprite = Carre();
            rendu.color = couleur;
            rendu.sortingOrder = ordre;
            return rendu;
        }
    }
}
