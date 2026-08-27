namespace SnakeSnack.Rules
{
    /// <summary>Ce que le premier appui directionnel d'une partie déclenche (GDD §4.1).</summary>
    public enum DecisionDemarrage
    {
        /// <summary>La partie démarre : ce tick est le premier.</summary>
        Demarre,

        /// <summary>
        /// Demi-tour : le refus se voit (§3) et <b>rien ne bouge</b>. La partie ne démarre pas.
        /// </summary>
        RefuseDemiTour
    }

    /// <summary>
    /// Le départ à l'arrêt (GDD §4.1, arbitrage de l'auteur du 2026-08-27) : le premier tick est
    /// déclenché par la première direction <b>applicable</b>, pas par un simple appui.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Cette règle ne peut pas vivre dans <see cref="FileEntrees"/></b>, et le GDD le dit
    /// explicitement : la file ne juge jamais le demi-tour à l'empilage — elle le juge au tick,
    /// contre la direction réellement appliquée (§4.2, contre-exemple Nord/Sud). Or au démarrage il
    /// n'y a pas encore eu de tick : c'est l'<i>orientation de pose</i> qui sert de référence, et
    /// c'est au câblage moteur de trancher. D'où cette classe, minuscule mais nommée : sans elle, la
    /// décision se dilue dans un <c>if</c> au milieu d'un <c>Update()</c> et personne ne peut plus
    /// la tester ni la relire.
    ///
    /// <para>⚠ <b>Un doublon démarre la partie.</b> Le joueur qui tape Est sur un serpent déjà
    /// orienté est obtient <see cref="ResultatEmpilage.RefuseeDoublon"/> de la file — mais le §4.1
    /// dit « la partie démarre au premier appui qui n'est pas un demi-tour », et taper le cap qu'on
    /// suit déjà est une intention parfaitement claire : « vas-y ». Faire dépendre le démarrage du
    /// résultat d'empilage donnerait un jeu qui refuse de partir sur la flèche Droite, sans rien
    /// afficher (le doublon n'a pas de retour visuel, <c>docs/ART.md</c> §5.3) : le joueur
    /// conclurait que le jeu est cassé.</para>
    /// </remarks>
    public static class Demarrage
    {
        /// <param name="orientationDePose">Orientation du serpent à l'arrêt (§4.3 : est).</param>
        /// <param name="directionDemandee">Direction que le joueur vient de taper.</param>
        public static DecisionDemarrage Decider(Direction orientationDePose, Direction directionDemandee)
        {
            return Directions.EstDemiTour(orientationDePose, directionDemandee)
                ? DecisionDemarrage.RefuseDemiTour
                : DecisionDemarrage.Demarre;
        }
    }
}
