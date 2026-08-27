#nullable enable
using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Coordonnée entière d'une case de la grille, en indices 0 (GDD §4.3).
    /// </summary>
    /// <remarks>
    /// ⚠ Ce type existe <b>parce que <c>Vector2Int</c> vient d'<c>UnityEngine</c></b> : l'utiliser
    /// ici rendrait tout <c>Rules/</c> incompilable hors moteur, donc intestable en quelques
    /// millisecondes par <c>dotnet test</c>. C'est à l'appelant de convertir vers le type moteur.
    ///
    /// <para>Entier et non flottant : le serpent « avance d'une case par tick, jamais entre deux
    /// ticks » (§4.1). Une position flottante autoriserait un état hors grille et rendrait la
    /// collision — donc la mort — dépendante d'un epsilon.</para>
    /// </remarks>
    public readonly struct Case : IEquatable<Case>
    {
        public Case(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        /// <summary>Somme composante par composante (une case plus un déplacement).</summary>
        public Case Plus(Case autre)
        {
            return new Case(X + autre.X, Y + autre.Y);
        }

        public bool Equals(Case autre)
        {
            return X == autre.X && Y == autre.Y;
        }

        public override bool Equals(object? obj)
        {
            return obj is Case autre && Equals(autre);
        }

        public override int GetHashCode()
        {
            // Combinaison suffisante pour une grille de quelques centaines de cases : la tête est
            // testée contre le corps à chaque tick, donc via un HashSet dans l'appelant.
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(Case gauche, Case droite)
        {
            return gauche.Equals(droite);
        }

        public static bool operator !=(Case gauche, Case droite)
        {
            return !gauche.Equals(droite);
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }
    }
}
