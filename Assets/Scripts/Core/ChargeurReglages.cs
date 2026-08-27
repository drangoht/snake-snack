using System.Collections.Generic;
using System.IO;
using SnakeSnack.Rules;
using UnityEngine;

namespace SnakeSnack.Core
{
    /// <summary>
    /// Lit <c>StreamingAssets/reglages.json</c> — le tuning réglable <b>sans recompiler</b>
    /// (CLAUDE.md, GDD §4.1 et §4.3).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Un fichier absent ou illisible n'est jamais une erreur bloquante.</b> Le jeu doit
    /// démarrer sur les valeurs du GDD, quoi qu'il arrive : refuser de se lancer parce qu'un fichier
    /// de confort manque transformerait un réglage en dépendance dure.
    ///
    /// <para>⚠ <b>En WebGL, ce chargeur ne lit rien</b> et rend les valeurs par défaut :
    /// <c>Application.streamingAssetsPath</c> y est une <i>URL</i>, pas un chemin de fichier, et
    /// <c>File.Exists</c> y répond faux sans lever quoi que ce soit. Régler le jeu en ligne
    /// demanderait un <c>UnityWebRequest</c> asynchrone — non fait, et sans objet tant que le tuning
    /// se fait sur le build bureau.</para>
    /// </remarks>
    public static class ChargeurReglages
    {
        /// <summary>Nom du fichier de tuning, dans <c>Assets/StreamingAssets/</c>.</summary>
        public const string NomFichier = "reglages.json";

        /// <summary>Charge, valide et journalise. Rend toujours un jeu de réglages utilisable.</summary>
        public static ReglagesJeu Charger()
        {
            ReglagesJeu lus = Lire();

            IList<string> anomalies;
            ReglagesJeu valides = lus.Valider(out anomalies);

            // ⚠ Jamais corrigé en silence : sinon le joueur édite son JSON, ne voit rien changer, et
            // n'a aucun moyen de savoir que sa valeur a été refusée.
            for (int i = 0; i < anomalies.Count; i++)
            {
                Debug.LogWarning("[reglages] " + anomalies[i]);
            }

            return valides;
        }

        private static ReglagesJeu Lire()
        {
            string chemin = Path.Combine(Application.streamingAssetsPath, NomFichier);

            if (!File.Exists(chemin))
            {
                Debug.Log("[reglages] " + NomFichier + " absent : valeurs du GDD par defaut.");
                return ReglagesJeu.ParDefaut();
            }

            try
            {
                string json = File.ReadAllText(chemin);
                ReglagesJeu lus = JsonUtility.FromJson<ReglagesJeu>(json);

                if (lus == null)
                {
                    Debug.LogWarning("[reglages] " + NomFichier + " illisible : valeurs par defaut.");
                    return ReglagesJeu.ParDefaut();
                }

                Debug.Log("[reglages] charges depuis " + chemin);
                return lus;
            }
            catch (IOException erreur)
            {
                Debug.LogWarning("[reglages] lecture impossible (" + erreur.Message + ") : valeurs par defaut.");
                return ReglagesJeu.ParDefaut();
            }
        }
    }
}
