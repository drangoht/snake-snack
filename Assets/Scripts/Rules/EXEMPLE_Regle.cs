namespace SnakeSnack.Rules
{
    /// <summary>
    /// GABARIT — a copier, puis supprimer ce fichier.
    ///
    /// <para>Toute regle chiffree du jeu (courbe, seuil, table, formule) vit dans ce dossier, en
    /// classe <b>statique, sans aucun <c>using UnityEngine</c></b>. C'est ce qui la rend testable
    /// en quelques millisecondes par <c>dotnet test</c>, sans moteur et sans build : les
    /// <c>MonoBehaviour</c> delegent ici et se contentent du travail moteur.</para>
    ///
    /// <para>Une classe de <c>Rules/</c> qui aurait besoin du moteur signale un mauvais decoupage :
    /// c'est a l'appelant de faire la partie moteur.</para>
    /// </summary>
    public static class ExempleRegle
    {
        /// <summary>Points necessaires pour atteindre le niveau donne (niveau 1 = 0 point).</summary>
        /// <remarks>
        /// Courbe quadratique douce : le palier grandit sans jamais doubler d'un niveau a l'autre,
        /// ce qui evite le mur de progression au milieu de la partie.
        /// </remarks>
        public static int SeuilDeNiveau(int niveau)
        {
            if (niveau <= 1) return 0;
            int n = niveau - 1;
            return 5 * n * n + 10 * n;
        }
    }
}
