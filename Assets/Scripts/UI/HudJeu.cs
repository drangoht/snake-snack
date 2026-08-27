using SnakeSnack.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SnakeSnack.UI
{
    /// <summary>
    /// Les textes de l'interface : état, rappel des commandes, écrans de pause et de mort.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Le HUD construit ses propres enfants au démarrage</b>, plutôt que d'être assemblé dans
    /// <c>SceneBuilder</c> avec des références sérialisées. Une référence sérialisée vers un
    /// <c>Text</c> qui a changé de nom ou d'ordre se retrouve nulle <b>dans la scène régénérée</b>,
    /// et le seul symptôme est un texte qui n'apparaît pas — aucune erreur, aucun avertissement.
    ///
    /// <para>⚠ Aucune flèche Unicode dans ces textes (<c>docs/ART.md</c> §5.7) : elles disparaissent
    /// silencieusement en WebGL. Les libellés viennent de <see cref="TextesUi"/>, jamais écrits en
    /// dur ici.</para>
    /// </remarks>
    public sealed class HudJeu : MonoBehaviour
    {
        private Text _etat;
        private Text _commandes;
        private Text _titre;
        private Text _sousTitre;
        private Text _refusEnPause;
        private Image _voile;

        private void Awake()
        {
            Construire();
        }

        private void Construire()
        {
            var canvasGo = new GameObject("Canvas HUD");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Sous le tampon de build (1000), au-dessus du monde : le HUD ne doit jamais masquer
            // l'estampille qui identifie la version sur une capture d'écran.
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            canvasGo.AddComponent<GraphicRaycaster>();

            _voile = ConstruireVoile(canvasGo.transform);

            _etat = ConstruireTexte(canvasGo.transform, "Etat", 22, TextAnchor.MiddleCenter,
                PaletteProvisoire.TexteHud, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 40f));

            _commandes = ConstruireTexte(canvasGo.transform, "Commandes", 15, TextAnchor.LowerCenter,
                PaletteProvisoire.TexteSecondaire, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1100f, 24f));
            _commandes.text = TextesUi.RappelDesCommandes;

            _titre = ConstruireTexte(canvasGo.transform, "Titre", 54, TextAnchor.MiddleCenter,
                PaletteProvisoire.TexteHud, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(900f, 80f));

            _sousTitre = ConstruireTexte(canvasGo.transform, "SousTitre", 20, TextAnchor.MiddleCenter,
                PaletteProvisoire.TexteSecondaire, new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(900f, 30f));

            _refusEnPause = ConstruireTexte(canvasGo.transform, "RefusEnPause", 18, TextAnchor.MiddleCenter,
                PaletteProvisoire.TexteHud, new Vector2(0.5f, 0.5f), new Vector2(0f, -65f), new Vector2(900f, 28f));
            _refusEnPause.text = TextesUi.RefusEnPause;
            _refusEnPause.gameObject.SetActive(false);
        }

        private static Image ConstruireVoile(Transform parent)
        {
            var go = new GameObject("Voile");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = PaletteProvisoire.VoileDePause;
            image.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.SetActive(false);
            return image;
        }

        private static Text ConstruireTexte(
            Transform parent, string nom, int taille, TextAnchor alignement,
            Color couleur, Vector2 ancre, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);

            var texte = go.AddComponent<Text>();
            // Police intégrée : aucun asset de police n'existe encore (docs/ART.md §2). Sans police,
            // un Text ne dessine rien du tout, en silence.
            texte.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texte.fontSize = taille;
            texte.alignment = alignement;
            texte.color = couleur;
            texte.raycastTarget = false;
            texte.horizontalOverflow = HorizontalWrapMode.Overflow;
            texte.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = ancre;
            rect.anchorMax = ancre;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            return texte;
        }

        /// <summary>Met l'interface à l'état de la partie.</summary>
        public void Afficher(EtatPartie etat)
        {
            switch (etat)
            {
                case EtatPartie.EnAttente:
                    _etat.text = TextesUi.EtatEnAttente;
                    Fin(false, string.Empty, string.Empty);
                    break;

                case EtatPartie.EnCours:
                    _etat.text = TextesUi.EtatEnCours;
                    Fin(false, string.Empty, string.Empty);
                    break;

                case EtatPartie.EnPause:
                    _etat.text = TextesUi.EtatEnPause;
                    Fin(true, TextesUi.TitrePause, TextesUi.SousTitrePause);
                    break;

                case EtatPartie.Victoire:
                    // ⚠ Cas écrit explicitement : sans lui, la victoire tombe dans le `default` et
                    // le joueur qui vient de remplir la grille lit « PERDU ». Rien ne le signalerait.
                    _etat.text = TextesUi.EtatVictoire;
                    Fin(true, TextesUi.TitreVictoire, TextesUi.SousTitreVictoire);
                    break;

                default:
                    _etat.text = TextesUi.EtatMort;
                    Fin(true, TextesUi.TitreMort, TextesUi.SousTitreMort);
                    break;
            }

            if (etat != EtatPartie.EnPause)
            {
                // Le message de refus appartient à l'écran de pause : le laisser vivre après la
                // reprise le ferait apparaître par-dessus la partie, sans rapport avec une touche.
                AfficherRefusEnPause(false);
            }
        }

        /// <summary>Ligne « touche ignorée » de l'écran de pause (ART §5.4).</summary>
        public void AfficherRefusEnPause(bool visible)
        {
            _refusEnPause.gameObject.SetActive(visible);
        }

        private void Fin(bool visible, string titre, string sousTitre)
        {
            _voile.gameObject.SetActive(visible);
            _titre.text = titre;
            _sousTitre.text = sousTitre;
        }
    }
}
