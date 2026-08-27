using UnityEngine;

namespace SnakeSnack.UI
{
    /// <summary>
    /// ⚠ <b>LE SEUL ENDROIT DU DÉPÔT OÙ UNE COULEUR EST ÉCRITE EN DUR — ET IL EST PROVISOIRE.</b>
    /// </summary>
    /// <remarks>
    /// <c>docs/ART.md</c> §1 (Palette) est <b>vide</b> au 2026-08-27 : aucun code hexa n'est décidé,
    /// et le §5.6 demande explicitement de « construire en niveaux de gris / silhouettes » en
    /// attendant. Ce fichier tient ce rôle, et rien d'autre.
    ///
    /// <para><b>Ce qu'il faut en faire quand la palette existera</b> : renommer ce type en
    /// <c>UiPalette</c>, y poser les couleurs tranchées, et supprimer cette remarque. Aucun appelant
    /// n'a à changer — c'est tout l'intérêt d'avoir centralisé.</para>
    ///
    /// <para>⚠ <b>Ne rien ajouter d'autre ici qu'une valeur de gris.</b> Une couleur posée en
    /// avance-de-phase deviendrait une décision artistique prise par le développeur, exactement ce
    /// que le brief interdit. Et l'ART §4 impose de toute façon que jamais une information ne soit
    /// portée par la seule couleur : tout ce qui se distingue ici se distingue AUSSI par la forme ou
    /// la position (la tête est plus grosse que le corps, le pictogramme est barré, le bord de
    /// l'aire est un trait continu).</para>
    /// </remarks>
    public static class PaletteProvisoire
    {
        /// <summary>Fond du cadre, hors aire de jeu — les marges latérales du GDD §4.3.</summary>
        public static readonly Color Fond = Gris(0.07f);

        /// <summary>Fond de l'aire de jeu : légèrement plus clair, pour que l'aire se détache.</summary>
        public static readonly Color AireDeJeu = Gris(0.13f);

        /// <summary>Traits de la grille : présents mais discrets — ils aident à compter, pas à lire.</summary>
        public static readonly Color TraitDeGrille = Gris(0.20f);

        /// <summary>
        /// Bordure de l'aire de jeu. <b>Nettement plus claire que tout le reste du décor</b> : c'est
        /// le mur qui tue (GDD §2), il doit se lire sans effort.
        /// </summary>
        public static readonly Color BordureAire = Gris(0.62f);

        /// <summary>Corps du serpent.</summary>
        public static readonly Color CorpsSerpent = Gris(0.58f);

        /// <summary>Tête du serpent — plus claire, ET plus grosse (l'information n'est jamais portée par la seule couleur).</summary>
        public static readonly Color TeteSerpent = Gris(0.94f);

        /// <summary>Pictogramme de refus : le signal le plus clair de l'écran, il doit primer.</summary>
        public static readonly Color Pictogramme = Gris(1.00f);

        /// <summary>Texte principal du HUD.</summary>
        public static readonly Color TexteHud = Gris(0.86f);

        /// <summary>Texte secondaire (rappel des touches).</summary>
        public static readonly Color TexteSecondaire = Gris(0.52f);

        /// <summary>Voile assombrissant l'écran de pause. Opaque à 62 % : la grille reste lisible dessous.</summary>
        public static readonly Color VoileDePause = new Color(0f, 0f, 0f, 0.62f);

        /// <summary>Tampon de build : présent pour le rapport de bug, discret pour le joueur.</summary>
        public static readonly Color TamponDeBuild = new Color(1f, 1f, 1f, 0.45f);

        private static Color Gris(float valeur)
        {
            return new Color(valeur, valeur, valeur, 1f);
        }
    }
}
