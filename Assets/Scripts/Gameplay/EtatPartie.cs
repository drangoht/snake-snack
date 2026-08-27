namespace SnakeSnack.Gameplay
{
    /// <summary>Les cinq états d'une partie (GDD §2, §4.1 et §4.4).</summary>
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
        Mort,

        /// <summary>
        /// Le serpent remplit la grille : plus une case libre, donc plus de pomme à poser (§4.4).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Cet état est hors de portée humaine</b> — 312 pommes sur la grille par défaut — et
        /// doit néanmoins exister. Sans lui, le tirage de la pomme part sur un intervalle vide au
        /// dernier tick de la partie parfaite : le jeu casse ou se fige, précisément dans la seule
        /// situation qu'aucune session de test n'atteindra jamais. Même écran et même relance à une
        /// touche que la mort, avec un libellé distinct.
        /// </remarks>
        Victoire
    }
}
