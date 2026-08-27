using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Le score et le record (GDD §4.5).
/// </summary>
public class ScoreTests
{
    [Fact]
    public void UnePartieCommenceAZero()
    {
        var score = new Score();

        Assert.Equal(0, score.Points);
        Assert.Equal(0, score.Record);
        Assert.False(score.RecordBattu);
    }

    [Fact]
    public void ChaquePommeVautExactementUnPoint()
    {
        var score = new Score();

        for (int i = 1; i <= 5; i++)
        {
            score.CompterUnePomme();
            Assert.Equal(i, score.Points);
        }
    }

    /// <summary>
    /// Le §4.5 : « le record monte pendant la partie, dès que le score courant le dépasse — pas à la
    /// mort ». Un record qui resterait sous le score affiché à côté de lui se lit comme un bug.
    /// </summary>
    [Fact]
    public void LeRecordMonteAvecLeScoreDesQuIlEstDepasse()
    {
        var score = new Score(2);

        score.CompterUnePomme();
        Assert.Equal(2, score.Record);

        score.CompterUnePomme();
        Assert.Equal(2, score.Record);

        score.CompterUnePomme();
        Assert.Equal(3, score.Record);
        Assert.Equal(3, score.Points);
    }

    /// <summary>
    /// Le retour de <c>CompterUnePomme</c> est le signal d'écriture persistante : il ne doit être
    /// vrai qu'aux ticks où le record change réellement de valeur, sinon le jeu écrit le stockage à
    /// chaque pomme de chaque partie.
    /// </summary>
    [Fact]
    public void LeSignalDeMonteeDuRecordNEstVraiQuAuTickOuIlChange()
    {
        var score = new Score(2);

        Assert.False(score.CompterUnePomme());
        Assert.False(score.CompterUnePomme());
        Assert.True(score.CompterUnePomme());
        Assert.True(score.CompterUnePomme());
    }

    [Fact]
    public void LeRecordSurvitAUneNouvellePartie()
    {
        var score = new Score();
        score.CompterUnePomme();
        score.CompterUnePomme();
        score.CompterUnePomme();

        score.NouvellePartie();

        Assert.Equal(0, score.Points);
        Assert.Equal(3, score.Record);
        Assert.False(score.RecordBattu);
    }

    [Fact]
    public void UneMauvaisePartieNeFaitPasDescendreLeRecord()
    {
        var score = new Score(5);
        score.NouvellePartie();

        score.CompterUnePomme();
        score.CompterUnePomme();

        Assert.Equal(2, score.Points);
        Assert.Equal(5, score.Record);
        Assert.False(score.RecordBattu);
    }

    /// <summary>
    /// ⚠ Le cas qui se perd quand on écrit <c>RecordBattu</c> comme <c>Points == Record</c> : égaler
    /// son meilleur score met bien les deux nombres à la même valeur, mais ne bat rien. Afficher
    /// « nouveau record » ici ferait mentir le seul moment gratifiant du jeu.
    /// </summary>
    [Fact]
    public void EgalerLeRecordNeLeBatPas()
    {
        var score = new Score(2);
        score.NouvellePartie();

        score.CompterUnePomme();
        score.CompterUnePomme();

        Assert.Equal(score.Record, score.Points);
        Assert.False(score.RecordBattu);
    }

    [Fact]
    public void DepasserLeRecordDUnSeulPointLeBat()
    {
        var score = new Score(2);
        score.NouvellePartie();

        score.CompterUnePomme();
        score.CompterUnePomme();
        score.CompterUnePomme();

        Assert.True(score.RecordBattu);
        Assert.Equal(3, score.Record);
    }

    /// <summary>
    /// La toute première partie d'un joueur, record inconnu à zéro : la première pomme bat déjà le
    /// record. C'est voulu — sinon la mention n'apparaîtrait jamais lors de la partie qui découvre
    /// le jeu.
    /// </summary>
    [Fact]
    public void LaPremierePommeDeLaPremierePartieBatLeRecord()
    {
        var score = new Score();

        score.CompterUnePomme();

        Assert.True(score.RecordBattu);
    }

    /// <summary>
    /// Un record illisible repart de zéro <b>sans erreur bloquante</b> (§4.5) : le jeu ne doit
    /// jamais refuser de démarrer pour un compteur.
    /// </summary>
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(int.MinValue, 0)]
    [InlineData(0, 0)]
    [InlineData(14, 14)]
    public void UnRecordAbimeRepartDeZeroSansLever(int lu, int attendu)
    {
        Assert.Equal(attendu, Score.NormaliserRecord(lu));
        Assert.Equal(attendu, new Score(lu).Record);
    }

    /// <summary>
    /// L'invariant du §4.5 — <c>longueur == 3 + score</c> — vérifié sur le vrai serpent plutôt
    /// qu'affirmé en commentaire : c'est lui qui justifie de ne PAS afficher la longueur, et il
    /// casserait en silence le jour où la croissance passerait au tick suivant.
    /// </summary>
    [Fact]
    public void LaLongueurDuSerpentVautToujoursTroisPlusLeScore()
    {
        Grille grille = Grille.ParDefaut;
        var serpent = new Serpent(grille.PoseDeDepart().Segments);
        var score = new Score();

        Assert.Equal(Score.LongueurDuSerpent(score.Points), serpent.Longueur);

        // Une pomme posée droit devant la tête à chaque tick : le serpent mange à tous les coups,
        // et la grille par défaut laisse dix pas vers l'est avant le mur.
        for (int i = 0; i < 8; i++)
        {
            Case pomme = Directions.Avance(serpent.Tete, Direction.Est);

            bool mange;
            Assert.Equal(ResultatDeplacement.Avance, serpent.Avancer(Direction.Est, grille, pomme, out mange));
            Assert.True(mange);

            score.CompterUnePomme();

            Assert.Equal(Score.LongueurDuSerpent(score.Points), serpent.Longueur);
        }

        Assert.Equal(8, score.Points);
        Assert.Equal(11, serpent.Longueur);
    }
}
