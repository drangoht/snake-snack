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

        /// <summary>Le canevas entier, masqué d'un bloc quand le menu prend l'écran (GDD §4.6).</summary>
        private GameObject _canvas;

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

        // --- Bonds des nombres (docs/art/juicy.md §5 et §8) ------------------------------
        //
        // ⚠ Sur unscaledTime : le bond du record rejoue à l'ouverture de l'écran de fin, donc à un
        // moment où la partie ne tourne plus.

        private const double DureeBondScore = 0.160;
        private const double DureeBondRecord = 0.220;
        private const double AmpleurBondScore = 0.18;
        private const double AmpleurBondRecord = 0.30;

        private double _debutBondScore = double.NegativeInfinity;
        private double _debutBondRecord = double.NegativeInfinity;

        private void Awake()
        {
            Construire();
        }

        private void Construire()
        {
            _policeCourante = PolicesUi.Charger(PolicesUi.Courante);
            _policeTitres = PolicesUi.Charger(PolicesUi.Titres);

            // Sous le menu (200) et sous le tampon de build (1000), au-dessus du monde : le HUD ne
            // doit masquer ni le menu ni l'estampille qui identifie la version sur une capture.
            GameObject canvasGo = FabriqueUi.Canevas(transform, "Canvas HUD", 100).gameObject;
            _canvas = canvasGo;

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
            Image voile = FabriqueUi.Voile(parent, "Voile", UiPalette.VoileDePause);
            voile.gameObject.SetActive(false);
            return voile;
        }

        private static Text ConstruireTexte(
            Transform parent, string nom, Font police, int taille, TextAnchor alignement,
            Color couleur, Vector2 ancre, Vector2 position, Vector2 dimensions)
        {
            return FabriqueUi.Texte(parent, nom, police, taille, alignement, couleur, ancre, position, dimensions);
        }

        /// <summary>
        /// Montre ou masque tout le HUD.
        /// </summary>
        /// <remarks>
        /// ⚠ Le canevas entier, et pas les textes un par un : le menu (GDD §4.6) prend l'écran en
        /// entier, et un seul texte oublié — le rappel des commandes, le tampon du score — se
        /// retrouverait posé par-dessus le titre du jeu.
        /// </remarks>
        public void Montrer(bool visible)
        {
            _canvas.SetActive(visible);
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
            // ⚠ Comparés AVANT d'écrire les champs : c'est le changement qui déclenche le bond, pas
            // la valeur. Sans cette comparaison, le simple rafraîchissement d'une nouvelle partie
            // ferait sauter les deux nombres alors que rien n'a été gagné.
            bool scoreMonte = points > _points;
            bool recordVientDEtreBattu = recordBattu && !_recordBattu;

            _points = points;
            _meilleur = record;
            _recordBattu = recordBattu;

            _score.text = TextesUi.LigneScore(points);
            _record.text = TextesUi.LigneRecord(record);

            if (scoreMonte)
            {
                _debutBondScore = Time.unscaledTimeAsDouble;
            }

            if (recordVientDEtreBattu)
            {
                _debutBondRecord = Time.unscaledTimeAsDouble;
            }
        }

        /// <summary>
        /// Fait respirer les nombres qui viennent de monter (<c>docs/art/juicy.md</c> §5, §8).
        /// </summary>
        /// <remarks>
        /// ⚠ L'échelle est reposée <b>exactement</b> à 1 en fin d'enveloppe : un `Text` laissé à
        /// 1,002 resterait imperceptiblement plus gros pour le reste de la session, et personne ne
        /// rattacherait ce décalage à une animation de 160 ms.
        ///
        /// <para>⚠ Aucun changement de couleur : le bond dit « ça a monté », il n'emprunte ni
        /// <c>Pictogramme</c> (réservé au refus) ni <c>Pomme</c> (réservée à la nourriture) —
        /// « une couleur = un rôle » (<c>docs/art/palette.md</c> §1.2).</para>
        /// </remarks>
        private void Update()
        {
            double maintenant = Time.unscaledTimeAsDouble;

            _debutBondScore = AppliquerBond(_score, _debutBondScore, DureeBondScore, AmpleurBondScore, maintenant);
            _debutBondRecord = AppliquerBond(_record, _debutBondRecord, DureeBondRecord, AmpleurBondRecord, maintenant);
        }

        /// <summary>Rend le nouveau début d'enveloppe : éteint une fois le bond terminé.</summary>
        private static double AppliquerBond(Text cible, double debut, double duree, double ampleur, double maintenant)
        {
            if (debut <= double.NegativeInfinity || cible == null)
            {
                return debut;
            }

            double t = Rules.Rebond.Progres(debut, duree, maintenant);
            float facteur = (float)(1.0 + (ampleur * Rules.Rebond.Impulsion(t)));
            cible.transform.localScale = new Vector3(facteur, facteur, 1f);

            if (t < 1.0)
            {
                return debut;
            }

            cible.transform.localScale = Vector3.one;
            return double.NegativeInfinity;
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
