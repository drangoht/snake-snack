namespace SnakeSnack.Rules
{
    /// <summary>
    /// Le score de la partie en cours et le record de toutes les parties (GDD §4.5).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Le record monte PENDANT la partie</b>, dès que le score courant le dépasse — pas à la
    /// mort. Le score est monotone croissant : attendre la fin ferait afficher un record inférieur
    /// au score affiché juste à côté, ce qui se lit comme un défaut d'affichage et non comme une
    /// règle. Et le record d'un onglet fermé en cours de partie serait perdu.
    ///
    /// <para>⚠ <b>« Record battu » se juge contre le record d'AVANT la partie</b>, jamais contre
    /// <see cref="Record"/> : celui-ci vient d'être relevé par le score courant, les comparer
    /// donnerait toujours faux. C'est ce prédicat qui déclenche la mention « nouveau record » de
    /// l'écran de fin, sans laquelle deux nombres égaux côte à côte passent pour un bug (§4.5).</para>
    ///
    /// <para>La lecture et l'écriture persistantes vivent hors de <c>Rules/</c> : cette classe reçoit
    /// le record connu à la construction et n'a aucune idée d'où il vient.</para>
    /// </remarks>
    public sealed class Score
    {
        private int _points;
        private int _record;
        private int _recordAvantLaPartie;

        /// <param name="recordConnu">
        /// Record lu au démarrage. ⚠ <b>Normalisé, jamais refusé</b> : le jeu ne doit pas refuser de
        /// démarrer pour un compteur (§4.5), et en WebGL ce stockage peut disparaître ou revenir
        /// abîmé.
        /// </param>
        public Score(int recordConnu = 0)
        {
            _record = NormaliserRecord(recordConnu);
            _recordAvantLaPartie = _record;
            _points = 0;
        }

        /// <summary>Pommes mangées dans la partie en cours, +1 par pomme, rien d'autre (§4.5).</summary>
        public int Points
        {
            get { return _points; }
        }

        /// <summary>Le plus haut score jamais atteint, la partie en cours comprise.</summary>
        public int Record
        {
            get { return _record; }
        }

        /// <summary>
        /// Vrai dès que la partie en cours a dépassé le record qu'elle a trouvé en commençant.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Égaler le record ne le bat pas.</b> Le joueur qui refait exactement son meilleur
        /// score voit bien deux nombres identiques, sans mention « nouveau record » : il n'a rien
        /// battu. Ce cas est le seul où l'égalité des deux nombres n'est pas la trace d'un record
        /// neuf, et il est écrit exprès plutôt que déduit d'une comparaison à <see cref="Record"/>.
        /// </remarks>
        public bool RecordBattu
        {
            get { return _points > _recordAvantLaPartie; }
        }

        /// <summary>
        /// Remet le score à zéro pour une nouvelle partie. Le record, lui, survit.
        /// </summary>
        public void NouvellePartie()
        {
            _points = 0;
            _recordAvantLaPartie = _record;
        }

        /// <summary>
        /// Compte une pomme mangée.
        /// </summary>
        /// <returns>
        /// Vrai si le record vient de monter d'un cran — c'est le signal qui déclenche l'écriture
        /// persistante, et il est rendu ici pour que l'appelant n'ait pas à comparer le record à sa
        /// valeur précédente qu'il aurait fallu retenir de son côté.
        /// </returns>
        public bool CompterUnePomme()
        {
            _points++;

            if (_points > _record)
            {
                _record = _points;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Le record utilisable à partir d'une valeur venue du stockage.
        /// </summary>
        /// <remarks>
        /// ⚠ Un record absent ou abîmé <b>repart de zéro sans erreur bloquante</b> (§4.5). Une
        /// valeur négative n'est pas un score possible : elle vient d'un stockage corrompu ou d'une
        /// clé écrite par autre chose, et la laisser passer afficherait « Record -1 » à l'écran.
        /// </remarks>
        public static int NormaliserRecord(int valeur)
        {
            return valeur < 0 ? 0 : valeur;
        }

        /// <summary>
        /// Longueur du serpent pour ce score (§4.5 : la longueur vaut <c>3 + score</c>).
        /// </summary>
        /// <remarks>
        /// Cette égalité est la raison pour laquelle le jeu n'affiche <b>pas</b> la longueur : ce
        /// serait un second nombre à lire pour la même information. Elle est écrite ici pour être
        /// vérifiable par un test plutôt que rappelée dans un commentaire.
        /// </remarks>
        public static int LongueurDuSerpent(int points)
        {
            return Grille.LongueurInitiale + NormaliserRecord(points);
        }
    }
}
