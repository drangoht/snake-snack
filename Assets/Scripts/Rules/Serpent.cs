using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>Ce qu'un pas de serpent a produit (GDD §2 : « la tête touche le corps ou un mur »).</summary>
    public enum ResultatDeplacement
    {
        /// <summary>Le serpent a avancé d'une case.</summary>
        Avance,

        /// <summary>La tête est sortie de la grille : mort contre un mur (§2, les bords tuent).</summary>
        MortMur,

        /// <summary>La tête est entrée dans son propre corps : mort par morsure (§1, l'adversaire du jeu).</summary>
        MortMorsure
    }

    /// <summary>
    /// Le corps du serpent et son seul verbe : avancer d'une case (GDD §4.1).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Un déplacement mortel ne bouge pas le serpent.</b> La tête n'est jamais écrite hors de
    /// la grille ni dans son propre corps : l'état reste celui du dernier tick vivant. Sans ça, le
    /// rendu dessinerait une tête hors de l'aire de jeu pendant l'image de la mort — le joueur
    /// verrait le serpent traverser le mur, exactement ce que le §2 interdit de laisser croire.
    ///
    /// <para>⚠ <b>La queue libère sa case dans le même tick — sauf au tick d'une pomme.</b> Entrer
    /// dans la case que la queue quitte n'est <b>pas</b> une morsure : c'est la manœuvre normale
    /// d'un serpent qui suit sa propre trace, et la refuser tuerait sur un mouvement que le joueur
    /// voit se libérer à l'écran. Mais au tick où le serpent mange, la queue <b>ne bouge pas</b>
    /// (§4.4) : elle redevient un obstacle. L'exception a donc elle-même une exception, et c'est
    /// exactement l'endroit où se loge le bug d'une case.</para>
    ///
    /// <para>Classe à état, comme <see cref="FileEntrees"/>, et sans aucune dépendance moteur :
    /// c'est le seul critère qui compte pour <c>Rules/</c>.</para>
    /// </remarks>
    public sealed class Serpent
    {
        private readonly List<Case> _segments = new List<Case>();

        /// <param name="segments">
        /// Segments de la tête (index 0) vers la queue — typiquement
        /// <see cref="Grille.PoseDeDepart"/>.
        /// </param>
        public Serpent(IReadOnlyList<Case> segments)
        {
            Reinitialiser(segments);
        }

        /// <summary>Segments, de la tête (index 0) vers la queue.</summary>
        public IReadOnlyList<Case> Segments
        {
            get { return _segments; }
        }

        /// <summary>Case de la tête.</summary>
        public Case Tete
        {
            get { return _segments[0]; }
        }

        /// <summary>Nombre de segments.</summary>
        public int Longueur
        {
            get { return _segments.Count; }
        }

        /// <summary>Vrai si un segment occupe cette case (tête comprise).</summary>
        public bool Occupe(Case caseTestee)
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i] == caseTestee)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Repose le serpent, typiquement pour une nouvelle partie.</summary>
        public void Reinitialiser(IReadOnlyList<Case> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            if (segments.Count < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(segments), segments.Count, "Un serpent a au moins un segment.");
            }

            _segments.Clear();
            for (int i = 0; i < segments.Count; i++)
            {
                _segments.Add(segments[i]);
            }
        }

        /// <summary>
        /// Avance d'une case dans cette direction, sans pomme sur la grille.
        /// </summary>
        /// <remarks>
        /// Surcharge de confort pour les cas où la pomme n'entre pas en jeu (tests de mur et de
        /// morsure). Le jeu appelle toujours la forme complète : au §4.4, il y a une pomme sur la
        /// grille <b>à tout instant</b>.
        /// </remarks>
        public ResultatDeplacement Avancer(Direction direction, Grille grille)
        {
            bool ignore;
            return Avancer(direction, grille, null, out ignore);
        }

        /// <summary>
        /// Joue le tick du GDD §4.4 : avance d'une case, mange, grandit, ou meurt.
        /// </summary>
        /// <param name="direction">Direction déjà validée par <see cref="FileEntrees.Tick"/>.</param>
        /// <param name="grille">Aire de jeu — ses bords tuent (§2).</param>
        /// <param name="pomme">Case de la pomme, ou <c>null</c> s'il n'y en a pas.</param>
        /// <param name="mange">
        /// Vrai si la tête vient d'entrer sur la pomme. ⚠ <b>Toujours faux quand le serpent
        /// meurt</b> : un pas mortel ne mange pas, même vers la case de la pomme.
        /// </param>
        /// <remarks>
        /// La direction n'est <b>pas</b> validée ici : le demi-tour est jugé par
        /// <see cref="FileEntrees.Tick"/>, contre la direction effectivement appliquée au tick
        /// précédent (§4.2). Dupliquer ce jugement ici ferait exister deux vérités sur la même
        /// règle, et c'est la seconde qui finirait par diverger.
        ///
        /// <para>⚠ <b>L'ordre des étapes est celui du GDD §4.4, à la lettre</b> : mur, puis
        /// croissance, puis morsure, puis déplacement. Tester la morsure avant de savoir si le
        /// serpent mange ferait perdre l'exclusion de la queue — ou la garderait à tort. Les deux
        /// erreurs sont invisibles en lecture et évidentes à l'écran : une mort d'une case trop tôt,
        /// ou un serpent qui se traverse.</para>
        ///
        /// <para>⚠ Le serpent <b>s'allonge par la tête</b>, au tick même où elle entre sur la
        /// pomme — pas au tick suivant, pas par un segment ajouté derrière la queue. La longueur
        /// passe de N à N+1 immédiatement, et vaut toujours <c>3 + score</c> (§4.5).</para>
        /// </remarks>
        public ResultatDeplacement Avancer(Direction direction, Grille grille, Case? pomme, out bool mange)
        {
            mange = false;

            // 1 et 2 — la case visée, puis le mur.
            Case suivante = Directions.Avance(Tete, direction);

            if (grille.EstHorsGrille(suivante))
            {
                return ResultatDeplacement.MortMur;
            }

            // 3 — manger se décide AVANT la collision, parce que c'est ce qui décide du sort de la queue.
            bool croissance = pomme.HasValue && pomme.Value == suivante;

            // 4 — collision : queue exclue seulement si elle bouge, c'est-à-dire si on ne mange pas.
            //     ⚠ Écrit sans supposer qu'une pomme n'apparaît jamais sur le corps : cette garantie
            //     est posée à l'étape 6, ailleurs, et une règle ne doit pas dépendre d'une garantie
            //     qu'elle ne porte pas elle-même.
            int obstacles = croissance ? _segments.Count : _segments.Count - 1;
            for (int i = 0; i < obstacles; i++)
            {
                if (_segments[i] == suivante)
                {
                    return ResultatDeplacement.MortMorsure;
                }
            }

            // 5 — insérer la tête ; ne retirer la queue que si on ne mange pas. Dupliquer la queue
            //     avant le décalage revient exactement à « ne pas la retirer » : la boucle suivante
            //     est alors la même dans les deux cas, donc il n'existe qu'un seul chemin à relire.
            if (croissance)
            {
                _segments.Add(_segments[_segments.Count - 1]);
            }

            for (int i = _segments.Count - 1; i > 0; i--)
            {
                _segments[i] = _segments[i - 1];
            }

            _segments[0] = suivante;

            mange = croissance;
            return ResultatDeplacement.Avance;
        }
    }
}
