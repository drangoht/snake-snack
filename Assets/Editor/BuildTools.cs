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
    /// Produces the Windows and web builds, usable from the editor menu or from the command line
    /// (<c>-executeMethod</c>).
    ///
    /// <para>Everything that matters to a build is set <b>here, in code</b>, and not left to editor
    /// settings: a setting made with a mouse only holds on the machine where it was made, and is lost
    /// on the first clone of the repository.</para>
    /// </summary>
    public static class BuildTools
    {
        const string OutputDirectory = "Build/Windows";
        const string WebOutputDirectory = "Build/Web";
        const string ExecutableName = "SnakeSnack.exe";
        const string ShaAssetPath = "Assets/Resources/build_sha.txt";

        // ------------------------------------------------------------------ CLI entry points

        /// <summary>URP pipeline + regenerated scene + Windows build. Command-line entry point.</summary>
        public static void RebuildEverything()
        {
            RenderPipelineSetup.Apply();
            SceneBuilder.Build();
            BuildWindows();
        }

        /// <summary>URP pipeline + regenerated scene + web build. Command-line entry point.</summary>
        public static void RebuildWeb()
        {
            RenderPipelineSetup.Apply();
            SceneBuilder.Build();
            BuildWeb();
        }

        // ------------------------------------------------------------------ Windows

        [MenuItem("Snake Snack/Build for Windows")]
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
                // ⚠ CONTRACT WITH tools/build.ps1: it searches the log for this exact phrase. A zero
                // exit code does not tell "built" apart from "nothing to do". Changing the wording
                // here means changing it there, in the same commit.
                Debug.Log($"Windows build succeeded: {summary.outputPath} ({summary.totalSize / 1024 / 1024} MB)");
                WriteBuildStamp(OutputDirectory);
            }
            else
            {
                Debug.LogError($"Windows build failed: {summary.result} ({summary.totalErrors} errors)");
            }
        }

        // ------------------------------------------------------------------ web

        /// <summary>
        /// Builds the browser-playable version. Output: <c>Build/Web</c>, to be pushed to itch.io as
        /// it is.
        /// </summary>
        [MenuItem("Snake Snack/Build the web version")]
        public static void BuildWeb()
        {
            ConfigurePlayerSettings();
            StampGitSha();
            ApplyWebSettings();
            Directory.CreateDirectory(WebOutputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.ScenePath },
                // ⚠ On WebGL, Unity expects a FOLDER and not a file: it writes index.html and Build/ there.
                locationPathName = WebOutputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Web build failed: {summary.result} ({summary.totalErrors} errors)");
                return;
            }

            // ⚠ CONTRACT WITH tools/build.ps1, same as above.
            Debug.Log($"Web build succeeded: {summary.outputPath} ({summary.totalSize / 1024 / 1024} MB)");
            WriteBuildStamp(WebOutputDirectory);
            StampWebCacheBuster(WebOutputDirectory);
        }

        /// <summary>
        /// Web player settings. Each one fixes a defect that does not show at compile time: they
        /// produce a game that starts, then misbehaves.
        /// </summary>
        static void ApplyWebSettings()
        {
            NamedBuildTarget web = NamedBuildTarget.WebGL;

            // Brotli compresses WebAssembly markedly better than gzip, but the browser can only
            // decompress it if the server announces the encoding. The JS fallback makes the build
            // independent of that configuration: it runs on itch.io as on any static host.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Without this cache, the audio and textures of the .data are re-downloaded on every visit.
            PlayerSettings.WebGL.dataCaching = true;

            // To raise if the game allocates a lot: too low, the heap grows in steps during play
            // (micro-freezes); too high, loading costs more.
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 512;

            // ⚠ WebGL is the only platform whose default stripping level is the most aggressive one.
            // The Input System resolves its control layouts by reflection: at the high level, the game
            // starts normally and no longer responds to the keyboard.
            PlayerSettings.SetManagedStrippingLevel(web, ManagedStrippingLevel.Low);

            // Explicitly thrown exceptions keep their stack in the browser console: the only way to
            // investigate a defect that cannot be reproduced outside the browser.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            // Unity's default canvas is 960 x 600 (16:10): a game composed for 16:9 ends up with bars
            // on it. These two values also feed the framing of the host page.
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;

            // The project's template: Assets/WebGLTemplates/SnakeSnack/. It carries the framing, the
            // capture of keys the browser hijacks, the audio-context wake-up, the touch guards and the
            // cache guard.
            PlayerSettings.WebGL.template = "PROJECT:SnakeSnack";
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;

            Debug.Log($"Web settings: heap {PlayerSettings.WebGL.initialMemorySize} MB, " +
                      $"{PlayerSettings.WebGL.compressionFormat} (fallback {PlayerSettings.WebGL.decompressionFallback}), " +
                      $"stripping Low, template {PlayerSettings.WebGL.template}.");
        }

        [MenuItem("Snake Snack/Apply the project settings")]
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

        // ------------------------------------------------------------------ build stamp

        /// <summary>
        /// Records the git identity of the code about to be built, in the resource the game reads to
        /// show its stamp. Called <b>before</b> the build, without which the binary would embed the
        /// previous value.
        /// </summary>
        /// <remarks>
        /// Written here and not by the release script: set only at publishing time, it would then stay
        /// in place, and every later local build would show the SHA of the last release — a freshness
        /// guard that lies is worse than no guard, since it is trusted.
        /// </remarks>
        static void StampGitSha()
        {
            string sha = Git("rev-parse --short HEAD");

            if (sha.Length == 0)
            {
                // No repository, or no git in PATH: "dev" admits ignorance, where a stale SHA would
                // claim knowledge.
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

            // The file is ignored by git: on a fresh clone it does not exist yet, so the asset
            // database does not know it — an ImportAsset alone would not bring it in.
            if (isNew) AssetDatabase.Refresh();

            // Without a reimport, the build would embed the value the asset database has in memory.
            AssetDatabase.ImportAsset(ShaAssetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"Git identity: {sha}");
        }

        /// <summary>
        /// Does the repository carry changes other than the ones the build itself writes?
        /// </summary>
        /// <remarks>
        /// Three files are excluded from the check because they are <b>artefacts</b> and not sources:
        /// the stamp and the version number, written just before building, and the scene, which
        /// <see cref="SceneBuilder"/> regenerates from scratch (hence with new object identifiers,
        /// hence a guaranteed diff). Without those exclusions, every build would declare itself built
        /// from a modified tree, including on a perfectly clean repository — and the warning meant to
        /// report a real discrepancy would stop meaning anything.
        /// </remarks>
        static bool HasLocalChanges()
        {
            foreach (string line in Git("status --porcelain").Split('\n'))
            {
                string entry = line.Trim();
                if (entry.Length == 0) continue;

                // "XY path": the status fits in the first two columns.
                string path = entry.Length > 2 ? entry.Substring(2).Trim().Replace('\\', '/') : "";

                if (path.EndsWith("Assets/Resources/build_sha.txt", StringComparison.Ordinal)) continue;
                if (path.EndsWith("ProjectSettings/ProjectSettings.asset", StringComparison.Ordinal)) continue;
                if (path.EndsWith(SceneBuilder.ScenePath, StringComparison.Ordinal)) continue;

                return true;
            }

            return false;
        }

        /// <summary>Writes, next to the build, the identity card of what has just been built.</summary>
        /// <remarks>
        /// It is the only honest freshness check: the metadata of a Unity binary describes the
        /// <i>engine</i> and not the game, and the timestamp is no better, the build being incremental
        /// — an identical file is not rewritten. This stamp is produced by the build: it cannot
        /// announce a version the build did not put there. The release script reads it before pushing.
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
            Debug.Log($"Build stamp: v{PlayerSettings.bundleVersion}-{sha}");
        }

        /// <summary>Replaces <c>__BUILD_ID__</c> in the page with a fingerprint unique to this build.</summary>
        /// <remarks>
        /// Without it, a browser that has already seen the page serves the loader of one build and the
        /// wasm of another: the game no longer starts, and the only clue is an error message that does
        /// not change while the build does. The timestamp is added to the SHA because two local builds
        /// in a row share the same commit and must still be told apart; it also invalidates Unity's
        /// IndexedDB cache, which is indexed by URL.
        /// </remarks>
        static void StampWebCacheBuster(string directory)
        {
            string indexPath = Path.Combine(directory, "index.html");
            if (!File.Exists(indexPath))
            {
                Debug.LogWarning("index.html not found: no cache guard placed.");
                return;
            }

            string buildId = ReadSha() + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string html = File.ReadAllText(indexPath);

            if (!html.Contains("__BUILD_ID__"))
            {
                // The template was modified without the token surviving: say it loudly, otherwise the
                // defect will only show up on a player's machine, as a game that does not start.
                Debug.LogWarning("__BUILD_ID__ missing from the template: the browser may mix two builds.");
                return;
            }

            File.WriteAllText(indexPath, html.Replace("__BUILD_ID__", buildId));
            Debug.Log($"Cache guard: {buildId}");
        }

        static string ReadSha()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ShaAssetPath);
            return asset != null && asset.text.Trim().Length > 0 ? asset.text.Trim() : "dev";
        }

        /// <summary>Runs a git command at the project root. Empty string if git is unavailable.</summary>
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
                Debug.LogWarning($"git unavailable: {error.Message}");
                return string.Empty;
            }
        }
    }
}
