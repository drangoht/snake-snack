using System;
using SnakeSnack.Core;
using SnakeSnack.Rules;
using SnakeSnack.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// La boucle de jeu : lit les entrées, fait tiquer la cadence, et rend l'état à l'écran.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Aucune règle n'est décidée ici.</b> La cadence, la file, le demi-tour, le démarrage, la
    /// mort, le score et la mise en page viennent tous de <c>Rules/</c>, testés sans moteur. Ce composant ne
    /// fait que trois choses qu'une classe pure ne peut pas faire : lire un clavier, mesurer le
    /// temps, poser des objets à l'écran. Toute règle qui remonterait ici deviendrait une seconde
    /// vérité, et c'est elle qui finirait par diverger.
    ///
    /// <para>⚠ <b>Les échéances du retour visuel utilisent le temps non mis à l'échelle</b>
    /// (<c>unscaledTime</c>) : le message « touche ignorée » de l'écran de pause doit s'afficher
    /// puis s'éteindre <i>pendant la pause</i>, c'est-à-dire à un moment où le temps de jeu peut
    /// être arrêté. Avec <c>Time.time</c>, ce message resterait figé à l'écran.</para>
    /// </remarks>
    public sealed class JeuSnake : MonoBehaviour
    {
        // ⚠ AZERTY : Key.W est la touche marquée Z, Key.A la touche marquée Q (voir Pressee).
        private static readonly Key[] ToucheHaut = { Key.UpArrow, Key.W };
        private static readonly Key[] ToucheBas = { Key.DownArrow, Key.S };
        private static readonly Key[] ToucheGauche = { Key.LeftArrow, Key.A };
        private static readonly Key[] ToucheDroite = { Key.RightArrow, Key.D };

        private ReglagesJeu _reglages;
        private Grille _grille;
        private Plateau _plateau;
        private Serpent _serpent;
        private FileEntrees _file;
        private VuePlateau _vue;
        private HudJeu _hud;

        /// <summary>
        /// Le menu principal (GDD §4.6). ⚠ C'est LUI qui dit s'il occupe l'écran
        /// (<see cref="EcranMenu.Actif"/>) : dupliquer ici un booléen « on est dans le menu »
        /// créerait deux vérités, et c'est celle du fondu de sortie qui finirait par diverger.
        /// </summary>
        private EcranMenu _menu;

        /// <summary>Le générateur de la partie en cours. ⚠ Rien d'autre que la pomme n'y tire (§4.4).</summary>
        private Aleatoire _alea;

        /// <summary>
        /// Le générateur qui fabrique les graines des parties, quand aucune n'est fixée.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Instance séparée, et c'est une règle du §4.4</b> : puiser une graine dans
        /// <see cref="_alea"/> décalerait la suite des pommes de la partie en cours. Il sert aussi à
        /// éviter un piège plus discret — deux parties relancées coup sur coup tireraient la même
        /// graine si celle-ci venait directement de l'horloge, dont la résolution réelle sous Windows
        /// est d'environ 15 ms. Le joueur qui appuie deux fois sur Espace rejouerait alors les mêmes
        /// pommes, sans que rien ne l'explique.
        /// </remarks>
        private Aleatoire _grainesDeSession;

        /// <summary>Case de la pomme. Il y en a une <b>à tout instant</b> pendant une partie (§4.4).</summary>
        private Case _pomme;

        /// <summary>
        /// Score de la partie et record de toutes les parties (§4.5).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Construit une seule fois, à l'ouverture du jeu</b>, et jamais remplacé à la relance :
        /// c'est lui qui porte le record entre deux parties. Le remplacer par un neuf à chaque
        /// nouvelle partie relirait le stockage à chaque mort — et surtout, ferait repartir le
        /// record à sa valeur écrite, perdant celui d'une partie en cours dont l'écriture aurait
        /// échoué.
        /// </remarks>
        private Score _score;

        private EtatRetourAEcheance _refusPictogramme;
        private EtatRetourAEcheance _refusTextePause;
        private Direction _directionRefusee;

        private EtatPartie _etat;
        private double _tempsAccumule;
        private double _dureeTick;

        private void Awake()
        {
            _reglages = ChargeurReglages.Charger();

            _grille = new Grille(_reglages.largeurGrille, _reglages.hauteurGrille);
            _plateau = new Plateau(_grille, Plateau.TailleDeCase(_grille));
            _dureeTick = Cadence.DureeTickSecondes(_reglages.ticksParSeconde);

            _refusPictogramme = new EtatRetourAEcheance(
                _reglages.dureeAffichageRefusSecondes,
                _reglages.plafondProlongationRefusSecondes,
                _reglages.dureeFonduRefusSecondes);

            _refusTextePause = new EtatRetourAEcheance(
                _reglages.dureeTextePauseSecondes,
                _reglages.dureeTextePauseSecondes,
                _reglages.dureeFonduRefusSecondes);

            _vue = gameObject.AddComponent<VuePlateau>();
            _vue.Construire(_plateau);

            _hud = gameObject.AddComponent<HudJeu>();

            // Le record vient du stockage du moteur ; tout ce qui se décide à son sujet appartient
            // à Score (§4.5). Un record absent ou abîmé vaut zéro et ne bloque rien.
            _score = new Score(RecordPersistant.Lire());

            // La graine de session vient de l'horloge : c'est le seul aléa non reproductible du jeu,
            // et il ne sert qu'à ce que deux sessions ne commencent pas sur les mêmes pommes.
            _grainesDeSession = new Aleatoire((ulong)DateTime.UtcNow.Ticks);

            PoseInitiale pose = _grille.PoseDeDepart();
            _serpent = new Serpent(pose.Segments);
            _file = new FileEntrees(pose.Orientation, _reglages.profondeurFile);

            _menu = gameObject.AddComponent<EcranMenu>();
            _menu.Validee += SurEntreeDeMenuValidee;

            // ⚠ Aucune partie n'est préparée ici : `NouvellePartie` sème l'aléatoire et journalise
            // la graine (§4.4). L'appeler au démarrage puis à nouveau sur « Jouer » écrirait deux
            // graines dans le journal pour une seule partie jouée, et c'est la première — celle
            // qu'on ne joue pas — qui serait lue en cas de rapport de bug.
            RevenirAuMenu();
        }

        /// <summary>Le menu prend l'écran : le plateau et le HUD s'effacent d'un bloc.</summary>
        private void RevenirAuMenu()
        {
            _vue.Montrer(false);
            _hud.Montrer(false);
            _menu.Ouvrir();
        }

        /// <summary>
        /// Une entrée du menu qui engage l'application a été validée, fondu de sortie terminé.
        /// </summary>
        /// <remarks>
        /// « Comment jouer » et « Crédits » n'arrivent jamais ici : ce sont des panneaux, et
        /// <see cref="EcranMenu"/> les gère sans quitter le menu.
        /// </remarks>
        private void SurEntreeDeMenuValidee(EntreeMenu entree)
        {
            if (entree == EntreeMenu.Quitter)
            {
                // ⚠ Sans effet dans l'éditeur ET en WebGL — d'où l'absence de l'entrée sur le web
                // (MenuPrincipal.Entrees). Sur le bureau, c'est bien le jeu qui se ferme.
                Application.Quit();
                return;
            }

            _vue.Montrer(true);
            _hud.Montrer(true);
            NouvellePartie();
        }

        private void OnEnable()
        {
            Application.focusChanged += SurChangementDeFocus;
        }

        private void OnDisable()
        {
            Application.focusChanged -= SurChangementDeFocus;
        }

        /// <summary>
        /// ⚠ <b>Dépendance du plafond de rattrapage</b> (GDD §4.1), pas un confort : le plafond jette
        /// le retard accumulé. Sans cette pause, tout le temps passé hors de la fenêtre serait perdu
        /// pour le joueur — il reviendrait sur un serpent qui n'a pas avancé, ou pire, qui a avancé.
        /// </summary>
        private void SurChangementDeFocus(bool aLeFocus)
        {
            if (!aLeFocus && _etat == EtatPartie.EnCours)
            {
                MettreEnPause();
            }
        }

        private void Update()
        {
            if (_menu.Actif)
            {
                // ⚠ Rien du jeu ne tourne tant que le menu est là, fondu de sortie compris : une
                // direction tapée sur les dernières images du fondu se retrouverait empilée dans la
                // file d'entrées et ferait démarrer la partie toute seule (§4.2).
                LireEntreesDuMenu();
                return;
            }

            LireEntrees();
            AvancerLeTemps();
            RafraichirRetourDeRefus();
        }

        /// <summary>
        /// Les touches du menu. Mêmes flèches et mêmes ZQSD que le jeu (GDD §3) : le joueur n'a pas
        /// deux jeux de commandes à apprendre, et l'AZERTY se déclare ici aussi en <c>Key.W</c>.
        /// </summary>
        private void LireEntreesDuMenu()
        {
            Keyboard clavier = Keyboard.current;
            if (clavier == null)
            {
                return;
            }

            if (clavier.escapeKey.wasPressedThisFrame)
            {
                _menu.Retour();
            }

            if (clavier.enterKey.wasPressedThisFrame
                || clavier.numpadEnterKey.wasPressedThisFrame
                || clavier.spaceKey.wasPressedThisFrame)
            {
                _menu.Valider();
            }

            if (Pressee(clavier, ToucheHaut))
            {
                _menu.Deplacer(Direction.Nord);
            }

            if (Pressee(clavier, ToucheBas))
            {
                _menu.Deplacer(Direction.Sud);
            }

            if (Pressee(clavier, ToucheGauche))
            {
                _menu.Deplacer(Direction.Ouest);
            }

            if (Pressee(clavier, ToucheDroite))
            {
                _menu.Deplacer(Direction.Est);
            }
        }

        private void LireEntrees()
        {
            Keyboard clavier = Keyboard.current;
            if (clavier == null)
            {
                return;
            }

            if (clavier.escapeKey.wasPressedThisFrame)
            {
                if (PartieTerminee)
                {
                    // Le GDD §2 garde la relance à une touche (Espace, zéro attente) : Échap n'est
                    // le chemin du menu QUE sur l'écran de fin, là où plus rien ne se joue.
                    RevenirAuMenu();
                    return;
                }

                BasculerLaPause();
            }

            if (clavier.spaceKey.wasPressedThisFrame && PartieTerminee)
            {
                NouvellePartie();
            }

            if (Pressee(clavier, ToucheHaut))
            {
                Demander(Direction.Nord);
            }

            if (Pressee(clavier, ToucheBas))
            {
                Demander(Direction.Sud);
            }

            if (Pressee(clavier, ToucheGauche))
            {
                Demander(Direction.Ouest);
            }

            if (Pressee(clavier, ToucheDroite))
            {
                Demander(Direction.Est);
            }
        }

        /// <summary>
        /// ⚠ <b>AZERTY</b> (CLAUDE.md) : <c>Key</c> désigne une <i>position</i> sur un clavier
        /// QWERTY. Les touches marquées Z, Q, S, D sur un clavier français se déclarent donc
        /// <c>Key.W</c>, <c>Key.A</c>, <c>Key.S</c>, <c>Key.D</c>. Rien n'est levé en cas d'erreur :
        /// le jeu répond simplement à la mauvaise touche.
        /// </summary>
        private static bool Pressee(Keyboard clavier, Key[] touches)
        {
            for (int i = 0; i < touches.Length; i++)
            {
                if (clavier[touches[i]].wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fin de partie, mort ou victoire : même écran, même place, même relance à une touche
        /// (§4.4). Les deux états ne se distinguent que par leur libellé.
        /// </summary>
        private bool PartieTerminee
        {
            get { return _etat == EtatPartie.Mort || _etat == EtatPartie.Victoire; }
        }

        /// <summary>Une direction tapée par le joueur, quel que soit l'état de la partie.</summary>
        private void Demander(Direction direction)
        {
            if (PartieTerminee)
            {
                // Seul Espace relance (§2) : une direction ne doit pas redémarrer par surprise.
                return;
            }

            if (_etat == EtatPartie.EnAttente)
            {
                if (Demarrage.Decider(_grille.PoseDeDepart().Orientation, direction) == DecisionDemarrage.RefuseDemiTour)
                {
                    SignalerRefus(MotifRefus.DemiTour, direction);
                    return;
                }

                _etat = EtatPartie.EnCours;
                _hud.Afficher(_etat);
            }

            ResultatEmpilage resultat = _file.Empiler(direction);

            MotifRefus motif;
            if (RoutageRefus.DepuisEmpilage(resultat, out motif))
            {
                SignalerRefus(motif, direction);
            }
        }

        private void AvancerLeTemps()
        {
            if (_etat != EtatPartie.EnCours)
            {
                return;
            }

            _tempsAccumule += Time.deltaTime;

            int ticks = Cadence.NombreDeTicks(
                _tempsAccumule, _dureeTick, out _tempsAccumule, _reglages.plafondDeRattrapage);

            for (int i = 0; i < ticks; i++)
            {
                JouerUnTick();

                if (_etat != EtatPartie.EnCours)
                {
                    // Mort pendant la rafale : les ticks restants n'ont plus de sens.
                    return;
                }
            }
        }

        /// <summary>
        /// Un tick, dans l'ordre exact du GDD §4.4.
        /// </summary>
        /// <remarks>
        /// Les étapes 1 à 5 (direction, mur, morsure, déplacement, croissance) appartiennent à
        /// <see cref="Serpent.Avancer(Direction, Grille, Case?, out bool)"/> ; seule l'étape 6 —
        /// remplacer la pomme, ou constater la grille pleine — vit ici, parce qu'elle touche à
        /// l'état de la partie et au rendu.
        /// </remarks>
        private void JouerUnTick()
        {
            ResultatTick tick = _file.Tick();

            if (tick.DemiTourRefuse)
            {
                SignalerRefus(MotifRefus.DemiTour, tick.DirectionRefusee);
            }

            bool mange;
            ResultatDeplacement resultat = _serpent.Avancer(tick.DirectionAppliquee, _grille, _pomme, out mange);

            if (resultat != ResultatDeplacement.Avance)
            {
                Mourir();
                return;
            }

            _vue.DessinerSerpent(_serpent.Segments);

            if (!mange)
            {
                return;
            }

            // Étape 6 — le score d'abord (§4.4 : « score +1, puis tirer la nouvelle pomme »). Compté
            // même quand cette pomme est celle qui remplit la grille : elle a été mangée, l'écran de
            // victoire doit afficher le score qui l'inclut.
            if (_score.CompterUnePomme())
            {
                // ⚠ Écrit ICI, au tick où le record monte, et pas à la mort : le §4.5 veut que le
                // record survive à un onglet fermé en cours de partie. Le signal rendu par
                // CompterUnePomme évite d'écrire le stockage à chaque pomme de chaque partie.
                RecordPersistant.Ecrire(_score.Record);
            }

            _hud.AfficherScore(_score.Points, _score.Record, _score.RecordBattu);

            // La victoire se teste AVANT le tirage : sans case libre, le tirage n'a aucune valeur à
            // rendre et lèverait, au dernier tick de la partie parfaite.
            if (Pomme.GrillePleine(_grille, _serpent.Longueur))
            {
                Gagner();
                return;
            }

            // ⚠ Tirée sur l'état FINAL du tick (§4.4) : le serpent vient de s'allonger, et une
            // pomme placée avant cette croissance pourrait tomber sur la case que la tête occupe.
            // Elle est posée dans le tick même où l'ancienne a été mangée — aucune image ne
            // s'affiche sans pomme, une grille vide se lisant comme un bug et non comme une
            // transition.
            _pomme = Pomme.Tirer(_grille, _serpent.Segments, _alea);
            _vue.DessinerPomme(_pomme);
        }

        /// <summary>Route un refus vers son registre visuel (<c>docs/ART.md</c> §5.2).</summary>
        private void SignalerRefus(MotifRefus motif, Direction direction)
        {
            switch (RoutageRefus.Registre(motif))
            {
                case RegistreRefus.Pictogramme:
                    // ⚠ La direction est mise à jour même quand la notification ne fait que
                    // prolonger : le joueur doit voir le refus qu'il vient de taper, pas le
                    // précédent. Seule l'animation d'apparition n'est pas relancée (ART §5.5).
                    _directionRefusee = direction;
                    _refusPictogramme.Notifier(Time.unscaledTimeAsDouble);
                    break;

                case RegistreRefus.TextePause:
                    _refusTextePause.Notifier(Time.unscaledTimeAsDouble);
                    break;

                default:
                    // Doublon : aucun retour, et le silence est ici une décision (ART §5.3).
                    break;
            }
        }

        private void RafraichirRetourDeRefus()
        {
            double maintenant = Time.unscaledTimeAsDouble;

            if (_refusPictogramme.EstVisible(maintenant))
            {
                _vue.AfficherRefus(_serpent.Tete, _directionRefusee, (float)_refusPictogramme.Opacite(maintenant));
            }
            else
            {
                _vue.MasquerRefus();
            }

            _hud.AfficherRefusEnPause(_refusTextePause.EstVisible(maintenant));
        }

        private void BasculerLaPause()
        {
            if (_etat == EtatPartie.EnCours)
            {
                MettreEnPause();
            }
            else if (_etat == EtatPartie.EnPause)
            {
                _file.Reprendre();
                _etat = EtatPartie.EnCours;
                _hud.Afficher(_etat);
            }
        }

        private void MettreEnPause()
        {
            _file.EntrerEnPause();
            _etat = EtatPartie.EnPause;
            _hud.Afficher(_etat);

            // Le temps accumulé est jeté : reprendre ne doit pas déclencher un tick immédiat.
            _tempsAccumule = 0.0;
        }

        private void Mourir()
        {
            _file.Mourir();
            _etat = EtatPartie.Mort;
            _hud.Afficher(_etat);
            _vue.DessinerSerpent(_serpent.Segments);
        }

        /// <summary>
        /// Grille pleine (GDD §4.4). Hors de portée humaine, écrit quand même.
        /// </summary>
        /// <remarks>
        /// ⚠ La pomme est <b>masquée</b> ici, et c'est le seul endroit du jeu où elle disparaît :
        /// il n'existe plus une seule case libre où la poser. La laisser affichée montrerait une
        /// pomme posée sur le serpent.
        /// </remarks>
        private void Gagner()
        {
            // Même purge que la mort : la partie est finie, aucun virage tapé après ne doit
            // survivre jusqu'à la partie suivante.
            _file.Mourir();
            _etat = EtatPartie.Victoire;
            _hud.Afficher(_etat);
            _vue.MasquerPomme();
        }

        private void NouvellePartie()
        {
            PoseInitiale pose = _grille.PoseDeDepart();

            _serpent.Reinitialiser(pose.Segments);
            _file.Reinitialiser(pose.Orientation);

            // Le score repart de zéro, le record survit — et « record battu » redevient faux, sans
            // quoi la mention resterait affichée sur l'écran de fin de la partie suivante.
            _score.NouvellePartie();

            _tempsAccumule = 0.0;
            _etat = EtatPartie.EnAttente;

            _refusPictogramme.Eteindre();
            _refusTextePause.Eteindre();

            SemerLAleatoire();

            // ⚠ La pomme est posée AVANT le premier appui (§4.4) : le départ est à l'arrêt, le
            // joueur regarde l'écran et choisit sa direction. Sans pomme à viser, ce choix serait
            // aveugle.
            _pomme = Pomme.Tirer(_grille, _serpent.Segments, _alea);

            _vue.DessinerSerpent(_serpent.Segments);
            _vue.DessinerPomme(_pomme);
            _vue.MasquerRefus();

            // ⚠ Les nombres AVANT l'état : Afficher() compose le récapitulatif de fin à partir des
            // derniers nombres reçus. Dans l'autre ordre, l'écran de mort porterait le score de la
            // partie précédente pendant une image.
            _hud.AfficherScore(_score.Points, _score.Record, _score.RecordBattu);
            _hud.Afficher(_etat);
            _hud.AfficherRefusEnPause(false);
        }

        /// <summary>
        /// Donne à la partie son générateur de pommes (GDD §4.4, « Aléa reproductible »).
        /// </summary>
        /// <remarks>
        /// Graine fixée dans le JSON de tuning : <b>toutes</b> les parties rejouent la même suite de
        /// pommes — c'est le mode banc, pas un mode de jeu. Graine laissée à zéro : chaque partie en
        /// reçoit une neuve.
        ///
        /// <para>⚠ <b>Journalisée à chaque partie, y compris quand elle vient de la session</b> :
        /// une graine non écrite quelque part rend la partie irrejouable, et c'est précisément la
        /// partie remarquable — celle qu'on veut rejouer — qui serait perdue.</para>
        /// </remarks>
        private void SemerLAleatoire()
        {
            ulong graine = _reglages.graine != ReglagesJeu.GraineDeLHorloge
                ? (ulong)_reglages.graine
                : _grainesDeSession.Suivant();

            _alea = new Aleatoire(graine);
            Debug.Log("[pomme] graine de la partie : " + graine);
        }
    }
}
