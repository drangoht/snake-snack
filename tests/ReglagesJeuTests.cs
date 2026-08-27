using System.Collections.Generic;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Le tuning réglable sans recompiler (GDD §4.1, §4.3) — et ce qu'il refuse de laisser passer.
/// </summary>
public class ReglagesJeuTests
{
    /// <summary>
    /// Le fichier absent ou vide doit donner exactement le jeu du GDD : c'est le repli, et il ne
    /// doit jamais dériver de ce que le document décrit.
    /// </summary>
    [Fact]
    public void LesReglagesParDefautSontCeuxDuGdd()
    {
        ReglagesJeu reglages = ReglagesJeu.ParDefaut();

        Assert.Equal(8.0, reglages.ticksParSeconde);
        Assert.Equal(1, reglages.plafondDeRattrapage);
        Assert.Equal(21, reglages.largeurGrille);
        Assert.Equal(15, reglages.hauteurGrille);
        Assert.Equal(2, reglages.profondeurFile);
    }

    [Fact]
    public void DesReglagesValidesNeProduisentAucuneAnomalie()
    {
        ReglagesJeu sain = ReglagesJeu.ParDefaut().Valider(out IList<string> anomalies);

        Assert.Empty(anomalies);
        Assert.Equal(8.0, sain.ticksParSeconde);
        Assert.Equal(21, sain.largeurGrille);
    }

    /// <summary>
    /// ⚠ Le cas qui motive toute cette validation : une dimension paire ne lève rien à l'exécution,
    /// elle décale seulement la pose de départ d'une demi-case (§4.3) — un défaut que personne ne
    /// remarque avant une capture d'écran. Le repli doit être la grille COMPLÈTE du GDD, pas une
    /// largeur voisine bricolée : 20 × 15 ne devient pas 21 × 15 par hasard, il redevient la grille
    /// que quelqu'un a décidée.
    /// </summary>
    [Fact]
    public void UneGrillePaireRetombeSurCelleDuGddEtLeDit()
    {
        ReglagesJeu brut = ReglagesJeu.ParDefaut();
        brut.largeurGrille = 20;

        ReglagesJeu sain = brut.Valider(out IList<string> anomalies);

        Assert.Equal(21, sain.largeurGrille);
        Assert.Equal(15, sain.hauteurGrille);
        Assert.NotEmpty(anomalies);
    }

    /// <summary>
    /// Une cadence nulle fige le jeu sans rien lever : le serpent n'avance plus, et le joueur croit
    /// à un plantage.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-4.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UneCadenceImpossibleRetombeSurCelleDuGdd(double cadence)
    {
        ReglagesJeu brut = ReglagesJeu.ParDefaut();
        brut.ticksParSeconde = cadence;

        ReglagesJeu sain = brut.Valider(out IList<string> anomalies);

        Assert.Equal(Cadence.TicksParSecondeParDefaut, sain.ticksParSeconde);
        Assert.NotEmpty(anomalies);
    }

    /// <summary>
    /// ⚠ La fourchette conseillée 6–10 ticks/s (§4.1) est un CONSEIL, pas une borne : sortir de la
    /// fourchette est exactement ce qu'on veut pouvoir essayer sans recompiler. La valeur est donc
    /// conservée — mais signalée, pour qu'une session de test à 20 ticks/s ne se croie pas au
    /// réglage nominal.
    /// </summary>
    [Fact]
    public void UneCadenceHorsFourchetteEstSignaleeMaisConservee()
    {
        ReglagesJeu brut = ReglagesJeu.ParDefaut();
        brut.ticksParSeconde = 20.0;

        ReglagesJeu sain = brut.Valider(out IList<string> anomalies);

        Assert.Equal(20.0, sain.ticksParSeconde);
        Assert.NotEmpty(anomalies);
    }

    /// <summary>
    /// Un plafond de rattrapage nul figerait le serpent (voir <c>Cadence.NombreDeTicks</c>, qui lève
    /// dans ce cas) : le repli évite de faire planter le jeu au premier réglage maladroit.
    /// </summary>
    [Fact]
    public void UnPlafondDeRattrapageNulRetombeSurUn()
    {
        ReglagesJeu brut = ReglagesJeu.ParDefaut();
        brut.plafondDeRattrapage = 0;

        ReglagesJeu sain = brut.Valider(out IList<string> anomalies);

        Assert.Equal(1, sain.plafondDeRattrapage);
        Assert.NotEmpty(anomalies);
    }

    /// <summary>
    /// Un plafond de prolongation plus court que la durée d'affichage éteindrait le pictogramme
    /// avant qu'il ait été lu — l'inverse exact de ce que l'ART §5.5 en attend. Les réglages sortis
    /// d'ici doivent être directement acceptables par <see cref="EtatRetourAEcheance"/>.
    /// </summary>
    [Fact]
    public void UnPlafondDeRefusPlusCourtQueLAffichageEstRedresse()
    {
        ReglagesJeu brut = ReglagesJeu.ParDefaut();
        brut.dureeAffichageRefusSecondes = 0.4;
        brut.plafondProlongationRefusSecondes = 0.1;

        ReglagesJeu sain = brut.Valider(out IList<string> anomalies);

        Assert.True(sain.plafondProlongationRefusSecondes >= sain.dureeAffichageRefusSecondes);
        Assert.NotEmpty(anomalies);
    }

    /// <summary>
    /// Le contrat qui compte vraiment : quel que soit le contenu du JSON, les réglages validés
    /// construisent un jeu qui démarre. Un tuning édité à la main ne doit jamais pouvoir lever une
    /// exception au lancement — il doit au pire être ignoré, bruyamment.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(20, 14)]
    [InlineData(3, 3)]
    [InlineData(-7, 15)]
    [InlineData(1001, 1001)]
    public void DesReglagesAberrantsRestentConstructibles(int largeur, int hauteur)
    {
        ReglagesJeu brut = ReglagesJeu.ParDefaut();
        brut.largeurGrille = largeur;
        brut.hauteurGrille = hauteur;
        brut.ticksParSeconde = 0.0;
        brut.profondeurFile = 0;
        brut.dureeAffichageRefusSecondes = -1.0;
        brut.dureeFonduRefusSecondes = 0.0;

        ReglagesJeu sain = brut.Valider(out IList<string> anomalies);

        Assert.NotEmpty(anomalies);

        Grille grille = new Grille(sain.largeurGrille, sain.hauteurGrille);
        FileEntrees file = new FileEntrees(Grille.OrientationInitiale, sain.profondeurFile);
        Serpent serpent = new Serpent(grille.PoseDeDepart().Segments);
        EtatRetourAEcheance retour = new EtatRetourAEcheance(
            sain.dureeAffichageRefusSecondes,
            sain.plafondProlongationRefusSecondes,
            sain.dureeFonduRefusSecondes);
        Plateau plateau = new Plateau(grille, Plateau.TailleDeCase(grille));

        Assert.Equal(ResultatDeplacement.Avance, serpent.Avancer(file.Tick().DirectionAppliquee, grille));
        Assert.True(Cadence.NombreDeTicks(1.0, Cadence.DureeTickSecondes(sain.ticksParSeconde), out _, sain.plafondDeRattrapage) >= 0);
        Assert.False(retour.EstVisible(0.0));
        Assert.True(plateau.TailleCase >= 1);
    }

    /// <summary>
    /// ⚠ Une correction muette est pire que pas de correction : le joueur édite son JSON, ne voit
    /// aucun changement, et n'a aucun moyen de savoir pourquoi. Chaque anomalie doit être une phrase
    /// exploitable, pas un code.
    /// </summary>
    [Fact]
    public void ChaqueAnomalieEstUnePhraseLisible()
    {
        ReglagesJeu brut = ReglagesJeu.ParDefaut();
        brut.largeurGrille = 20;
        brut.ticksParSeconde = -1.0;

        brut.Valider(out IList<string> anomalies);

        Assert.All(anomalies, phrase => Assert.True(phrase.Length > 20, $"Anomalie trop laconique : « {phrase} »"));
    }

    /// <summary>
    /// La graine traverse la validation <b>sans être corrigée ni signalée</b> : toute la plage de
    /// <c>long</c> désigne une suite légitime (GDD §4.4). Une graine « corrigée » ferait rejouer une
    /// autre partie que celle qu'on voulait rejouer, ce qui est exactement l'inverse du but.
    /// </summary>
    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-20260827L)]
    [InlineData(long.MaxValue)]
    public void LaGraineNEstJamaisCorrigee(long graine)
    {
        ReglagesJeu brut = ReglagesJeu.ParDefaut();
        brut.graine = graine;

        ReglagesJeu sain = brut.Valider(out IList<string> anomalies);

        Assert.Equal(graine, sain.graine);
        Assert.Empty(anomalies);
    }

    /// <summary>Par défaut, chaque partie reçoit une graine neuve (§4.4).</summary>
    [Fact]
    public void ParDefautAucuneGraineNEstFixee()
    {
        Assert.Equal(ReglagesJeu.GraineDeLHorloge, ReglagesJeu.ParDefaut().graine);
    }
}
