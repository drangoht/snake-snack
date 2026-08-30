using UnityEngine;

namespace SnakeSnack
{
    /// <summary>
    /// The binary's identity card: the published version number and the commit it came from.
    ///
    /// <para>The stamp shown at the bottom of the screen is not there for the player but for the
    /// <b>bug report</b>: without it, a screenshot does not say which version it shows, and a test
    /// session can run on a stale build without anyone noticing — which counts double for a web
    /// page, where the browser happily serves an old file from its cache.</para>
    ///
    /// <para>The version comes from the project settings; the SHA lives in a resource written by the
    /// build itself rather than compiled in: it must name the commit this binary came from, known at
    /// the last moment.</para>
    /// </summary>
    public static class BuildInfo
    {
        /// <summary>Resource written by the build: one line, the short SHA.</summary>
        public const string ResourcePath = "build_sha";

        static string sha;

        /// <summary>Project version, as it will be published.</summary>
        public static string Version => Application.version;

        /// <summary>
        /// Short SHA of the commit this binary came from — suffixed with <c>+</c> if the working
        /// tree carried changes, <c>dev</c> if git could say nothing.
        /// </summary>
        /// <remarks>
        /// The three cases do not say the same thing: a bare SHA names a commit that can be checked
        /// out again; a suffixed SHA warns that the binary matches <b>no</b> commit; "dev" admits
        /// ignorance, where a stale SHA would claim knowledge.
        /// </remarks>
        public static string GitSha
        {
            get
            {
                if (sha != null) return sha;

                var asset = Resources.Load<TextAsset>(ResourcePath);
                sha = asset != null && asset.text.Trim().Length > 0 ? asset.text.Trim() : "dev";
                return sha;
            }
        }

        /// <summary>What is shown on screen: <c>v1.2.0-a1b2c3d</c>.</summary>
        public static string Label => $"v{Version}-{GitSha}";
    }
}
