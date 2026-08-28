using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// La composition et la navigation du menu principal (GDD §4.6).
/// </summary>
public class MenuPrincipalTests
{
    /// <summary>
    /// « Jouer » en tête : c'est la seule entrée que la quasi totalité des visiteurs d'une page itch
    /// utilisera, et elle doit être sous le curseur au premier affichage.
    /// </summary>
    [Fact]
    public void JouerEstLaPremiereEntree()
    {
        Assert.Equal(EntreeMenu.Jouer, MenuPrincipal.Entrees(true)[0]);
        Assert.Equal(EntreeMenu.Jouer, MenuPrincipal.Entrees(false)[0]);
    }

    /// <summary>
    /// ⚠ Le cas qui motive le paramètre : en WebGL, <c>Application.Quit()</c> ne fait rien. L'entrée
    /// serait un bouton mort — le joueur clique, rien ne se passe, et c'est tout le menu qui perd sa
    /// crédibilité.
    /// </summary>
    [Fact]
    public void QuitterDisparaitQuandLaPlateformeNeSaitPasFermer()
    {
        Assert.DoesNotContain(EntreeMenu.Quitter, MenuPrincipal.Entrees(false));
        Assert.Contains(EntreeMenu.Quitter, MenuPrincipal.Entrees(true));
    }

    /// <summary>Quitter reste en dernier : jamais sous un Entrée tapé par réflexe.</summary>
    [Fact]
    public void QuitterEstLaDerniereEntree()
    {
        var entrees = MenuPrincipal.Entrees(true);
        Assert.Equal(EntreeMenu.Quitter, entrees[entrees.Count - 1]);
    }

    [Fact]
    public void SudDescendDUneEntree()
    {
        int index;
        Assert.True(MenuPrincipal.Deplacer(0, 4, Direction.Sud, out index));
        Assert.Equal(1, index);
    }

    [Fact]
    public void NordRemonteDUneEntree()
    {
        int index;
        Assert.True(MenuPrincipal.Deplacer(2, 4, Direction.Nord, out index));
        Assert.Equal(1, index);
    }

    /// <summary>
    /// Le bouclage vers le bas. Sans lui, marteler la flèche du bas contre la dernière entrée ne
    /// produit rien de visible, et le menu n'a aucun retour de refus pour l'expliquer.
    /// </summary>
    [Fact]
    public void DepuisLaDerniereEntreeSudRevientALaPremiere()
    {
        int index;
        Assert.True(MenuPrincipal.Deplacer(3, 4, Direction.Sud, out index));
        Assert.Equal(0, index);
    }

    /// <summary>
    /// ⚠ Le bouclage vers le haut est le cas qui casse : <c>(0 - 1) % 4</c> vaut <b>-1</b> en C#, et
    /// un index négatif désigne une entrée qui n'existe pas. C'est ce test qui tient le double
    /// modulo de <c>Deplacer</c>.
    /// </summary>
    [Fact]
    public void DepuisLaPremiereEntreeNordVaALaDerniere()
    {
        int index;
        Assert.True(MenuPrincipal.Deplacer(0, 4, Direction.Nord, out index));
        Assert.Equal(3, index);
    }

    /// <summary>
    /// ⚠ Est et Ouest ne bougent rien, et c'est une décision : le joueur d'un jeu de serpent tape
    /// les flèches latérales par réflexe. Les accepter ferait sauter le curseur au moment où il
    /// essaie simplement de tourner.
    /// </summary>
    [Theory]
    [InlineData(Direction.Est)]
    [InlineData(Direction.Ouest)]
    public void LesDirectionsLateralesNeDeplacentRien(Direction direction)
    {
        int index;
        Assert.False(MenuPrincipal.Deplacer(1, 4, direction, out index));
        Assert.Equal(1, index);
    }

    /// <summary>
    /// Le cas réel du bornage : un index mémorisé sur le menu du bureau (4 entrées) appliqué au menu
    /// web (3 entrées). Retomber sur la dernière vaut mieux que lever au démarrage d'un build web.
    /// </summary>
    [Fact]
    public void UnIndexHorsBornesRetombeSurLaDerniereEntree()
    {
        Assert.Equal(2, MenuPrincipal.Borner(3, 3));
        Assert.Equal(0, MenuPrincipal.Borner(-1, 3));
        Assert.Equal(1, MenuPrincipal.Borner(1, 3));
    }

    /// <summary>Un menu vide est un défaut de composition, pas une entrée du joueur : il lève.</summary>
    [Fact]
    public void UnMenuSansEntreeLeve()
    {
        int index;
        Assert.Throws<ArgumentOutOfRangeException>(() => MenuPrincipal.Deplacer(0, 0, Direction.Sud, out index));
        Assert.Throws<ArgumentOutOfRangeException>(() => MenuPrincipal.Borner(0, 0));
    }
}
