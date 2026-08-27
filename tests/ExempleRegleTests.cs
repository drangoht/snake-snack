using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// GABARIT — a copier, puis supprimer avec la regle qu'il accompagne.
///
/// Un test ne vise pas la couverture de lignes : il verrouille ce que le DESIGN interdit. Un test
/// qui paraphrase l'implementation casse a chaque refonte sans jamais rien attraper ; celui qui
/// dit « la courbe ne doit jamais doubler d'un niveau a l'autre » attrape la vraie regression.
/// </summary>
public class ExempleRegleTests
{
    [Fact]
    public void LePremierNiveauNeCouteRien()
    {
        Assert.Equal(0, ExempleRegle.SeuilDeNiveau(1));
        Assert.Equal(0, ExempleRegle.SeuilDeNiveau(0));
    }

    [Fact]
    public void LaCourbeEstStrictementCroissante()
    {
        for (int niveau = 1; niveau < 50; niveau++)
        {
            Assert.True(ExempleRegle.SeuilDeNiveau(niveau + 1) > ExempleRegle.SeuilDeNiveau(niveau),
                $"Le niveau {niveau + 1} ne coute pas plus que le {niveau}.");
        }
    }

    [Fact]
    public void AucunPalierNeDoubleParRapportAuPrecedent()
    {
        // L'intention de design : la progression ralentit, mais jamais d'un coup. C'est cette
        // phrase-la que le test protege, pas la formule.
        for (int niveau = 2; niveau < 50; niveau++)
        {
            int precedent = ExempleRegle.SeuilDeNiveau(niveau) - ExempleRegle.SeuilDeNiveau(niveau - 1);
            int courant = ExempleRegle.SeuilDeNiveau(niveau + 1) - ExempleRegle.SeuilDeNiveau(niveau);
            Assert.True(courant < precedent * 2, $"Le palier double au niveau {niveau + 1}.");
        }
    }
}
