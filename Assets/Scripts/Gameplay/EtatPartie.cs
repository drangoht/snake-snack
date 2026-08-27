namespace SnakeSnack.Gameplay
{
    /// <summary>Les quatre états d'une partie (GDD §2 et §4.1).</summary>
    public enum EtatPartie
    {
        /// <summary>
        /// Serpent posé, orienté, <b>immobile</b>. Aucun tick n'est joué tant qu'une direction
        /// applicable n'a pas été tapée (§4.1) : personne ne meurt pendant que le joueur lit l'écran.
        /// </summary>
        EnAttente,

        /// <summary>La partie tourne : le serpent avance d'une case par tick.</summary>
        EnCours,

        /// <summary>
        /// Pause. ⚠ Aucun tick n'est joué — <see cref="SnakeSnack.Rules.FileEntrees.Tick"/> lève si
        /// on l'appelle en pause, précisément pour qu'un serpent ne puisse pas avancer hors de la
        /// vue du joueur.
        /// </summary>
        EnPause,

        /// <summary>Mort contre un mur ou contre son propre corps. Espace relance (§2).</summary>
        Mort
    }
}
