using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Ce que le design INTERDIT dans la file d'entrées (GDD §4.2). Chaque test nomme la règle qu'il
/// verrouille — pas la ligne de code qu'il traverse.
/// </summary>
public class FileEntreesTests
{
    /// <summary>
    /// LE test du §4.2, écrit en premier parce que c'est lui qui impose toute la conception :
    /// serpent vers l'est, le joueur tape Nord puis Sud dans le même tick.
    ///
    /// Ni Nord ni Sud n'est un demi-tour de l'EST. Validés à l'appui, ils passeraient tous les deux
    /// et le tick suivant appliquerait Sud sur un serpent parti au nord : il se mange la nuque.
    /// Validé au tick contre la direction RÉELLEMENT appliquée au tick précédent, Sud est comparé
    /// au Nord, reconnu comme demi-tour, refusé.
    /// </summary>
    [Fact]
    public void NordPuisSudDansLeMemeTick_LeNordPasseEtLeSudEstRefuseAuTickSuivant()
    {
        var file = new FileEntrees(Direction.Est);

        Assert.Equal(ResultatEmpilage.Acceptee, file.Empiler(Direction.Nord));
        Assert.Equal(ResultatEmpilage.Acceptee, file.Empiler(Direction.Sud));

        ResultatTick premier = file.Tick();
        Assert.Equal(Direction.Nord, premier.DirectionAppliquee);
        Assert.False(premier.DemiTourRefuse);

        ResultatTick second = file.Tick();
        Assert.True(second.DemiTourRefuse);
        Assert.Equal(Direction.Sud, second.DirectionRefusee);

        // Le tick reconduit la direction courante : le serpent continue au nord, il ne se retourne
        // pas et il ne s'arrête pas non plus.
        Assert.Equal(Direction.Nord, second.DirectionAppliquee);
        Assert.Equal(Direction.Nord, file.DirectionCourante);
    }

    /// <summary>
    /// Corollaire du test précédent : l'appui n'est JAMAIS l'endroit où l'on juge un demi-tour.
    /// Refuser Ouest dès l'appui alors que le serpent va à l'est semble anodin — c'est exactement
    /// la conception qui perd le contre-exemple Nord/Sud dès qu'un virage s'intercale.
    /// </summary>
    [Fact]
    public void LeDemiTourNEstPasJugeALAppui()
    {
        var file = new FileEntrees(Direction.Est);

        Assert.Equal(ResultatEmpilage.Acceptee, file.Empiler(Direction.Ouest));
        Assert.Equal(1, file.NombreEnAttente);

        ResultatTick tick = file.Tick();
        Assert.True(tick.DemiTourRefuse);
        Assert.Equal(Direction.Est, tick.DirectionAppliquee);
    }

    /// <summary>
    /// Un demi-tour devient légitime dès qu'un virage s'intercale : Est puis Nord appliqué, Ouest
    /// n'est plus un demi-tour. Le refus doit être relatif au tick précédent, pas à la direction de
    /// départ de la partie.
    /// </summary>
    [Fact]
    public void ApresUnVirage_LeDemiTourParRapportALaDirectionInitialeEstLegitime()
    {
        var file = new FileEntrees(Direction.Est);

        file.Empiler(Direction.Nord);
        Assert.Equal(Direction.Nord, file.Tick().DirectionAppliquee);

        file.Empiler(Direction.Ouest);
        ResultatTick tick = file.Tick();

        Assert.False(tick.DemiTourRefuse);
        Assert.Equal(Direction.Ouest, tick.DirectionAppliquee);
    }

    /// <summary>
    /// Un seul virage par tick, quoi qu'il arrive : c'est ce qui garantit que la trajectoire lue à
    /// l'écran est celle que le joueur a tapée, dans l'ordre où il l'a tapée.
    /// </summary>
    [Fact]
    public void UnTickNeConsommeQuUneSeuleEntree()
    {
        var file = new FileEntrees(Direction.Est);

        file.Empiler(Direction.Nord);
        file.Empiler(Direction.Ouest);
        Assert.Equal(2, file.NombreEnAttente);

        Assert.Equal(Direction.Nord, file.Tick().DirectionAppliquee);
        Assert.Equal(1, file.NombreEnAttente);

        Assert.Equal(Direction.Ouest, file.Tick().DirectionAppliquee);
        Assert.Equal(0, file.NombreEnAttente);
    }

    /// <summary>
    /// File pleine : la nouvelle touche est ignorée, la plus ancienne n'est PAS écrasée. Écraser
    /// annulerait en silence un virage déjà parti des doigts du joueur — le serpent raterait un
    /// virage que le joueur a bel et bien tapé (§4.2).
    /// </summary>
    [Fact]
    public void FilePleine_LaNouvelleToucheEstIgnoreeEtLaPlusAncienneSurvit()
    {
        var file = new FileEntrees(Direction.Est);

        Assert.Equal(ResultatEmpilage.Acceptee, file.Empiler(Direction.Nord));
        Assert.Equal(ResultatEmpilage.Acceptee, file.Empiler(Direction.Ouest));
        Assert.Equal(ResultatEmpilage.RefuseeFilePleine, file.Empiler(Direction.Sud));

        // La file n'a pas grandi et son contenu est intact, dans l'ordre.
        Assert.Equal(2, file.NombreEnAttente);
        Assert.Equal(Direction.Nord, file.Tick().DirectionAppliquee);
        Assert.Equal(Direction.Ouest, file.Tick().DirectionAppliquee);
    }

    /// <summary>
    /// Le débordement doit être OBSERVABLE : « invisible se lit inexistant » (§3). Un appui ignoré
    /// sans retour à l'écran est lu comme un appui raté par le jeu.
    /// </summary>
    [Fact]
    public void ChaqueRefusPorteSonMotif()
    {
        var file = new FileEntrees(Direction.Est);

        Assert.Equal(ResultatEmpilage.RefuseeDoublon, file.Empiler(Direction.Est));

        file.Empiler(Direction.Nord);
        file.Empiler(Direction.Ouest);
        Assert.Equal(ResultatEmpilage.RefuseeFilePleine, file.Empiler(Direction.Sud));

        file.EntrerEnPause();
        Assert.Equal(ResultatEmpilage.RefuseeJeuEnPause, file.Empiler(Direction.Nord));
    }

    /// <summary>
    /// Un appui identique à la direction courante (file vide) ne consomme pas de place : sinon,
    /// marteler la touche que l'on suit déjà remplirait la file et ferait rater le virage suivant.
    /// </summary>
    [Fact]
    public void FileVide_LAppuiIdentiqueALaDirectionCouranteEstRefuse()
    {
        var file = new FileEntrees(Direction.Est);

        Assert.Equal(ResultatEmpilage.RefuseeDoublon, file.Empiler(Direction.Est));
        Assert.Equal(0, file.NombreEnAttente);
    }

    /// <summary>
    /// Même chose contre la dernière direction DÉJÀ EN FILE : c'est le cas du joueur qui martèle
    /// pendant que le virage attend son tick.
    /// </summary>
    [Fact]
    public void LAppuiIdentiqueALaDerniereDirectionEnFileEstRefuse()
    {
        var file = new FileEntrees(Direction.Est);

        Assert.Equal(ResultatEmpilage.Acceptee, file.Empiler(Direction.Nord));
        Assert.Equal(ResultatEmpilage.RefuseeDoublon, file.Empiler(Direction.Nord));
        Assert.Equal(1, file.NombreEnAttente);

        // ... et la place laissée libre sert au virage suivant, qui, lui, change quelque chose.
        Assert.Equal(ResultatEmpilage.Acceptee, file.Empiler(Direction.Ouest));
    }

    /// <summary>
    /// Un doublon reste un doublon quand la file est pleine : annoncer « file pleine » donnerait au
    /// joueur une raison fausse de son refus, et l'UI afficherait le mauvais retour.
    /// </summary>
    [Fact]
    public void FilePleine_UnDoublonEstAnnonceCommeDoublonPasCommeDebordement()
    {
        var file = new FileEntrees(Direction.Est);

        file.Empiler(Direction.Nord);
        file.Empiler(Direction.Ouest);

        Assert.Equal(ResultatEmpilage.RefuseeDoublon, file.Empiler(Direction.Ouest));
    }

    /// <summary>
    /// L'entrée refusée est JETÉE : elle ne bloque pas la file. Sans ça, un demi-tour tapé par
    /// erreur gèlerait tous les virages suivants et le joueur lirait « le jeu ne répond plus ».
    /// </summary>
    [Fact]
    public void UneEntreeRefuseeNeBloquePasCellesQuiSuivent()
    {
        var file = new FileEntrees(Direction.Est);

        file.Empiler(Direction.Ouest); // demi-tour, sera refusé au tick
        file.Empiler(Direction.Nord);

        ResultatTick refuse = file.Tick();
        Assert.True(refuse.DemiTourRefuse);
        Assert.Equal(Direction.Est, refuse.DirectionAppliquee);

        Assert.Equal(Direction.Nord, file.Tick().DirectionAppliquee);
    }

    /// <summary>
    /// Purge à la pause (§4.2) : reprendre doit rendre l'état VISIBLE à l'écran, pas exécuter un
    /// virage tapé avant la pause. Le joueur a regardé la grille figée et rejoue ce qu'il voit.
    /// </summary>
    [Fact]
    public void LaPauseVideLaFileEtLaRepriseReconduitLaDirectionCourante()
    {
        var file = new FileEntrees(Direction.Est);

        file.Empiler(Direction.Nord);
        file.Empiler(Direction.Ouest);

        file.EntrerEnPause();
        Assert.Equal(0, file.NombreEnAttente);

        // Une direction tapée pendant la pause n'est pas empilée (§3).
        Assert.Equal(ResultatEmpilage.RefuseeJeuEnPause, file.Empiler(Direction.Nord));
        Assert.Equal(0, file.NombreEnAttente);

        file.Reprendre();
        Assert.Equal(0, file.NombreEnAttente);
        Assert.Equal(Direction.Est, file.Tick().DirectionAppliquee);
    }

    /// <summary>
    /// Le jeu ne tique pas en pause. Un no-op silencieux ferait avancer le serpent d'une case
    /// pendant la pause sans que rien ne le signale : on lève, pour que ce soit vu au bon moment.
    /// </summary>
    [Fact]
    public void TiquerPendantLaPauseEstUneErreurDAppelant()
    {
        var file = new FileEntrees(Direction.Est);
        file.EntrerEnPause();

        Assert.Throws<InvalidOperationException>(() => file.Tick());
    }

    /// <summary>
    /// Purge à la mort (§4.2) : aucun virage tapé pendant l'agonie ne doit s'appliquer à la partie
    /// suivante — la relance coûte une touche et zéro attente (§2), elle ne doit pas hériter d'un
    /// geste de panique.
    /// </summary>
    [Fact]
    public void LaMortVideLaFile()
    {
        var file = new FileEntrees(Direction.Est);

        file.Empiler(Direction.Nord);
        file.Empiler(Direction.Ouest);

        file.Mourir();
        Assert.Equal(0, file.NombreEnAttente);

        file.Reinitialiser(Grille.OrientationInitiale);
        Assert.Equal(0, file.NombreEnAttente);
        Assert.False(file.EnPause);
        Assert.Equal(Direction.Est, file.DirectionCourante);
    }

    /// <summary>
    /// Une profondeur de 1 perdrait la seconde moitié de toute chicane tapée en moins d'un tick :
    /// c'est l'origine habituelle du « ce Snake rate mes virages », écartée au §7. Ce test montre
    /// ce que la profondeur 2 achète, et tombera si quelqu'un ramène la file à 1.
    /// </summary>
    [Fact]
    public void LaProfondeurParDefautEncaisseUnVirageEnLTapeDansLeMemeTick()
    {
        Assert.Equal(2, FileEntrees.ProfondeurParDefaut);

        var file = new FileEntrees(Direction.Est);

        // Le geste complet « monte puis va à gauche », tapé plus vite que la cadence.
        Assert.Equal(ResultatEmpilage.Acceptee, file.Empiler(Direction.Nord));
        Assert.Equal(ResultatEmpilage.Acceptee, file.Empiler(Direction.Ouest));

        Assert.Equal(Direction.Nord, file.Tick().DirectionAppliquee);
        Assert.Equal(Direction.Ouest, file.Tick().DirectionAppliquee);
    }
}
