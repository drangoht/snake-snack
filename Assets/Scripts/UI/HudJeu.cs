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
        /// <summary>
        /// Marge entre le bord de l'écran et les nombres du bandeau, en pixels du cadre 1280x720.
        /// </summary>
        private const float MargeLaterale = 28f;

        /// <summary>Largeur réservée à un nombre du bandeau (« Record 312 » tient largement).</summary>
        private const float LargeurNombre = 260f;

        private Text _etat;
        private Text _commandes;
        private Text _score;
        private Text _record;
        private Text _titre;
        private Text _recapFin;
        private Text _sousTitre;
        private Text _refusEnPause;
        private Image _voile;

        // Les deux seules graisses du jeu (ART §2.2). SemiBold porte le texte secondaire et
        // permanent, ExtraBold les titres et les nombres — il n'y a pas de Regular : à ces tailles,
        // sur un rendu WebGL redimensionné, un trait fin de police ronde disparaît avant de se lire.
        private Font _policeCourante;
        private Font _policeTitres;

        // Derniers nombres reçus. ⚠ Le récapitulatif de fin se compose À LA MORT, à partir d'eux :
        // l'écran de fin ne doit pas dépendre de l'ordre dans lequel le gameplay appelle les deux
        // méthodes publiques de ce composant.
        private int _points;
        private int _meilleur;
        private bool _recordBattu;

        private void Awake()
        {
            Construire();
        }

        private void Construire()
        {
            _policeCourante = ChargerGraisse("Nunito-SemiBold");
            _policeTitres = ChargerGraisse("Nunito-ExtraBold");

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

            // ⚠ Les corps ci-dessous sont ceux de docs/ART.md §2.3, relevés de deux points. Le
            // CanvasScaler ci-dessus les exprime en pixels du cadre 1280x720 : sur la fenêtre plus
            // petite d'une page itch ils RÉTRÉCISSENT proportionnellement, ce qui est l'inverse
            // d'une marge de sécurité. Plancher absolu : 18 px ici, sous quoi le downscale rend le
            // texte illisible avant même le poids de la police.
            _voile = ConstruireVoile(canvasGo.transform);

            _etat = ConstruireTexte(canvasGo.transform, "Etat", _policeTitres, 24, TextAnchor.MiddleCenter,
                UiPalette.TexteHud, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 40f));

            // ⚠ 14 px du bas, pas 10 : le pivot est au centre d'une boîte de 24 px de haut, donc à
            // 10 px le BAS de la boîte tombait 2 px SOUS l'écran et les jambages de « g », « p »,
            // « q » étaient tronqués (BUG-002 du 2026-08-28, mesuré sur build). 14 px fait tenir la
            // boîte entière avec 2 px de reste. Ce n'est PAS le fond du sujet : il n'y a aucune
            // marge basse sous l'aire de jeu (docs/gdd/grille.md), et cet arbitrage-là reste ouvert.
            _commandes = ConstruireTexte(canvasGo.transform, "Commandes", _policeCourante, 18, TextAnchor.LowerCenter,
                UiPalette.TexteSecondaire, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1100f, 24f));
            _commandes.text = TextesUi.RappelDesCommandes;

            // Score à gauche, record à droite, dans le bandeau du haut : hors de l'aire de jeu
            // (GDD §4.3), et affichés EN PERMANENCE (§4.5). Un objectif qu'on ne découvre qu'une
            // fois perdu ne se vise pas — c'est le record lu pendant la partie qui transforme la
            // relance en « battre 14 ». Le rect est décalé d'une demi-largeur parce que le pivot est
            // au centre : le bord du texte tombe alors à MargeLaterale du bord de l'écran.
            _score = ConstruireTexte(canvasGo.transform, "Score", _policeTitres, 24, TextAnchor.MiddleLeft,
                UiPalette.TexteHud, new Vector2(0f, 1f),
                new Vector2(MargeLaterale + (LargeurNombre / 2f), -30f), new Vector2(LargeurNombre, 40f));

            // Record en texte secondaire : même place, même taille, mais c'est le score courant que
            // le joueur suit pendant la partie. La hiérarchie se lit sans lire les deux étiquettes.
            _record = ConstruireTexte(canvasGo.transform, "Record", _policeTitres, 24, TextAnchor.MiddleRight,
                UiPalette.TexteSecondaire, new Vector2(1f, 1f),
                new Vector2(-MargeLaterale - (LargeurNombre / 2f), -30f), new Vector2(LargeurNombre, 40f));

            AfficherScore(0, 0, false);

            _titre = ConstruireTexte(canvasGo.transform, "Titre", _policeTitres, 56, TextAnchor.MiddleCenter,
                UiPalette.TexteHud, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(900f, 80f));

            // Entre le titre et la relance : le GDD §2 veut score et record « affichés sur place »
            // à la mort. Ils sont déjà dans le bandeau, mais faire remonter le regard tout en haut
            // au moment où l'on décide de rejouer, c'est le perdre.
            _recapFin = ConstruireTexte(canvasGo.transform, "RecapFin", _policeTitres, 26, TextAnchor.MiddleCenter,
                UiPalette.TexteHud, new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(900f, 34f));

            _sousTitre = ConstruireTexte(canvasGo.transform, "SousTitre", _policeCourante, 22, TextAnchor.MiddleCenter,
                UiPalette.TexteSecondaire, new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(900f, 30f));

            // ⚠ Sous le sous-titre, et pas à la place du récapitulatif : les deux ne s'affichent
            // jamais ensemble (l'un en pause, l'autre à la fin), mais deux textes qui se disputent
            // la même position finissent par se superposer le jour où cette exclusion cesse d'être
            // vraie.
            _refusEnPause = ConstruireTexte(canvasGo.transform, "RefusEnPause", _policeCourante, 20, TextAnchor.MiddleCenter,
                UiPalette.TexteHud, new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(900f, 28f));
            _refusEnPause.text = TextesUi.RefusEnPause;
            _refusEnPause.gameObject.SetActive(false);
        }

        private static Image ConstruireVoile(Transform parent)
        {
            var go = new GameObject("Voile");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = UiPalette.VoileDePause;
            image.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.SetActive(false);
            return image;
        }

        /// <summary>
        /// Charge une graisse depuis <c>Resources/Polices/</c>.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Une police absente ne lève rien</b> : <c>Resources.Load</c> rend <c>null</c>, le
        /// <c>Text</c> garde une police nulle et ne dessine simplement aucun pixel — pas de carré
        /// blanc, pas d'exception, un HUD vide. D'où l'erreur explicite ET le repli sur la police
        /// intégrée : un HUD moche se voit, un HUD vide se cherche pendant une heure.
        ///
        /// <para>Les deux <c>.ttf</c> sont <b>produits</b> par <c>tools/generer_polices.py</c> :
        /// google/fonts ne publie Nunito qu'en fichier variable (<c>buildStatic: false</c> en amont),
        /// le script en instancie <c>wght=600</c> et <c>wght=800</c>. Ils ne se retéléchargent
        /// pas à la main.</para>
        /// </remarks>
        private static Font ChargerGraisse(string nom)
        {
            Font police = Resources.Load<Font>("Polices/" + nom);
            if (police != null)
            {
                return police;
            }

            Debug.LogError("Police introuvable : Resources/Polices/" + nom
                           + " — relancer « py tools/generer_polices.py ». Repli sur la police intégrée.");
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Text ConstruireTexte(
            Transform parent, string nom, Font police, int taille, TextAnchor alignement,
            Color couleur, Vector2 ancre, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);

            var texte = go.AddComponent<Text>();
            texte.font = police;
            // ⚠ Jamais de FontStyle.Bold par-dessus : la graisse vient du FICHIER (SemiBold ou
            // ExtraBold). Le gras synthétique d'uGUI s'ajouterait au dessin déjà gras et boucherait
            // les contre-formes rondes de Nunito — exactement ce que l'ART §2.4 interdit aux
            // contours épais.
            texte.fontStyle = FontStyle.Normal;
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
                    Fin(true, TextesUi.TitreVictoire, TextesUi.SousTitreVictoire, Recapitulatif());
                    break;

                default:
                    _etat.text = TextesUi.EtatMort;
                    Fin(true, TextesUi.TitreMort, TextesUi.SousTitreMort, Recapitulatif());
                    break;
            }

            if (etat != EtatPartie.EnPause)
            {
                // Le message de refus appartient à l'écran de pause : le laisser vivre après la
                // reprise le ferait apparaître par-dessus la partie, sans rapport avec une touche.
                AfficherRefusEnPause(false);
            }
        }

        /// <summary>
        /// Les deux nombres du bandeau (GDD §4.5).
        /// </summary>
        /// <param name="recordBattu">
        /// Vrai quand la partie en cours a dépassé le record qu'elle a trouvé en commençant. Sert
        /// uniquement au récapitulatif de fin — pendant la partie, deux nombres égaux se
        /// comprennent d'eux-mêmes, puisqu'on les a vus monter ensemble.
        /// </param>
        public void AfficherScore(int points, int record, bool recordBattu)
        {
            _points = points;
            _meilleur = record;
            _recordBattu = recordBattu;

            _score.text = TextesUi.LigneScore(points);
            _record.text = TextesUi.LigneRecord(record);
        }

        /// <summary>Ligne « touche ignorée » de l'écran de pause (ART §5.4).</summary>
        public void AfficherRefusEnPause(bool visible)
        {
            _refusEnPause.gameObject.SetActive(visible);
        }

        private string Recapitulatif()
        {
            return TextesUi.RecapitulatifDeFin(_points, _meilleur, _recordBattu);
        }

        private void Fin(bool visible, string titre, string sousTitre)
        {
            Fin(visible, titre, sousTitre, string.Empty);
        }

        private void Fin(bool visible, string titre, string sousTitre, string recapitulatif)
        {
            _voile.gameObject.SetActive(visible);
            _titre.text = titre;
            _sousTitre.text = sousTitre;
            _recapFin.text = recapitulatif;
        }
    }
}
