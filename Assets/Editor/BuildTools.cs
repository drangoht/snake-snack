using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SnakeSnack.EditorTools
{
    /// <summary>
    /// Génération des builds Windows et web, utilisable depuis le menu de l'éditeur ou en ligne de
    /// commande (<c>-executeMethod</c>).
    ///
    /// <para>Tout ce qui compte pour un build est posé <b>ici, par du code</b>, et non laissé aux
    /// réglages de l'éditeur : un réglage fait à la souris ne vaut que sur le poste où il a été
    /// fait, et se perd au premier clone du dépôt.</para>
    /// </summary>
    public static class BuildTools
    {
        const string OutputDirectory = "Build/Windows";
        const string WebOutputDirectory = "Build/Web";
        const string ExecutableName = "SnakeSnack.exe";
        const string ShaAssetPath = "Assets/Resources/build_sha.txt";

        // ------------------------------------------------------------------ points d'entrée CLI

        /// <summary>Pipeline URP + scène régénérée + build Windows. Point d'entrée en ligne de commande.</summary>
        public static void RebuildEverything()
        {
            RenderPipelineSetup.Apply();
            SceneBuilder.Build();
            BuildWindows();
        }

        /// <summary>Pipeline URP + scène régénérée + build web. Point d'entrée en ligne de commande.</summary>
        public static void RebuildWeb()
        {
            RenderPipelineSetup.Apply();
            SceneBuilder.Build();
            BuildWeb();
        }

        // ------------------------------------------------------------------ Windows

        [MenuItem("Snake Snack/Compiler le build Windows")]
        public static void BuildWindows()
        {
            ConfigurePlayerSettings();
            StampGitSha();
            Directory.CreateDirectory(OutputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.ScenePath },
                locationPathName = OutputDirectory + "/" + ExecutableName,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                // Le script de publication cherche cette phrase exacte dans le journal : un code
                // retour nul ne distingue pas « construit » de « rien à faire ».
                Debug.Log($"Build Windows reussi : {summary.outputPath} ({summary.totalSize / 1024 / 1024} Mo)");
                WriteBuildStamp(OutputDirectory);
            }
            else
            {
                Debug.LogError($"Build Windows en echec : {summary.result} ({summary.totalErrors} erreurs)");
            }
        }

        // ------------------------------------------------------------------ web

        /// <summary>
        /// Compile la version jouable dans un navigateur. Sortie : <c>Build/Web</c>, à pousser telle
        /// quelle sur itch.io.
        /// </summary>
        [MenuItem("Snake Snack/Compiler la version web")]
        public static void BuildWeb()
        {
            ConfigurePlayerSettings();
            StampGitSha();
            ApplyWebSettings();
            Directory.CreateDirectory(WebOutputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.ScenePath },
                // ⚠ En WebGL, Unity attend un DOSSIER et non un fichier : il y écrit index.html et Build/.
                locationPathName = WebOutputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Build web en echec : {summary.result} ({summary.totalErrors} erreurs)");
                return;
            }

            Debug.Log($"Build web reussi : {summary.outputPath} ({summary.totalSize / 1024 / 1024} Mo)");
            WriteBuildStamp(WebOutputDirectory);
            StampWebCacheBuster(WebOutputDirectory);
        }

        /// <summary>
        /// Réglages du lecteur web. Chacun corrige un défaut qui ne se voit pas à la compilation :
        /// ils produisent un jeu qui démarre, puis se comporte mal.
        /// </summary>
        static void ApplyWebSettings()
        {
            NamedBuildTarget web = NamedBuildTarget.WebGL;

            // Brotli comprime nettement mieux que gzip sur du WebAssembly, mais le navigateur ne sait
            // le décompresser que si le serveur annonce l'encodage. Le repli JS rend le build
            // indépendant de cette configuration : il tourne sur itch.io comme sur n'importe quel
            // hébergement statique.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Sans ce cache, l'audio et les textures du .data se retéléchargent à chaque visite.
            PlayerSettings.WebGL.dataCaching = true;

            // À relever si le jeu alloue beaucoup : trop bas, le tas croît par paliers en cours de
            // partie (micro-freezes) ; trop haut, le chargement coûte plus cher.
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 512;

            // ⚠ WebGL est la seule plateforme dont le niveau de stripping par défaut est le plus
            // agressif. L'Input System résout ses couches de contrôle par réflexion : au niveau
            // élevé, le jeu démarre normalement et ne répond plus au clavier.
            PlayerSettings.SetManagedStrippingLevel(web, ManagedStrippingLevel.Low);

            // Les exceptions explicitement levées gardent leur pile dans la console du navigateur :
            // seul moyen d'instruire un défaut qu'on ne reproduit pas hors du navigateur.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            // La toile par défaut d'Unity est en 960 x 600 (16/10) : un jeu composé pour du 16/9 s'y
            // retrouve bordé de bandes. Ces deux valeurs alimentent aussi le cadrage de la page hôte.
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;

            // Le gabarit du projet : Assets/WebGLTemplates/SnakeSnack/. Il porte le cadrage, la
            // confiscation des touches détournées par le navigateur, le réveil du contexte audio,
            // les gardes tactiles et la garde-cache.
            PlayerSettings.WebGL.template = "PROJECT:SnakeSnack";
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;

            Debug.Log($"Reglages web : tas {PlayerSettings.WebGL.initialMemorySize} Mo, " +
                      $"{PlayerSettings.WebGL.compressionFormat} (repli {PlayerSettings.WebGL.decompressionFallback}), " +
                      $"stripping Low, gabarit {PlayerSettings.WebGL.template}.");
        }

        [MenuItem("Snake Snack/Appliquer les reglages du projet")]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Drangoht";
            PlayerSettings.productName = "Snake Snack";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultIsNativeResolution = false;
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ tampon de build

        /// <summary>
        /// Pose l'identité git du code qu'on s'apprête à construire, dans la ressource que le jeu lit
        /// pour afficher son tampon. Appelé <b>avant</b> le build, sans quoi le binaire embarquerait
        /// la valeur précédente.
        /// </summary>
        /// <remarks>
        /// Écrite ici et non par le script de publication : posée seulement au moment de publier,
        /// elle resterait ensuite en place, et tout build local suivant afficherait le SHA de la
        /// dernière release — un garde-fou de fraîcheur qui se trompe est pire que pas de garde-fou,
        /// puisqu'on lui fait confiance.
        /// </remarks>
        static void StampGitSha()
        {
            string sha = Git("rev-parse --short HEAD");

            if (sha.Length == 0)
            {
                // Pas de dépôt, ou pas de git dans le PATH : « dev » avoue l'ignorance, là où un SHA
                // périmé prétendrait savoir.
                sha = "dev";
            }
            else if (HasLocalChanges())
            {
                sha += "+";
            }

            string full = Path.GetFullPath(ShaAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));

            bool isNew = !File.Exists(full);
            File.WriteAllText(full, sha);

            // Le fichier est ignoré par git : sur un clone frais il n'existe pas encore, et la base
            // d'assets ne le connaît donc pas — un ImportAsset seul ne l'y ferait pas entrer.
            if (isNew) AssetDatabase.Refresh();

            // Sans réimport, le build embarquerait la valeur que la base d'assets a en mémoire.
            AssetDatabase.ImportAsset(ShaAssetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"Identite git : {sha}");
        }

        /// <summary>
        /// Le dépôt porte-t-il des modifications autres que celles que le build pose lui-même ?
        /// </summary>
        /// <remarks>
        /// Trois fichiers sont exclus du constat parce qu'ils sont des <b>artefacts</b> et non des
        /// sources : le tampon et le numéro de version, posés juste avant de construire, et la scène,
        /// que <see cref="SceneBuilder"/> régénère de zéro (donc avec de nouveaux identifiants
        /// d'objets, donc un diff garanti). Sans ces exclusions, tout build se déclarerait issu d'un
        /// arbre modifié, y compris sur un dépôt parfaitement propre — et l'avertissement qui doit
        /// signaler un vrai écart ne voudrait plus rien dire.
        /// </remarks>
        static bool HasLocalChanges()
        {
            foreach (string line in Git("status --porcelain").Split('\n'))
            {
                string entry = line.Trim();
                if (entry.Length == 0) continue;

                // « XY chemin » : le statut tient sur les deux premières colonnes.
                string path = entry.Length > 2 ? entry.Substring(2).Trim().Replace('\\', '/') : "";

                if (path.EndsWith("Assets/Resources/build_sha.txt", StringComparison.Ordinal)) continue;
                if (path.EndsWith("ProjectSettings/ProjectSettings.asset", StringComparison.Ordinal)) continue;
                if (path.EndsWith(SceneBuilder.ScenePath, StringComparison.Ordinal)) continue;

                return true;
            }

            return false;
        }

        /// <summary>Écrit, à côté du build, la carte d'identité de ce qui vient d'être construit.</summary>
        /// <remarks>
        /// C'est le seul contrôle honnête de fraîcheur : les métadonnées d'un binaire Unity décrivent
        /// le <i>moteur</i> et non le jeu, et l'horodatage ne vaut pas mieux, le build étant
        /// incrémental — un fichier identique n'est pas réécrit. Ce tampon-ci est produit par le
        /// build : il ne peut pas annoncer une version que le build n'a pas posée. Le script de
        /// publication le lit avant de pousser.
        /// </remarks>
        static void WriteBuildStamp(string directory)
        {
            string sha = ReadSha();
            string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            string json = "{\n" +
                          $"  \"version\": \"{PlayerSettings.bundleVersion}\",\n" +
                          $"  \"sha\": \"{sha}\",\n" +
                          $"  \"date\": \"{date}\",\n" +
                          $"  \"engine\": \"{Application.unityVersion}\"\n" +
                          "}\n";

            File.WriteAllText(Path.Combine(directory, "build_stamp.json"), json);
            Debug.Log($"Tampon de build : v{PlayerSettings.bundleVersion}-{sha}");
        }

        /// <summary>Remplace <c>__BUILD_ID__</c> dans la page par une empreinte propre à ce build.</summary>
        /// <remarks>
        /// Sans elle, un navigateur qui a déjà vu la page ressert le chargeur d'un build et le wasm
        /// d'un autre : le jeu ne démarre plus, et le seul indice est un message d'erreur qui ne
        /// change pas alors que le build, lui, a changé. L'horodatage s'ajoute au SHA parce que deux
        /// builds locaux d'affilée partagent le même commit et doivent quand même se distinguer ; il
        /// invalide aussi le cache IndexedDB d'Unity, qui indexe par URL.
        /// </remarks>
        static void StampWebCacheBuster(string directory)
        {
            string indexPath = Path.Combine(directory, "index.html");
            if (!File.Exists(indexPath))
            {
                Debug.LogWarning("index.html introuvable : pas de garde-cache posee.");
                return;
            }

            string buildId = ReadSha() + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string html = File.ReadAllText(indexPath);

            if (!html.Contains("__BUILD_ID__"))
            {
                // Le gabarit a été modifié sans que le jeton y survive : le dire fort, sans quoi le
                // défaut ne se manifestera que chez un joueur, sous la forme d'un jeu qui ne démarre pas.
                Debug.LogWarning("__BUILD_ID__ absent du gabarit : le navigateur pourra melanger deux builds.");
                return;
            }

            File.WriteAllText(indexPath, html.Replace("__BUILD_ID__", buildId));
            Debug.Log($"Garde-cache : {buildId}");
        }

        static string ReadSha()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ShaAssetPath);
            return asset != null && asset.text.Trim().Length > 0 ? asset.text.Trim() : "dev";
        }

        /// <summary>Exécute une commande git à la racine du projet. Chaîne vide si git est indisponible.</summary>
        static string Git(string arguments)
        {
            try
            {
                var info = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = Process.Start(info);
                if (process == null) return string.Empty;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);
                return process.ExitCode == 0 ? output.Trim() : string.Empty;
            }
            catch (Exception error)
            {
                Debug.LogWarning($"git indisponible : {error.Message}");
                return string.Empty;
            }
        }
    }
}
