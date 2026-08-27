using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Où poser la pomme (GDD §4.4). Une seule sur la grille, jamais sur le serpent.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Cette classe répond « où » et « combien », jamais « quand »</b> (GDD §4.4) : la
    /// résolution du tick — l'ordre exact entre mur, morsure, croissance et nouveau tirage —
    /// appartient à <see cref="Serpent"/>. Deux endroits qui décident du même enchaînement, c'est un
    /// bug d'une case qui n'apparaît qu'à l'écran.
    ///
    /// <para>⚠ <b>Le tirage énumère, il ne rejette pas.</b> « Tirer une case au hasard et
    /// recommencer tant qu'elle est occupée » est le piège de ce système : sur une grille presque
    /// pleine, l'espérance du nombre de tirages tend vers l'infini et le jeu <b>se fige sans lever
    /// la moindre erreur</b> — pas d'exception, pas de log, juste une image qui ne revient pas. Et
    /// le défaut n'apparaît qu'en fin de partie longue, c'est-à-dire jamais pendant les tests. Ici
    /// le coût est <b>borné</b> : un seul parcours de la grille, quel que soit le remplissage.</para>
    /// </remarks>
    public static class Pomme
    {
        /// <summary>
        /// Nombre de cases où la pomme peut tomber.
        /// </summary>
        /// <remarks>
        /// Le serpent occupe <b>exactement</b> <paramref name="longueurSerpent"/> cases distinctes :
        /// deux segments superposés signifieraient qu'il s'est mordu, donc que la partie est finie
        /// (GDD §4.4). La soustraction est donc juste, sans avoir à parcourir le corps.
        /// </remarks>
        public static int NombreDeCasesLibres(Grille grille, int longueurSerpent)
        {
            if (longueurSerpent < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(longueurSerpent), longueurSerpent, "Un serpent n'a pas une longueur négative.");
            }

            if (longueurSerpent > grille.NombreDeCases)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(longueurSerpent), longueurSerpent,
                    "Le serpent ne peut pas occuper plus de cases que la grille n'en contient.");
            }

            return grille.NombreDeCases - longueurSerpent;
        }

        /// <summary>
        /// Vrai si le serpent remplit la grille : <b>c'est la victoire</b> (GDD §4.4), pas une erreur.
        /// </summary>
        /// <remarks>
        /// ⚠ À tester <b>avant</b> <see cref="Tirer"/>, jamais après : sans case libre, le tirage
        /// n'a aucune valeur à rendre. Cet état est hors de portée humaine (312 pommes sur la grille
        /// par défaut) et doit néanmoins être écrit — c'est exactement le genre de branche qu'on
        /// n'écrit pas « parce qu'elle n'arrivera jamais », et qui casse le jour où un banc
        /// automatisé joue une partie parfaite.
        /// </remarks>
        public static bool GrillePleine(Grille grille, int longueurSerpent)
        {
            return NombreDeCasesLibres(grille, longueurSerpent) == 0;
        }

        /// <summary>
        /// La <paramref name="rang"/>-ième case libre, en parcourant <b>X croissant dans Y
        /// croissant</b> (GDD §4.4).
        /// </summary>
        /// <param name="grille">Aire de jeu.</param>
        /// <param name="casesOccupees">Segments du serpent — leur ordre n'a aucune importance ici.</param>
        /// <param name="rang">Rang de la case voulue, dans <c>[0, NombreDeCasesLibres)</c>.</param>
        /// <remarks>
        /// ⚠ <b>L'ordre de parcours fait partie du contrat</b>, ce n'est pas un détail
        /// d'implémentation : c'est lui qui, à graine égale, donne la même partie sur les trois
        /// cibles. L'inverser (Y dans X) ne casserait aucun test d'uniformité et casserait tout
        /// appariement de banc.
        ///
        /// <para>Cette méthode est <b>séparée du tirage</b> pour être testable sans générateur : on
        /// lui donne un rang, elle rend une case, et l'assertion porte sur une valeur exacte.</para>
        /// </remarks>
        public static Case CaseLibreAuRang(Grille grille, IReadOnlyList<Case> casesOccupees, int rang)
        {
            if (casesOccupees == null)
            {
                throw new ArgumentNullException(nameof(casesOccupees));
            }

            int libres = NombreDeCasesLibres(grille, casesOccupees.Count);

            if (rang < 0 || rang >= libres)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rang), rang,
                    "Il n'y a que " + libres + " case(s) libre(s) : le rang doit tomber dedans.");
            }

            int restantes = rang;

            for (int y = 0; y < grille.Hauteur; y++)
            {
                for (int x = 0; x < grille.Largeur; x++)
                {
                    Case candidate = new Case(x, y);

                    if (EstOccupee(casesOccupees, candidate))
                    {
                        continue;
                    }

                    if (restantes == 0)
                    {
                        return candidate;
                    }

                    restantes--;
                }
            }

            // Inatteignable tant que `casesOccupees` tient dans la grille et ne contient pas de
            // doublon — les deux conditions du §4.4. Levée plutôt que rendue en silence : une pomme
            // posée « quelque part » serait indétectable à la lecture et évidente à l'écran.
            throw new InvalidOperationException(
                "Les cases occupées débordent de la grille ou contiennent un doublon : le décompte des cases libres est faux.");
        }

        /// <summary>
        /// Tire la case de la prochaine pomme (GDD §4.4).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// La grille est pleine. L'appelant doit avoir traité la victoire avec
        /// <see cref="GrillePleine"/> avant d'arriver ici.
        /// </exception>
        /// <remarks>
        /// ⚠ <b>Aucune contrainte de placement</b> (§4.4) : pas de distance minimale à la tête, pas
        /// d'interdiction dans son prolongement immédiat. Manger n'est jamais obligatoire, donc
        /// aucune position ne peut nuire — contraindre ne retirerait au joueur que des pommes
        /// <i>favorables</i>, tout en rendant chaque banc plus difficile à décrire.
        /// </remarks>
        public static Case Tirer(Grille grille, IReadOnlyList<Case> casesOccupees, Aleatoire alea)
        {
            if (casesOccupees == null)
            {
                throw new ArgumentNullException(nameof(casesOccupees));
            }

            if (alea == null)
            {
                throw new ArgumentNullException(nameof(alea));
            }

            int libres = NombreDeCasesLibres(grille, casesOccupees.Count);

            if (libres == 0)
            {
                throw new InvalidOperationException(
                    "Aucune case libre : la grille pleine est une victoire (GDD §4.4), elle se traite avant le tirage.");
            }

            return CaseLibreAuRang(grille, casesOccupees, alea.Entier(libres));
        }

        /// <summary>
        /// Balayage linéaire, sans allocation ni table intermédiaire.
        /// </summary>
        /// <remarks>
        /// Le coût d'un tirage complet est donc au pire <c>NombreDeCases × longueur</c> comparaisons
        /// d'entiers — 315 × 315 sur la grille par défaut, et seulement dans la position où le
        /// serpent remplit tout. Ce coût n'est payé <b>qu'aux ticks où une pomme est mangée</b>,
        /// jamais aux autres. Construire un <c>HashSet</c> ici coûterait une allocation par pomme,
        /// donc un ramassage de mémoire régulier — visible en WebGL sous forme de micro-saccades, et
        /// une saccade décale la lecture d'un virage.
        /// </remarks>
        private static bool EstOccupee(IReadOnlyList<Case> casesOccupees, Case candidate)
        {
            for (int i = 0; i < casesOccupees.Count; i++)
            {
                if (casesOccupees[i] == candidate)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
