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
    /// de <c>docs/ART.md</c> §5.7 et de <c>docs/PITFALLS_UNITY.md</c> : les flèches Unicode
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
        /// <c>docs/PITFALLS_UNITY.md</c> : une capacité qui n'annonce pas sa touche n'existe pas
        /// pour le joueur).
        /// </summary>
        public const string RappelDesCommandes = "Fleches ou ZQSD : diriger   -   Echap : pause   -   Espace : relancer";

        /// <summary>Titre de l'écran de pause.</summary>
        public const string TitrePause = "PAUSE";

        /// <summary>Sous-titre de l'écran de pause.</summary>
        public const string SousTitrePause = "Echap pour reprendre";

        /// <summary>
        /// La ligne de refus de l'écran de pause (<c>docs/ART.md</c> §5.4, mot pour mot).
        /// </summary>
        public const string RefusEnPause = "Touche ignorée - le jeu est en pause";

        /// <summary>
        /// Message de mort. ⚠ Le GDD §2 prévoit « score et record affichés sur place » : ils
        /// <b>manquent</b> ici, et c'est attendu — sans pomme, il n'y a ni croissance ni score dans
        /// ce lot. À compléter quand la pomme sera spécifiée.
        /// </summary>
        public const string TitreMort = "PERDU";

        /// <summary>Relance à une touche, zéro attente (GDD §2).</summary>
        public const string SousTitreMort = "Espace pour rejouer";
    }
}
