using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Le départ à l'arrêt (GDD §4.1, arbitrage de l'auteur du 2026-08-27).
/// </summary>
public class DemarrageTests
{
    /// <summary>
    /// Le §4.1 est explicite : « un joueur qui tape Ouest voit le refus (§3) et rien ne bouge ».
    /// C'est ce cas qui enseigne la règle du demi-tour avant qu'aucun danger n'existe.
    /// </summary>
    [Fact]
    public void TaperOuestSurUnSerpentOrienteEstNeLancePasLaPartie()
    {
        Assert.Equal(
            DecisionDemarrage.RefuseDemiTour,
            Demarrage.Decider(Grille.OrientationInitiale, Direction.Ouest));
    }

    /// <summary>
    /// ⚠ Le cas qui se perd quand on branche le démarrage sur le résultat d'empilage : taper Est sur
    /// un serpent déjà orienté est produit <c>RefuseeDoublon</c>, qui n'a AUCUN retour visuel
    /// (ART §5.3). Un jeu qui refuserait de partir là-dessus resterait figé sans rien afficher — le
    /// joueur conclurait qu'il est cassé. Le §4.1 dit « le premier appui qui n'est pas un
    /// demi-tour » : le doublon en est un.
    /// </summary>
    [Fact]
    public void TaperLaDirectionDejaSuivieLanceQuandMemeLaPartie()
    {
        Assert.Equal(
            DecisionDemarrage.Demarre,
            Demarrage.Decider(Grille.OrientationInitiale, Direction.Est));
    }

    [Theory]
    [InlineData(Direction.Nord)]
    [InlineData(Direction.Sud)]
    public void UnVirageLanceLaPartie(Direction direction)
    {
        Assert.Equal(
            DecisionDemarrage.Demarre,
            Demarrage.Decider(Grille.OrientationInitiale, direction));
    }

    /// <summary>
    /// La règle ne connaît pas « l'est » en particulier : elle suit l'orientation de pose, quelle
    /// qu'elle soit. Le jour où le §4.3 change d'orientation de départ, rien à retoucher ici.
    /// </summary>
    [Fact]
    public void SeuleLOppositionALOrientationDePoseEstRefusee()
    {
        foreach (Direction pose in Directions.Toutes())
        {
            foreach (Direction demandee in Directions.Toutes())
            {
                DecisionDemarrage attendue = demandee == Directions.Oppose(pose)
                    ? DecisionDemarrage.RefuseDemiTour
                    : DecisionDemarrage.Demarre;

                Assert.Equal(attendue, Demarrage.Decider(pose, demandee));
            }
        }
    }
}
