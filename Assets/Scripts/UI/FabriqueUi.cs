using UnityEngine;
using UnityEngine.UI;

namespace SnakeSnack.UI
{
    /// <summary>
    /// Fabrique les briques d'interface (un <c>Text</c>, une <c>Image</c>) avec les réglages que
    /// tout écran du jeu doit partager.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Toute l'interface est construite par code</b>, jamais assemblée dans la scène : une
    /// référence sérialisée vers un <c>Text</c> renommé se retrouve nulle <b>dans la scène
    /// régénérée</b> (<c>SceneBuilder</c>), et le seul symptôme est un texte qui n'apparaît pas.
    /// Cette fabrique existe pour que les écrans se ressemblent sans se recopier : le jour où le
    /// débordement ou le raycast changent, ils changent en un endroit.
    /// </remarks>
    public static class FabriqueUi
    {
        /// <summary>Un texte posé par ancre, position et dimensions, en pixels du cadre 1280x720.</summary>
        /// <remarks>
        /// ⚠ <b>Jamais de <c>FontStyle.Bold</c></b> : la graisse vient du FICHIER (SemiBold ou
        /// ExtraBold). Le gras synthétique d'uGUI s'ajouterait au dessin déjà gras et boucherait les
        /// contre-formes rondes de Nunito — exactement ce que l'ART §2.4 interdit.
        /// </remarks>
        public static Text Texte(
            Transform parent, string nom, Font police, int taille, TextAnchor alignement,
            Color couleur, Vector2 ancre, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);

            var texte = go.AddComponent<Text>();
            texte.font = police;
            texte.fontStyle = FontStyle.Normal;
            texte.fontSize = taille;
            texte.alignment = alignement;
            texte.color = couleur;
            texte.raycastTarget = false;
            texte.horizontalOverflow = HorizontalWrapMode.Overflow;
            texte.verticalOverflow = VerticalWrapMode.Overflow;

            Poser(go.GetComponent<RectTransform>(), ancre, position, dimensions);
            return texte;
        }

        /// <summary>Un rectangle plein, posé de la même façon qu'un texte.</summary>
        public static Image Rectangle(
            Transform parent, string nom, Color couleur, Vector2 ancre, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = couleur;
            image.raycastTarget = false;

            Poser(go.GetComponent<RectTransform>(), ancre, position, dimensions);
            return image;
        }

        /// <summary>Une image étirée sur tout son parent (voile, fond d'écran).</summary>
        public static Image Voile(Transform parent, string nom, Color couleur)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = couleur;
            image.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        /// <summary>
        /// Un canevas en surimpression, à l'ordre de tri donné.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>L'ordre de tri est le seul arbitre</b> (<c>docs/pitfalls/interface.md</c>) : deux
        /// canevas au même ordre s'empilent selon la hiérarchie, qui n'est pas stable quand la scène
        /// est régénérée par code. Chaque canevas du jeu en pose un explicitement — tampon de build
        /// 1000, menu 200, HUD 100.
        /// </remarks>
        public static Canvas Canevas(Transform parent, string nom, int ordre)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordre;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void Poser(RectTransform rect, Vector2 ancre, Vector2 position, Vector2 dimensions)
        {
            rect.anchorMin = ancre;
            rect.anchorMax = ancre;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }
    }
}
