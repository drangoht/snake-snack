using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Le générateur pseudo-aléatoire du jeu : semé par un entier, <b>reproductible partout</b>
    /// (GDD §4.4, « Aléa reproductible »).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Ce type existe parce qu'aucun générateur de la plateforme ne convient</b>, et la raison
    /// n'est pas une préférence de style :
    /// <list type="bullet">
    /// <item><c>UnityEngine.Random</c> est un <b>état global partagé</b> — n'importe quel effet
    /// visuel qui y puiserait décalerait la suite des pommes — et il est de toute façon indisponible
    /// dans <c>Rules/</c>, qui ne dépend d'aucun moteur.</item>
    /// <item>La suite de <c>System.Random</c> <b>n'est pas contractuellement stable</b> d'un runtime
    /// à l'autre : .NET Core 2.0 puis .NET 6 en ont changé l'algorithme. Un banc dont les pommes
    /// diffèrent entre <c>dotnet test</c>, le build bureau (Mono/IL2CPP) et le build WebGL ne
    /// compare plus rien — et il ne lèverait aucune erreur pour le dire.</item>
    /// </list>
    ///
    /// <para>L'algorithme est <b>SplitMix64</b> : quatre lignes d'arithmétique sur 64 bits non
    /// signés, dont le résultat ne dépend que du langage — <c>unchecked</c>, décalages logiques,
    /// multiplication modulo 2^64 sont définis à l'identique par toute implémentation C#. C'est ce
    /// qui rend la suite identique sur les trois cibles. Il n'a pas à être cryptographique : il doit
    /// être <i>uniforme</i> et <i>rejouable</i>, rien de plus.</para>
    ///
    /// <para>⚠ <b>Rien d'autre que la pomme ne tire dans l'instance de la partie</b> (GDD §4.4). Un
    /// besoin cosmétique ou audio prend sa <b>propre</b> instance : puiser dans celle-là décalerait
    /// toute la suite des pommes sans qu'aucun test ne tombe.</para>
    /// </remarks>
    public sealed class Aleatoire
    {
        // Constantes de SplitMix64 (Steele, Lea & Flood, 2014). Le pas d'or 2^64/φ étale les graines
        // voisines : semer 1 puis 2 ne donne pas deux suites qui se ressemblent — ce qui compte, vu
        // que les graines de banc seront écrites à la main et se suivront.
        private const ulong PasDOr = 0x9E3779B97F4A7C15UL;
        private const ulong Melange1 = 0xBF58476D1CE4E5B9UL;
        private const ulong Melange2 = 0x94D049BB133111EBUL;

        private ulong _etat;

        /// <param name="graine">
        /// Semence de la suite. Deux instances semées à la même valeur produisent <b>exactement</b>
        /// la même suite, sur n'importe quelle cible.
        /// </param>
        public Aleatoire(ulong graine)
        {
            Graine = graine;
            _etat = graine;
        }

        /// <summary>La graine reçue à la construction — à journaliser pour rejouer la partie.</summary>
        public ulong Graine { get; }

        /// <summary>Repart de la graine d'origine : la suite recommence à l'identique.</summary>
        public void Reinitialiser()
        {
            _etat = Graine;
        }

        /// <summary>Le prochain entier 64 bits de la suite, uniforme sur toute la plage.</summary>
        public ulong Suivant()
        {
            unchecked
            {
                _etat += PasDOr;
                ulong z = _etat;
                z = (z ^ (z >> 30)) * Melange1;
                z = (z ^ (z >> 27)) * Melange2;
                return z ^ (z >> 31);
            }
        }

        /// <summary>
        /// Un entier uniforme dans <c>[0, borneExclue)</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="borneExclue"/> n'est pas strictement positif — il n'existe alors aucune
        /// valeur à rendre, et rendre 0 « par défaut » poserait la pomme sur la case (0, 0).
        /// </exception>
        /// <remarks>
        /// ⚠ <b>Ce rejet-ci n'est PAS celui que le GDD §4.4 proscrit.</b> Le piège écarté est
        /// « tirer une case au hasard, retirer tant qu'elle est occupée » : sur une grille presque
        /// pleine, son espérance tend vers l'infini et le jeu se fige en silence. Ici on rejette la
        /// <b>frange non divisible</b> de 2^64, dont la taille est au plus <c>borneExclue</c> : pour
        /// une grille de 315 cases, la probabilité de reboucler vaut moins de 2·10⁻¹⁷ par tirage.
        /// Sans ce rejet, un simple <c>% borne</c> favoriserait les petites valeurs — donc, ici, le
        /// coin haut-gauche de la grille.
        /// </remarks>
        public int Entier(int borneExclue)
        {
            if (borneExclue <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(borneExclue), borneExclue,
                    "Il faut au moins une valeur possible pour en tirer une.");
            }

            ulong borne = (ulong)borneExclue;

            // 2^64 mod borne, écrit sans jamais représenter 2^64 : c'est la taille de la frange qui
            // déborde du dernier multiple entier de `borne`.
            ulong seuil = (ulong.MaxValue - borne + 1) % borne;

            ulong tirage;
            do
            {
                tirage = Suivant();
            }
            while (tirage < seuil);

            return (int)(tirage % borne);
        }
    }
}
