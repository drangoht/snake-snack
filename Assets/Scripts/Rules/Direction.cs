namespace SnakeSnack.Rules
{
    /// <summary>Les quatre directions du serpent (GDD §3 : « Tourner (4 directions) »).</summary>
    /// <remarks>
    /// Convention d'axes : <b>Nord = Y croissant</b>, comme l'axe Y d'Unity qui monte à l'écran.
    /// Le rendu n'a donc aucune inversion à faire entre la grille logique et la scène — une
    /// inversion oubliée quelque part se traduirait par un jeu qui répond « à l'envers » sans
    /// lever la moindre erreur.
    /// </remarks>
    public enum Direction
    {
        Nord,
        Est,
        Sud,
        Ouest
    }

    /// <summary>Opérations pures sur <see cref="Direction"/> — aucune dépendance moteur.</summary>
    public static class Directions
    {
        /// <summary>Toutes les directions, dans l'ordre de l'énumération (utile aux tests et à l'UI).</summary>
        public static Direction[] Toutes()
        {
            // Un tableau neuf à chaque appel : un tableau statique partagé serait modifiable par
            // l'appelant, et la table des directions n'a aucune raison d'être un état global.
            return new[] { Direction.Nord, Direction.Est, Direction.Sud, Direction.Ouest };
        }

        /// <summary>Direction opposée (Nord ↔ Sud, Est ↔ Ouest).</summary>
        public static Direction Oppose(Direction direction)
        {
            switch (direction)
            {
                case Direction.Nord: return Direction.Sud;
                case Direction.Sud: return Direction.Nord;
                case Direction.Est: return Direction.Ouest;
                default: return Direction.Est;
            }
        }

        /// <summary>
        /// Vrai si passer de <paramref name="appliquee"/> à <paramref name="demandee"/> est un
        /// demi-tour instantané — le serpent se mangerait la nuque (GDD §3).
        /// </summary>
        /// <remarks>
        /// ⚠ <paramref name="appliquee"/> doit être la direction <b>effectivement appliquée au tick
        /// précédent</b>, jamais la dernière touche tapée : c'est tout l'objet du contre-exemple
        /// Nord/Sud du GDD §4.2. Cette règle est exposée publiquement pour que l'appelant puisse
        /// afficher un refus (§3) sans réimplémenter la comparaison de son côté.
        /// </remarks>
        public static bool EstDemiTour(Direction appliquee, Direction demandee)
        {
            return demandee == Oppose(appliquee);
        }

        /// <summary>
        /// Sens du virage entre deux directions successives : <c>+1</c> vers la gauche
        /// (anti-horaire), <c>-1</c> vers la droite (horaire), <c>0</c> si le serpent va tout droit.
        /// </summary>
        /// <remarks>
        /// Le signe suit la convention d'angle d'Unity — Z croissant tourne dans le sens
        /// anti-horaire —, ce qui permet à l'appelant de multiplier directement par un angle en
        /// degrés (<c>docs/art/juicy.md</c> §9) sans réinventer l'orientation.
        ///
        /// <para>⚠ <b>Un demi-tour rend 0, pas un sens arbitraire.</b> Il ne peut pas arriver en
        /// jeu — la file le refuse au tick (GDD §4.2) — mais s'il arrivait, choisir la gauche ou la
        /// droite serait une invention : les deux quarts de tour sont également faux. Zéro veut
        /// dire « pas de virage à montrer », et c'est la seule réponse honnête ici.</para>
        ///
        /// <para>⚠ <b>Purement présentation.</b> Aucune règle ne lit ce sens : la trajectoire est
        /// déjà décidée par la direction appliquée au tick (<c>juicy.md</c> §11).</para>
        /// </remarks>
        public static int SensDuVirage(Direction avant, Direction apres)
        {
            // L'énumération est ordonnée dans le sens horaire (Nord, Est, Sud, Ouest) : un pas en
            // avant dans cette liste est un virage à droite, un pas en arrière un virage à gauche.
            int quarts = (((int)apres - (int)avant) + 4) % 4;

            switch (quarts)
            {
                case 1: return -1;
                case 3: return 1;
                default: return 0;
            }
        }

        /// <summary>Déplacement d'une case dans cette direction.</summary>
        public static Case Deplacement(Direction direction)
        {
            switch (direction)
            {
                case Direction.Nord: return new Case(0, 1);
                case Direction.Sud: return new Case(0, -1);
                case Direction.Est: return new Case(1, 0);
                default: return new Case(-1, 0);
            }
        }

        /// <summary>Case atteinte depuis <paramref name="depart"/> après un pas dans cette direction.</summary>
        public static Case Avance(Case depart, Direction direction)
        {
            return depart.Plus(Deplacement(direction));
        }
    }
}
