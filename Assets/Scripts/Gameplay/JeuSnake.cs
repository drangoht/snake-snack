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
    /// mort et la mise en page viennent tous de <c>Rules/</c>, testés sans moteur. Ce composant ne
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

            PoseInitiale pose = _grille.PoseDeDepart();
            _serpent = new Serpent(pose.Segments);
            _file = new FileEntrees(pose.Orientation, _reglages.profondeurFile);

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
            LireEntrees();
            AvancerLeTemps();
            RafraichirRetourDeRefus();
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
                BasculerLaPause();
            }

            if (clavier.spaceKey.wasPressedThisFrame && _etat == EtatPartie.Mort)
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

        /// <summary>Une direction tapée par le joueur, quel que soit l'état de la partie.</summary>
        private void Demander(Direction direction)
        {
            if (_etat == EtatPartie.Mort)
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

        private void JouerUnTick()
        {
            ResultatTick tick = _file.Tick();

            if (tick.DemiTourRefuse)
            {
                SignalerRefus(MotifRefus.DemiTour, tick.DirectionRefusee);
            }

            ResultatDeplacement resultat = _serpent.Avancer(tick.DirectionAppliquee, _grille);

            if (resultat != ResultatDeplacement.Avance)
            {
                Mourir();
                return;
            }

            _vue.DessinerSerpent(_serpent.Segments);
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

        private void NouvellePartie()
        {
            PoseInitiale pose = _grille.PoseDeDepart();

            _serpent.Reinitialiser(pose.Segments);
            _file.Reinitialiser(pose.Orientation);

            _tempsAccumule = 0.0;
            _etat = EtatPartie.EnAttente;

            _refusPictogramme.Eteindre();
            _refusTextePause.Eteindre();

            _vue.DessinerSerpent(_serpent.Segments);
            _vue.MasquerRefus();
            _hud.Afficher(_etat);
            _hud.AfficherRefusEnPause(false);
        }
    }
}
