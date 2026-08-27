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
    /// <para>⚠ <b>La queue libère sa case dans le même tick.</b> Entrer dans la case que la queue
    /// quitte n'est <b>pas</b> une morsure : c'est la manœuvre normale d'un serpent qui suit sa
    /// propre trace. Compter la queue parmi les obstacles produit une mort inexplicable pour le
    /// joueur, sur une case qu'il a vue se vider. Ce cas n'a pas de contre-partie : il n'y a pas de
    /// pomme dans ce lot, donc pas de croissance, donc la queue avance toujours.</para>
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
        /// Avance d'une case dans cette direction, ou meurt (GDD §2).
        /// </summary>
        /// <remarks>
        /// La direction n'est <b>pas</b> validée ici : le demi-tour est jugé par
        /// <see cref="FileEntrees.Tick"/>, contre la direction effectivement appliquée au tick
        /// précédent (§4.2). Dupliquer ce jugement ici ferait exister deux vérités sur la même
        /// règle, et c'est la seconde qui finirait par diverger.
        /// </remarks>
        public ResultatDeplacement Avancer(Direction direction, Grille grille)
        {
            Case suivante = Directions.Avance(Tete, direction);

            if (grille.EstHorsGrille(suivante))
            {
                return ResultatDeplacement.MortMur;
            }

            // Tous les segments SAUF le dernier : celui-là libère sa case pendant ce même tick.
            int indexQueue = _segments.Count - 1;
            for (int i = 0; i < indexQueue; i++)
            {
                if (_segments[i] == suivante)
                {
                    return ResultatDeplacement.MortMorsure;
                }
            }

            for (int i = indexQueue; i > 0; i--)
            {
                _segments[i] = _segments[i - 1];
            }

            _segments[0] = suivante;
            return ResultatDeplacement.Avance;
        }
    }
}
