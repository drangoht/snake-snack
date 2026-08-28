using System;
using System.Collections.Generic;
using SnakeSnack.Rules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SnakeSnack.UI
{
    /// <summary>
    /// Le menu principal (GDD §4.6) : titre, illustration du serpent, entrées navigables et panneaux
    /// de lecture.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Aucune règle n'est décidée ici</b> : la composition des entrées et la navigation viennent
    /// de <see cref="MenuPrincipal"/>, testé sans moteur. Ce composant lit le résultat, le dessine et
    /// l'anime — rien de plus. C'est la même séparation que <c>JeuSnake</c> et <c>Rules/</c>, et pour
    /// la même raison : une règle recopiée dans un <c>Update()</c> devient une seconde vérité.
    ///
    /// <para>⚠ <b>Tout est construit par code</b>, comme le HUD : la scène est régénérée à chaque
    /// build (<c>SceneBuilder</c>), et une référence sérialisée perdue ne lèverait rien — elle
    /// donnerait un menu incomplet.</para>
    ///
    /// <para>⚠ <b>Le temps employé est <c>unscaledTime</c></b> : le menu s'affiche à un moment où
    /// rien ne garantit que le temps de jeu avance, et une animation d'ouverture figée passerait
    /// pour un gel du jeu.</para>
    /// </remarks>
    public sealed class EcranMenu : MonoBehaviour
    {
        /// <summary>Au-dessus du HUD (100), sous le tampon de build (1000) — <c>docs/pitfalls/interface.md</c>.</summary>
        private const int OrdreDeTri = 200;

        // --- Mise en page, en pixels du cadre de référence 1280x720, origine au centre ---------

        /// <summary>Bord gauche de la colonne de texte.</summary>
        private const float ColonneX = -520f;

        /// <summary>Décalage du libellé par rapport au bord de la ligne : c'est la place du curseur.</summary>
        private const float DecalageLibelle = 46f;

        private const float LargeurLigne = 470f;
        private const float HauteurLigne = 48f;
        private const float EspacementLignes = 62f;

        /// <summary>Centre vertical du bloc d'entrées — le bloc reste centré quel que soit son nombre d'entrées.</summary>
        private const float CentreLignes = -66f;

        private const float IllustrationX = 300f;
        private const float IllustrationY = 24f;
        private const float IllustrationCote = 390f;

        // --- Animations -----------------------------------------------------------------------

        private const float DureeOuverture = 0.42f;
        private const float DureeApparitionLigne = 0.26f;
        private const float RetardDesLignes = 0.10f;
        private const float RetardParLigne = 0.07f;
        private const float DureeFermeture = 0.16f;

        /// <summary>De combien une entrée glisse vers la droite en apparaissant.</summary>
        private const float GlissementLigne = 34f;

        /// <summary>
        /// Flottement de l'illustration.
        /// </summary>
        /// <remarks>
        /// ⚠ <c>docs/ART.md</c> §4 interdit « le clignotement périodique en boucle sur une grande
        /// surface » : c'est une variation d'<b>opacité</b> qui est visée. Ici, l'opacité ne bouge
        /// pas — l'image se déplace de 8 px sur une période de 4 s et s'incline de 1,6°. Rien ne
        /// scintille, et le menu cesse d'avoir l'air d'une capture d'écran figée.
        /// </remarks>
        private const float PeriodeFlottement = 4.2f;

        private const float AmplitudeFlottement = 8f;
        private const float PeriodeBalancement = 5.3f;
        private const float AmplitudeBalancement = 1.6f;

        /// <summary>Vitesse de rattrapage du curseur et du surlignage (lissage exponentiel).</summary>
        private const float VitesseSelection = 16f;

        private const float GrossissementSelection = 1.07f;

        private enum Phase
        {
            /// <summary>Rien à l'écran, aucun coût : <c>Update</c> sort tout de suite.</summary>
            Ferme,

            Ouverture,
            Repos,
            Fermeture
        }

        /// <summary>
        /// Levée quand une entrée qui <b>engage l'application</b> a été validée, une fois le fondu de
        /// sortie terminé.
        /// </summary>
        /// <remarks>
        /// « Comment jouer » et « Crédits » n'en font pas partie : ils ouvrent un panneau, restent
        /// dans le menu, et ne regardent donc que ce composant. Seuls <see cref="EntreeMenu.Jouer"/>
        /// et <see cref="EntreeMenu.Quitter"/> remontent.
        /// </remarks>
        public event Action<EntreeMenu> Validee;

        private readonly List<RectTransform> _lignes = new List<RectTransform>();
        private readonly List<Text> _libelles = new List<Text>();

        /// <summary>Opacité de chaque ligne, posée par l'animation d'ouverture en cascade.</summary>
        private readonly List<float> _opacites = new List<float>();

        /// <summary>Avancement du surlignage de chaque ligne : 0 au repos, 1 sélectionnée.</summary>
        private readonly List<float> _mises = new List<float>();

        private GameObject _racine;
        private CanvasGroup _groupe;
        private RectTransform _illustration;
        private RectTransform _titre;
        private RectTransform _accroche;
        private RectTransform _curseur;
        private Image _curseurImage;

        private IReadOnlyList<EntreeMenu> _entrees;
        private int _index;

        private PanneauInfo _aide;
        private PanneauInfo _credits;

        private Phase _phase = Phase.Ferme;
        private float _chrono;
        private float _curseurY;
        private EntreeMenu _entreeValidee;

        /// <summary>Position du pointeur au moment où le menu s'est ouvert — voir <see cref="Survoler"/>.</summary>
        private Vector2 _pointeurALOuverture;

        private bool _pointeurABouge;

        /// <summary>Vrai tant que le menu occupe l'écran, fondu de sortie compris.</summary>
        public bool Actif
        {
            get { return _phase != Phase.Ferme; }
        }

        /// <summary>Vrai quand un panneau de lecture est ouvert : Échap le referme au lieu de rien faire.</summary>
        public bool PanneauOuvert
        {
            get { return _aide.Demande || _credits.Demande; }
        }

        private bool Interactif
        {
            get { return _phase == Phase.Repos; }
        }

        private void Awake()
        {
            Construire();
        }

        private void Construire()
        {
            // ⚠ La disponibilité de « Quitter » est décidée par la PLATEFORME, pas par une directive
            // de compilation : le menu du build web se teste ainsi dans l'éditeur en changeant une
            // seule valeur, sans construire un build WebGL de vingt minutes.
            _entrees = MenuPrincipal.Entrees(Application.platform != RuntimePlatform.WebGLPlayer);

            Canvas canvas = FabriqueUi.Canevas(transform, "Canvas Menu", OrdreDeTri);
            _racine = canvas.gameObject;
            _groupe = _racine.AddComponent<CanvasGroup>();

            // Fond opaque : le menu ne montre jamais l'aire de jeu par transparence, même si un
            // appelant oubliait de la masquer. Il intercepte aussi les clics (raycastTarget).
            Image fond = FabriqueUi.Voile(_racine.transform, "Fond", UiPalette.Fond);
            fond.raycastTarget = true;

            ConstruireIllustration(_racine.transform);

            Font policeTitres = PolicesUi.Charger(PolicesUi.Titres);
            Font policeCourante = PolicesUi.Charger(PolicesUi.Courante);

            Text titre = FabriqueUi.Texte(_racine.transform, "TitreDuJeu", policeTitres, 64, TextAnchor.MiddleLeft,
                UiPalette.TexteHud, Centre, new Vector2(ColonneX, 172f), new Vector2(640f, 84f));
            titre.text = TextesUi.TitreDuJeu;
            _titre = AlignerAGauche(titre.rectTransform, ColonneX, 172f);

            Text accroche = FabriqueUi.Texte(_racine.transform, "Accroche", policeCourante, 21, TextAnchor.MiddleLeft,
                UiPalette.TexteSecondaire, Centre, new Vector2(ColonneX, 118f), new Vector2(640f, 30f));
            accroche.text = TextesUi.AccrocheMenu;
            _accroche = AlignerAGauche(accroche.rectTransform, ColonneX + 4f, 118f);

            ConstruireCurseur(_racine.transform);
            ConstruireLignes(_racine.transform, policeTitres);

            FabriqueUi.Texte(_racine.transform, "PiedDeMenu", policeCourante, 18, TextAnchor.LowerCenter,
                UiPalette.TexteSecondaire, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1100f, 24f))
                .text = TextesUi.PiedDeMenu;

            // ⚠ Les panneaux sont créés APRÈS les entrées : à ordre de tri égal, uGUI empile dans
            // l'ordre de la hiérarchie, et un panneau créé avant passerait sous les entrées qu'il
            // est censé recouvrir.
            _aide = new PanneauInfo(_racine.transform, "PanneauAide",
                TextesUi.TitreCommentJouer, TextesUi.CorpsCommentJouer, () => Retour());
            _credits = new PanneauInfo(_racine.transform, "PanneauCredits",
                TextesUi.TitreCredits, TextesUi.CorpsCredits, () => Retour());

            _racine.SetActive(false);
        }

        private static Vector2 Centre
        {
            get { return new Vector2(0.5f, 0.5f); }
        }

        /// <summary>
        /// Repose un rectangle sur son bord gauche.
        /// </summary>
        /// <remarks>
        /// ⚠ <see cref="FabriqueUi"/> centre le pivot, ce qui convient à tout le HUD mais pas à une
        /// colonne : avec un pivot centré, deux textes de longueurs différentes ne commencent pas au
        /// même x, et la colonne du menu se lit alors comme un alignement raté.
        /// </remarks>
        private static RectTransform AlignerAGauche(RectTransform rect, float x, float y)
        {
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        private void ConstruireIllustration(Transform parent)
        {
            // ⚠ Chargée PAR CHEMIN depuis Resources/ : le menu n'a aucune référence sérialisée
            // (la scène est régénérée à chaque build). Le PNG est produit par
            // `tools/generer_illustration_serpent.py` et importé en Sprite par
            // `Assets/Editor/ImportIllustrations.cs`.
            Sprite illustration = Resources.Load<Sprite>("Illustrations/serpent-menu");

            var go = new GameObject("Illustration");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            _illustration = go.GetComponent<RectTransform>();
            _illustration.anchorMin = Centre;
            _illustration.anchorMax = Centre;
            _illustration.pivot = Centre;
            _illustration.anchoredPosition = new Vector2(IllustrationX, IllustrationY);
            _illustration.sizeDelta = new Vector2(IllustrationCote, IllustrationCote);

            if (illustration == null)
            {
                // ⚠ Ce cas ne lève rien de lui-même : `Resources.Load<Sprite>` rend `null` aussi
                // bien quand le fichier manque que quand il est importé en TEXTURE au lieu de
                // sprite (docs/pitfalls/assets-import.md). Sans ce message, le menu s'afficherait
                // simplement amputé de son illustration, et la cause serait invisible.
                Debug.LogError("Illustration introuvable : Resources/Illustrations/serpent-menu — "
                               + "relancer « py tools/generer_illustration_serpent.py » puis un build. "
                               + "Si le fichier existe, vérifier qu'il est importé en Sprite.");
                go.SetActive(false);
                return;
            }

            image.sprite = illustration;
        }

        /// <summary>
        /// Le curseur de sélection : un <b>losange</b> rouge, la pomme du jeu.
        /// </summary>
        /// <remarks>
        /// ⚠ La forme reprend celle de la pomme (ART §4 : l'information ne tient jamais à la seule
        /// couleur). Le joueur qui n'a pas encore lancé de partie apprend d'un coup d'œil ce que la
        /// forme rouge signifie, et le menu ne dépense pas un symbole de plus pour ça.
        /// </remarks>
        private void ConstruireCurseur(Transform parent)
        {
            _curseurImage = FabriqueUi.Rectangle(parent, "Curseur", UiPalette.Pomme, Centre,
                new Vector2(ColonneX + 16f, 0f), new Vector2(16f, 16f));
            _curseur = _curseurImage.rectTransform;
            _curseur.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void ConstruireLignes(Transform parent, Font police)
        {
            for (int i = 0; i < _entrees.Count; i++)
            {
                // Le RectTransform est demandé À LA CRÉATION : ajouté après coup à un GameObject
                // qui porte déjà un Transform, il oblige Unity à remplacer le composant, ce qui
                // marche mais dépend d'un détail d'implémentation.
                var go = new GameObject("Entree" + _entrees[i], typeof(RectTransform));
                go.transform.SetParent(parent, false);

                var ligne = (RectTransform)go.transform;
                ligne.anchorMin = Centre;
                ligne.anchorMax = Centre;
                ligne.pivot = new Vector2(0f, 0.5f);
                ligne.sizeDelta = new Vector2(LargeurLigne, HauteurLigne);
                ligne.anchoredPosition = new Vector2(ColonneX, YdeLaLigne(i));

                // Zone de survol : une image entièrement transparente. Ce n'est pas une couleur de
                // la palette, c'est une cible de raycast — les `Text` du jeu ont tous
                // `raycastTarget = false`, la souris n'aurait donc rien à toucher.
                Image zone = FabriqueUi.Voile(ligne, "Zone", new Color(0f, 0f, 0f, 0f));
                zone.raycastTarget = true;

                int rang = i; // ⚠ capturé dans une variable locale : `i` vaudrait _entrees.Count
                var cliquable = zone.gameObject.AddComponent<ZoneCliquable>();
                cliquable.Survolee = () => Survoler(rang);
                cliquable.Cliquee = () => { Survoler(rang); Valider(); };

                Text libelle = FabriqueUi.Texte(ligne, "Libelle", police, 30, TextAnchor.MiddleLeft,
                    UiPalette.TexteSecondaire, new Vector2(0f, 0.5f), Vector2.zero,
                    new Vector2(LargeurLigne - DecalageLibelle, 40f));
                libelle.text = TextesUi.LibelleEntree(_entrees[i]);
                AlignerAGauche(libelle.rectTransform, DecalageLibelle, 0f);

                _lignes.Add(ligne);
                _libelles.Add(libelle);
                _opacites.Add(1f);
                _mises.Add(0f);
            }
        }

        /// <summary>Ordonnée d'une entrée : le bloc reste centré sur <see cref="CentreLignes"/>.</summary>
        private float YdeLaLigne(int rang)
        {
            float demiBloc = (_entrees.Count - 1) * EspacementLignes / 2f;
            return CentreLignes + demiBloc - (rang * EspacementLignes);
        }

        /// <summary>Affiche le menu et rejoue son animation d'ouverture.</summary>
        /// <remarks>
        /// ⚠ La sélection repart sur la <b>première entrée</b> à chaque ouverture, y compris au
        /// retour d'une partie : « Jouer » est ce que le joueur veut faire neuf fois sur dix, et
        /// retrouver le curseur là où on l'avait laissé n'a d'intérêt que dans un menu long.
        /// </remarks>
        public void Ouvrir()
        {
            _racine.SetActive(true);
            _groupe.alpha = 0f;
            _groupe.blocksRaycasts = true;

            _phase = Phase.Ouverture;
            _chrono = 0f;
            _index = 0;

            _aide.Fermer();
            _credits.Fermer();

            // Le curseur est POSÉ, pas interpolé : à l'ouverture, il n'a pas de position précédente
            // d'où glisser, et une glissade depuis le haut de l'écran se lirait comme un défaut.
            _curseurY = YdeLaLigne(0);

            // ⚠ Le pointeur ne prend la main qu'après avoir BOUGÉ (voir Survoler).
            _pointeurALOuverture = PositionDuPointeur();
            _pointeurABouge = false;

            for (int i = 0; i < _lignes.Count; i++)
            {
                _opacites[i] = 0f;
                _mises[i] = i == 0 ? 1f : 0f;
            }

            AppliquerSelection();
        }

        /// <summary>Ferme immédiatement, sans fondu ni événement. Pour un appelant qui reprend la main.</summary>
        public void FermerImmediatement()
        {
            _phase = Phase.Ferme;
            _groupe.alpha = 0f;
            _groupe.blocksRaycasts = false;
            _racine.SetActive(false);
        }

        /// <summary>Déplace la sélection (GDD §4.6 : bouclage, et rien sur les directions latérales).</summary>
        public void Deplacer(Direction direction)
        {
            if (!Interactif || PanneauOuvert)
            {
                return;
            }

            int nouvel;
            if (MenuPrincipal.Deplacer(_index, _entrees.Count, direction, out nouvel))
            {
                _index = nouvel;
            }
        }

        /// <summary>Valide l'entrée courante — ou referme le panneau ouvert.</summary>
        public void Valider()
        {
            if (!Interactif)
            {
                return;
            }

            if (PanneauOuvert)
            {
                // Entrée et Espace referment aussi : un panneau qui ne se ferme QUE par Échap piège
                // le joueur qui vient d'apprendre que ce menu se valide à Entrée.
                Retour();
                return;
            }

            EntreeMenu entree = _entrees[MenuPrincipal.Borner(_index, _entrees.Count)];

            switch (entree)
            {
                case EntreeMenu.CommentJouer:
                    _aide.Ouvrir();
                    return;

                case EntreeMenu.Credits:
                    _credits.Ouvrir();
                    return;

                default:
                    _entreeValidee = entree;
                    _phase = Phase.Fermeture;
                    _chrono = 0f;
                    _groupe.blocksRaycasts = false;
                    return;
            }
        }

        /// <summary>Referme le panneau ouvert. Rend vrai s'il y en avait un.</summary>
        public bool Retour()
        {
            if (!PanneauOuvert)
            {
                return false;
            }

            _aide.Fermer();
            _credits.Fermer();
            return true;
        }

        /// <summary>
        /// Le pointeur est entré sur une entrée : elle devient la sélection courante.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Tant que la souris n'a pas bougé, elle ne sélectionne rien</b>, et ce n'est pas une
        /// précaution théorique : le menu s'ouvre sous un curseur immobile — au lancement du jeu, au
        /// retour d'une partie, quand la fenêtre reprend le premier plan — et uGUI envoie alors un
        /// « pointeur entré » pour une souris que personne n'a touchée. Sans ce verrou, la sélection
        /// saute à l'entrée qui se trouve par hasard sous le curseur, et le joueur qui tape Entrée en
        /// croyant lancer une partie <b>quitte le jeu</b>. Constaté le 2026-08-28, en pilotant le
        /// build : le curseur reposait sur « Quitter » et le jeu s'est fermé sur la première touche.
        /// </remarks>
        private void Survoler(int rang)
        {
            if (!Interactif || PanneauOuvert || !_pointeurABouge)
            {
                return;
            }

            _index = MenuPrincipal.Borner(rang, _entrees.Count);
        }

        /// <summary>Vrai dès que le pointeur s'est déplacé depuis l'ouverture du menu.</summary>
        /// <remarks>
        /// Le seuil est en pixels d'écran au carré : deux ou trois pixels de dérive ne sont pas une
        /// intention, et une souris optique en produit toute seule.
        /// </remarks>
        private void SurveillerLePointeur()
        {
            if (_pointeurABouge)
            {
                return;
            }

            _pointeurABouge = (PositionDuPointeur() - _pointeurALOuverture).sqrMagnitude > 16f;
        }

        private static Vector2 PositionDuPointeur()
        {
            Mouse souris = Mouse.current;
            return souris == null ? Vector2.zero : souris.position.ReadValue();
        }

        private void Update()
        {
            if (_phase == Phase.Ferme)
            {
                return;
            }

            float pas = Time.unscaledDeltaTime;
            _chrono += pas;

            if (_phase == Phase.Ouverture)
            {
                AnimerOuverture();
            }
            else if (_phase == Phase.Fermeture)
            {
                _groupe.alpha = Mathf.Clamp01(1f - (_chrono / DureeFermeture));

                if (_chrono >= DureeFermeture)
                {
                    Terminer();
                    return;
                }
            }

            SurveillerLePointeur();
            AnimerIllustration();
            AnimerSelection(pas);

            _aide.Animer(pas);
            _credits.Animer(pas);
        }

        private void AnimerOuverture()
        {
            float avancement = Adoucir(Mathf.Clamp01(_chrono / DureeOuverture));
            _groupe.alpha = avancement;

            // Titre et accroche montent de quelques pixels : le mouvement dit « ça vient d'arriver »
            // sans que le joueur ait à attendre.
            _titre.anchoredPosition = new Vector2(ColonneX, 172f - (14f * (1f - avancement)));
            _accroche.anchoredPosition = new Vector2(ColonneX + 4f, 118f - (10f * (1f - avancement)));

            for (int i = 0; i < _lignes.Count; i++)
            {
                float t = Adoucir(Mathf.Clamp01(
                    (_chrono - RetardDesLignes - (i * RetardParLigne)) / DureeApparitionLigne));

                _opacites[i] = t;
                _lignes[i].anchoredPosition = new Vector2(ColonneX - (GlissementLigne * (1f - t)), YdeLaLigne(i));
            }

            if (_chrono >= DureeTotaleOuverture)
            {
                _phase = Phase.Repos;
                PoserAuRepos();
            }
        }

        private float DureeTotaleOuverture
        {
            get
            {
                float cascade = RetardDesLignes + ((_lignes.Count - 1) * RetardParLigne) + DureeApparitionLigne;
                return Mathf.Max(DureeOuverture, cascade);
            }
        }

        private void PoserAuRepos()
        {
            _groupe.alpha = 1f;
            _titre.anchoredPosition = new Vector2(ColonneX, 172f);
            _accroche.anchoredPosition = new Vector2(ColonneX + 4f, 118f);

            for (int i = 0; i < _lignes.Count; i++)
            {
                _opacites[i] = 1f;
                _lignes[i].anchoredPosition = new Vector2(ColonneX, YdeLaLigne(i));
            }
        }

        private void AnimerIllustration()
        {
            if (_illustration == null)
            {
                return;
            }

            float temps = Time.unscaledTime;
            float flottement = Mathf.Sin(temps * 2f * Mathf.PI / PeriodeFlottement) * AmplitudeFlottement;
            float balancement = Mathf.Sin(temps * 2f * Mathf.PI / PeriodeBalancement) * AmplitudeBalancement;

            float echelle = _phase == Phase.Ouverture
                ? Mathf.Lerp(0.93f, 1f, Adoucir(Mathf.Clamp01(_chrono / DureeOuverture)))
                : 1f;

            _illustration.anchoredPosition = new Vector2(IllustrationX, IllustrationY + flottement);
            _illustration.localRotation = Quaternion.Euler(0f, 0f, balancement);
            _illustration.localScale = new Vector3(echelle, echelle, 1f);
        }

        private void AnimerSelection(float pas)
        {
            // Lissage exponentiel : indépendant de la fréquence d'images, contrairement à un
            // `Lerp(a, b, 0.2f)` par image qui va deux fois plus vite à 120 Hz qu'à 60.
            float rattrapage = 1f - Mathf.Exp(-VitesseSelection * pas);

            _curseurY = Mathf.Lerp(_curseurY, YdeLaLigne(_index), rattrapage);

            for (int i = 0; i < _lignes.Count; i++)
            {
                _mises[i] = Mathf.Lerp(_mises[i], i == _index ? 1f : 0f, rattrapage);
            }

            AppliquerSelection();
        }

        private void AppliquerSelection()
        {
            _curseur.anchoredPosition = new Vector2(ColonneX + 16f, _curseurY);

            Color rouge = UiPalette.Pomme;
            rouge.a = _opacites[MenuPrincipal.Borner(_index, _opacites.Count)];
            _curseurImage.color = rouge;

            for (int i = 0; i < _lignes.Count; i++)
            {
                // L'opacité vient de l'ouverture en cascade, la teinte de la sélection : les deux
                // animations écrivent la même couleur, il faut donc les composer et non les
                // écraser l'une l'autre.
                Color couleur = Color.Lerp(UiPalette.TexteSecondaire, UiPalette.TexteHud, _mises[i]);
                couleur.a = _opacites[i];
                _libelles[i].color = couleur;

                float echelle = Mathf.Lerp(1f, GrossissementSelection, _mises[i]);
                _lignes[i].localScale = new Vector3(echelle, echelle, 1f);
            }
        }

        private void Terminer()
        {
            _phase = Phase.Ferme;
            _groupe.alpha = 0f;
            _racine.SetActive(false);

            if (Validee != null)
            {
                Validee(_entreeValidee);
            }
        }

        /// <summary>Amorti cubique : rapide au départ, posé à l'arrivée.</summary>
        private static float Adoucir(float t)
        {
            float reste = 1f - t;
            return 1f - (reste * reste * reste);
        }
    }
}
