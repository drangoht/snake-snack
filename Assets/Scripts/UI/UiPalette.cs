using UnityEngine;

namespace SnakeSnack.UI
{
    /// <summary>
    /// ⚠ <b>LE SEUL ENDROIT DU DÉPÔT OÙ UNE COULEUR EST ÉCRITE EN DUR.</b> Douze rôles nommés,
    /// tranchés par <c>docs/ART.md</c> §1 (détail et preuves de contraste : <c>docs/art/palette.md</c>).
    /// </summary>
    /// <remarks>
    /// <para>Un sprite, un shader, un <c>Image</c> ou un <c>Text</c> référencent le <b>rôle</b>, jamais
    /// un <c>#RRGGBB</c> recopié : c'est ce qui permet de retoucher toute l'identité visuelle sans
    /// relire un seul appelant.</para>
    ///
    /// <para>⚠ <b>Le projet est en espace colorimétrique Gamma</b> (<c>ProjectSettings.asset</c> :
    /// <c>m_ActiveColorSpace: 0</c>) : les octets se posent tels quels, aucune reconversion linéaire.
    /// Si le projet passe un jour en Linear, ces valeurs sont à rouvrir — pas à « corriger » par
    /// anticipation.</para>
    ///
    /// <para>⚠ <b>Jamais une information portée par la seule couleur</b> (ART §4) : tout ce qui se
    /// distingue ici se distingue AUSSI par la forme ou la position (la tête est plus grosse que le
    /// corps, la pomme est un losange plus petit qu'une case, le pictogramme est barré, le bord de
    /// l'aire est un trait continu). La paire la plus faible en luminance, pomme contre corps
    /// (1,07 : 1), ne tient QUE par la forme — voir <c>docs/art/palette.md</c> §1.5.</para>
    /// </remarks>
    public static class UiPalette
    {
        /// <summary>Fond du cadre, hors aire de jeu — les marges latérales du GDD §4.3.</summary>
        /// <remarks>
        /// Slate quasi noir, jamais un noir pur : un <c>#000000</c> strict s'écrase sur les écrans
        /// bas de gamme et rend <see cref="TraitDeGrille"/> invisible chez une partie du public itch.
        /// </remarks>
        public static readonly Color Fond = Octets(0x0A, 0x0E, 0x13);

        /// <summary>Fond de l'aire de jeu : légèrement plus clair, pour que l'aire se détache.</summary>
        public static readonly Color AireDeJeu = Octets(0x12, 0x18, 0x21);

        /// <summary>Traits de la grille : présents mais discrets — ils aident à compter, pas à lire.</summary>
        public static readonly Color TraitDeGrille = Octets(0x1C, 0x25, 0x30);

        /// <summary>
        /// Bordure de l'aire de jeu. <b>Ambre</b> : c'est le mur qui tue (GDD §2), et la seule couleur
        /// « alerte » posée à demeure sur tout l'écran. 8,06 : 1 contre <see cref="AireDeJeu"/>.
        /// </summary>
        public static readonly Color BordureAire = Octets(0xE3, 0xA2, 0x3A);

        /// <summary>
        /// Corps du serpent. Vert moyen : le serpent est le joueur — ni danger ni objectif, donc la
        /// seule couleur du jeu qui ne signale rien.
        /// </summary>
        public static readonly Color CorpsSerpent = Octets(0x4E, 0x93, 0x58);

        /// <summary>
        /// Tête du serpent — même vert tiré vers le clair, ET plus grosse : la case qui compte le plus
        /// au tick reste la plus lisible, sans que l'information tienne à la seule couleur.
        /// </summary>
        public static readonly Color TeteSerpent = Octets(0xD8, 0xF5, 0xC4);

        /// <summary>
        /// La pomme. Rouge chaud, seule couleur de ce hue dans le jeu. ⚠ Elle se distingue du serpent
        /// par sa <b>forme</b> (un losange, contre des carrés) et par sa <b>taille</b> avant de se
        /// distinguer par sa couleur : contre <see cref="CorpsSerpent"/> le contraste de luminance
        /// tombe à 1,07 : 1, et rouge/vert est justement la paire qu'une deutéranopie confond.
        /// </summary>
        public static readonly Color Pomme = Octets(0xE5, 0x47, 0x3B);

        /// <summary>
        /// Pictogramme de refus : le signal le plus clair de l'écran, il doit primer. Blanc pur
        /// <b>réservé</b> — aucun autre rôle n'atteint cette valeur, y compris <see cref="TexteHud"/>.
        /// Le chevron d'un demi-tour tombe toujours sur le corps du serpent (ART §5.6), d'où 3,72 : 1
        /// contre <see cref="CorpsSerpent"/>.
        /// </summary>
        public static readonly Color Pictogramme = Octets(0xFF, 0xFF, 0xFF);

        /// <summary>Texte principal du HUD : blanc légèrement froid, jamais aussi saturé que le pictogramme.</summary>
        public static readonly Color TexteHud = Octets(0xE7, 0xED, 0xF2);

        /// <summary>Texte secondaire (rappel des touches) : gris-bleu de la famille du fond, hiérarchie sous <see cref="TexteHud"/>.</summary>
        public static readonly Color TexteSecondaire = Octets(0x87, 0x92, 0xA0);

        /// <summary>
        /// Voile assombrissant l'écran de pause. Opaque à 62 % : la grille reste lisible dessous.
        /// Achromatique à dessein — un voile teinté entrerait en concurrence avec les quatre couleurs
        /// chaudes du jeu.
        /// </summary>
        public static readonly Color VoileDePause = new Color(0f, 0f, 0f, 0.62f);

        /// <summary>
        /// Tampon de build : présent pour le rapport de bug, discret pour le joueur. Blanc à 45 %,
        /// achromatique pour rester lisible quel que soit le fond réel qu'il recouvre.
        /// </summary>
        public static readonly Color TamponDeBuild = new Color(1f, 1f, 1f, 0.45f);

        /// <summary>
        /// Un code hexa d'<c>ART.md</c> §1 posé octet par octet.
        /// </summary>
        /// <remarks>
        /// Les octets sont écrits en <c>0xNN</c> pour se relire tels quels contre le brief. Division
        /// par 255 sans conversion sRGB → linéaire : le projet est en Gamma (voir la remarque de
        /// classe). <c>ColorUtility.TryParseHtmlString</c> est volontairement évité — il renvoie un
        /// booléen que personne ne teste, et une chaîne mal frappée passerait en noir sans rien lever.
        /// </remarks>
        private static Color Octets(byte rouge, byte vert, byte bleu)
        {
            return new Color(rouge / 255f, vert / 255f, bleu / 255f, 1f);
        }
    }
}
