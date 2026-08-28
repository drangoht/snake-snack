using System.Globalization;
using SnakeSnack.Rules;

namespace SnakeSnack.UI
{
    /// <summary>
    /// Toutes les chaînes affichées par le jeu, en un seul endroit.
    /// </summary>
    /// <remarks>
    /// Le jeu n'est <b>pas localisé</b> au 2026-08-27 : il n'existe aucun système de traduction, et
    /// le GDD n'en demande pas. Les chaînes vivent quand même ici plutôt qu'au fil du code, pour que
    /// le jour où la localisation arrive, il y ait <b>un</b> fichier à reprendre et pas quinze
    /// littéraux disséminés dans des <c>MonoBehaviour</c>.
    ///
    /// <para>⚠ <b>Aucun symbole hors ASCII autre que les accents français.</b> Interdits explicites
    /// de <c>docs/ART.md</c> §5.7 et de <c>docs/pitfalls/polices-texte.md</c> : les flèches Unicode
    /// (← → ↑ ↓) sont perdues <b>en silence</b> par un build WebGL, sans carré blanc ni
    /// avertissement. Tout symbole directionnel est un <b>sprite</b>, jamais un caractère. Le tiret
    /// cadratin (—) est proscrit pour la même raison : un tiret simple le remplace partout.</para>
    ///
    /// <para>⚠ Les accents, eux, sont conservés (le brief §5.4 les écrit lui-même dans son exemple
    /// de message) mais ils restent à <b>vérifier dans le navigateur</b>, pas au raisonnement — le
    /// piège des glyphes manquants ne se voit pas sur le bureau.</para>
    /// </remarks>
    public static class TextesUi
    {
        // --- Menu principal (GDD §4.6) ---------------------------------------------------

        /// <summary>Titre du jeu, sur le menu principal. Le même mot que la page itch.</summary>
        public const string TitreDuJeu = "SNAKE SNACK";

        /// <summary>
        /// L'accroche sous le titre : le pitch du GDD §1 réduit à ce qui se lit en une seconde. Elle
        /// dit la <b>conséquence</b> de manger, pas la commande — c'est la conséquence qui fait
        /// comprendre pourquoi la partie finit toujours mal.
        /// </summary>
        public const string AccrocheMenu = "Il s'allonge à chaque bouchée.";

        /// <summary>Rappel des touches du menu, en pied d'écran (« invisible se lit inexistant »).</summary>
        public const string PiedDeMenu = "Flèches ou ZQSD pour choisir   -   Entrée ou Espace pour valider";

        /// <summary>Le libellé d'une entrée du menu (GDD §4.6).</summary>
        /// <remarks>
        /// ⚠ Un <c>switch</c> exhaustif avec un défaut qui <b>se voit</b> : une entrée ajoutée à
        /// <see cref="EntreeMenu"/> et oubliée ici s'afficherait sinon comme une ligne vide, et une
        /// ligne vide dans un menu se lit comme un défaut d'affichage, pas comme un oubli de texte.
        /// </remarks>
        public static string LibelleEntree(EntreeMenu entree)
        {
            switch (entree)
            {
                case EntreeMenu.Jouer: return "Jouer";
                case EntreeMenu.CommentJouer: return "Comment jouer";
                case EntreeMenu.Credits: return "Crédits";
                case EntreeMenu.Quitter: return "Quitter";
                default: return "(entrée sans libellé : " + entree + ")";
            }
        }

        /// <summary>Titre du panneau des commandes.</summary>
        public const string TitreCommentJouer = "COMMENT JOUER";

        /// <summary>
        /// Le panneau des commandes : les touches du GDD §3, puis les deux règles qui tuent.
        /// </summary>
        /// <remarks>
        /// ⚠ Aucune flèche Unicode (§5.7) : « Flèches » est écrit en toutes lettres. Et le refus du
        /// demi-tour est annoncé <b>ici</b> plutôt que découvert en jeu — un joueur qui voit son
        /// appui ignoré sans explication conclut que le jeu a raté sa touche.
        /// </remarks>
        public const string CorpsCommentJouer =
            "Flèches ou ZQSD : diriger le serpent\n" +
            "Échap : mettre en pause\n" +
            "Espace : relancer une partie\n" +
            "\n" +
            "Le serpent avance seul, une case à la fois.\n" +
            "Chaque pomme l'allonge d'un segment et vaut un point.\n" +
            "\n" +
            "Les bords tuent : ils ne téléportent pas, et se mordre le corps tue aussi.\n" +
            "Le demi-tour instantané est refusé : un chevron barré le signale.";

        /// <summary>Titre du panneau des crédits.</summary>
        public const string TitreCredits = "CRÉDITS";

        /// <summary>
        /// Les crédits affichés en jeu.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Ce texte n'est pas décoratif : c'est une obligation de licence.</b> La SIL OFL 1.1
        /// de Nunito exige l'attribution, et <c>docs/CREDITS.md</c> tient la liste de référence.
        /// Toute ressource tierce ajoutée au jeu s'ajoute aux deux endroits, dans le même commit.
        /// </remarks>
        public const string CorpsCredits =
            "Snake Snack - un jeu de Drangoht.\n" +
            "\n" +
            "Police : Nunito, par Vernon Adams, Cyreal et Jacques Le Bailly.\n" +
            "SIL Open Font License 1.1.\n" +
            "\n" +
            "Illustration et interface produites pour ce jeu.\n" +
            "Moteur : Unity.";

        /// <summary>Pied des panneaux du menu.</summary>
        public const string RetourPanneau = "Échap pour revenir";

        // --- Jeu -------------------------------------------------------------------------

        /// <summary>
        /// Bandeau, avant le premier appui. Le §4.1 fait démarrer la partie sur la première
        /// direction applicable : le joueur doit savoir qu'on l'attend, sinon il croit à un gel.
        /// </summary>
        public const string EtatEnAttente = "Une direction pour commencer";

        /// <summary>Bandeau, partie en cours. Vide : rien à dire tant que tout va bien.</summary>
        public const string EtatEnCours = "";

        /// <summary>Bandeau, jeu en pause.</summary>
        public const string EtatEnPause = "Pause";

        /// <summary>Bandeau, après la mort.</summary>
        public const string EtatMort = "Perdu";

        /// <summary>
        /// Rappel permanent des commandes (GDD §3, et le piège « invisible se lit inexistant » de
        /// <c>docs/pitfalls/interface.md</c> : une capacité qui n'annonce pas sa touche n'existe pas
        /// pour le joueur).
        /// </summary>
        public const string RappelDesCommandes = "Flèches ou ZQSD : diriger   -   Échap : pause   -   Espace : relancer";

        /// <summary>Titre de l'écran de pause.</summary>
        public const string TitrePause = "PAUSE";

        /// <summary>
        /// Sous-titre de l'écran de pause.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Retour arrière est annoncé ici, et nulle part ailleurs</b> : c'est le seul chemin du
        /// jeu vers le menu depuis une partie en cours (GDD §4.6). Une touche qui ne s'annonce pas
        /// n'existe pas pour le joueur (<c>docs/pitfalls/interface.md</c>) — et celle-ci ne peut
        /// s'annoncer que sur l'écran de pause, puisqu'elle n'agit que là.
        /// </remarks>
        public const string SousTitrePause = "Échap pour reprendre   -   Retour arrière pour le menu";

        /// <summary>
        /// La ligne de refus de l'écran de pause (<c>docs/ART.md</c> §5.4, mot pour mot).
        /// </summary>
        public const string RefusEnPause = "Touche ignorée - le jeu est en pause";

        /// <summary>
        /// Message de mort. Le GDD §2 veut « score et record affichés sur place » : ils le sont, par
        /// le récapitulatif de <see cref="RecapitulatifDeFin"/> posé juste sous ce titre.
        /// </summary>
        public const string TitreMort = "PERDU";

        /// <summary>Relance à une touche, zéro attente (GDD §2).</summary>
        public const string SousTitreMort = "Espace pour rejouer   -   Échap pour le menu";

        /// <summary>Bandeau, grille remplie (GDD §4.4).</summary>
        public const string EtatVictoire = "Grille remplie";

        /// <summary>
        /// Titre de la victoire. Libellé <b>distinct</b> de celui de la mort (§4.4) : même écran,
        /// même place, même relance — mais rien ne doit laisser croire qu'une partie parfaite a mal
        /// fini.
        /// </summary>
        public const string TitreVictoire = "GAGNÉ";

        /// <summary>Sous-titre de la victoire : le serpent occupe toute la grille.</summary>
        public const string SousTitreVictoire = "Plus une seule case libre - Espace pour rejouer   -   Échap pour le menu";

        /// <summary>Bandeau, score de la partie en cours (GDD §4.5).</summary>
        public static string LigneScore(int points)
        {
            return "Score " + points.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Bandeau, record de toutes les parties (GDD §4.5).</summary>
        public static string LigneRecord(int record)
        {
            return "Record " + record.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Le récapitulatif de l'écran de fin, mort ou victoire.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>La mention « nouveau record » n'est pas un ornement</b> (GDD §4.5) : quand le record
        /// vient d'être battu, score et record portent le <b>même nombre</b>, et deux valeurs
        /// identiques côte à côte se lisent comme un défaut d'affichage. Sans cette phrase, le seul
        /// moment gratifiant du jeu passe pour un bug. On n'affiche alors qu'un seul nombre : le
        /// répéter sous deux étiquettes serait exactement la confusion qu'on cherche à lever.
        /// </remarks>
        public static string RecapitulatifDeFin(int points, int record, bool recordBattu)
        {
            if (recordBattu)
            {
                return "Nouveau record : " + points.ToString(CultureInfo.InvariantCulture);
            }

            return LigneScore(points) + "   -   " + LigneRecord(record);
        }
    }
}
