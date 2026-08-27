using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>Pose du serpent au démarrage d'une partie (GDD §4.3).</summary>
    public readonly struct PoseInitiale
    {
        public PoseInitiale(IReadOnlyList<Case> segments, Direction orientation)
        {
            Segments = segments;
            Orientation = orientation;
        }

        /// <summary>Segments <b>tête en premier</b> : l'ordre porte la géométrie du corps.</summary>
        public IReadOnlyList<Case> Segments { get; }

        /// <summary>Orientation de départ. Le serpent est immobile mais orienté (§4.3).</summary>
        public Direction Orientation { get; }

        /// <summary>Case de la tête.</summary>
        public Case Tete
        {
            get { return Segments[0]; }
        }

        /// <summary>Longueur du serpent au départ (§2 et §4.3 : 3).</summary>
        public int Longueur
        {
            get { return Segments.Count; }
        }
    }

    /// <summary>
    /// L'aire de jeu du GDD §4.3 : dimensions, case centrale exacte, pose de départ, et le test
    /// « case hors grille » qui porte le mur mortel du §2.
    /// </summary>
    /// <remarks>
    /// Type valeur porteur de ses dimensions plutôt que classe statique à constantes : c'est ce qui
    /// rend la grille réglable <b>sans recompiler</b> (§4.3). L'appelant moteur lit largeur et
    /// hauteur d'un JSON de <c>StreamingAssets</c> et construit une <see cref="Grille"/> ; le repli
    /// est <see cref="ParDefaut"/>.
    /// </remarks>
    public readonly struct Grille
    {
        /// <summary>Largeur par défaut : 21 cases (§4.3, au jugé).</summary>
        public const int LargeurParDefaut = 21;

        /// <summary>Hauteur par défaut : 15 cases (§4.3, au jugé). 315 cases au total.</summary>
        public const int HauteurParDefaut = 15;

        /// <summary>Longueur du serpent au départ : 3 segments (§2, repris en §4.3).</summary>
        public const int LongueurInitiale = 3;

        /// <summary>Orientation de départ : est (§4.3).</summary>
        public const Direction OrientationInitiale = Direction.Est;

        /// <param name="largeur">Nombre de colonnes. <b>Impair obligatoire.</b></param>
        /// <param name="hauteur">Nombre de lignes. <b>Impair obligatoire.</b></param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Dimension paire, ou trop petite pour poser le serpent de départ.
        /// </exception>
        /// <remarks>
        /// ⚠ <b>Le refus des dimensions paires est une règle de design, pas une coquetterie</b>
        /// (§4.3) : sans axe impair il n'existe pas de case centrale exacte, et le serpent
        /// apparaîtrait décalé d'une demi-case — le « au centre » du §2 deviendrait faux. Une grille
        /// paire ne lève rien à l'exécution : elle produit juste une pose légèrement de travers que
        /// personne ne remarque avant une capture d'écran. D'où l'échec ici, au plus tôt.
        /// (C'est aussi ce qui a fait écarter la grille 32 × 18, §7.)
        /// </remarks>
        public Grille(int largeur, int hauteur)
        {
            // Largeur minimale : la tête occupe la colonne centrale et les deux segments de corps
            // s'étendent vers l'ouest, donc (largeur - 1) / 2 doit valoir au moins LongueurInitiale - 1.
            const int largeurMinimale = 2 * (LongueurInitiale - 1) + 1;

            if (largeur < largeurMinimale)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(largeur), largeur,
                    "La grille doit être assez large pour la pose de départ (au moins " + largeurMinimale + " colonnes).");
            }

            if (hauteur < 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hauteur), hauteur, "La grille doit avoir au moins 3 lignes pour qu'un virage existe.");
            }

            if (largeur % 2 == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(largeur), largeur, "La largeur doit être impaire : sans cela, pas de case centrale exacte (GDD §4.3).");
            }

            if (hauteur % 2 == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hauteur), hauteur, "La hauteur doit être impaire : sans cela, pas de case centrale exacte (GDD §4.3).");
            }

            Largeur = largeur;
            Hauteur = hauteur;
        }

        /// <summary>Nombre de colonnes.</summary>
        public int Largeur { get; }

        /// <summary>Nombre de lignes.</summary>
        public int Hauteur { get; }

        /// <summary>La grille du GDD §4.3 : 21 × 15.</summary>
        public static Grille ParDefaut
        {
            get { return new Grille(LargeurParDefaut, HauteurParDefaut); }
        }

        /// <summary>Nombre total de cases (315 par défaut).</summary>
        public int NombreDeCases
        {
            get { return Largeur * Hauteur; }
        }

        /// <summary>
        /// Case centrale exacte, en indices 0 : <c>(10, 7)</c> sur la grille par défaut (§4.3).
        /// </summary>
        public Case Centre
        {
            get { return new Case((Largeur - 1) / 2, (Hauteur - 1) / 2); }
        }

        /// <summary>Vrai si la case appartient à l'aire de jeu.</summary>
        public bool Contient(Case caseTestee)
        {
            return caseTestee.X >= 0
                   && caseTestee.X < Largeur
                   && caseTestee.Y >= 0
                   && caseTestee.Y < Hauteur;
        }

        /// <summary>
        /// Vrai si la case est hors de l'aire de jeu — <b>c'est la mort</b> (§2).
        /// </summary>
        /// <remarks>
        /// ⚠ Les bords tuent, ils ne téléportent pas. Aucun modulo nulle part : le jour où
        /// quelqu'un en écrit un « pour éviter un index négatif », il réintroduit sans le dire les
        /// bords traversants écartés au §7, et la mort cesse d'être imputable au dernier virage
        /// (§2). Une grille close se lit d'un coup d'œil ; un bord traversant demande de simuler
        /// mentalement une continuité invisible.
        /// </remarks>
        public bool EstHorsGrille(Case caseTestee)
        {
            return !Contient(caseTestee);
        }

        /// <summary>
        /// Pose de départ (§4.3) : tête sur la case centrale, corps aligné derrière elle vers
        /// l'ouest, longueur 3, orientation est.
        /// </summary>
        /// <remarks>
        /// Le corps s'étend <b>à l'opposé de l'orientation</b> : c'est ce qui donne
        /// <c>(10, 7) (9, 7) (8, 7)</c> sur la grille par défaut. Poser le corps devant la tête
        /// tuerait le serpent au premier tick, sans autre symptôme qu'une partie qui ne démarre pas.
        /// </remarks>
        public PoseInitiale PoseDeDepart()
        {
            Case reculDUnPas = Directions.Deplacement(Directions.Oppose(OrientationInitiale));
            Case[] segments = new Case[LongueurInitiale];
            segments[0] = Centre;

            for (int i = 1; i < LongueurInitiale; i++)
            {
                segments[i] = segments[i - 1].Plus(reculDUnPas);
            }

            return new PoseInitiale(segments, OrientationInitiale);
        }
    }
}
