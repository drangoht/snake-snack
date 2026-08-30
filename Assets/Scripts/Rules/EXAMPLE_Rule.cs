namespace SnakeSnack.Rules
{
    /// <summary>
    /// TEMPLATE — copy it, then delete this file.
    ///
    /// <para>Every numeric rule of the game (curve, threshold, table, formula) lives in this folder,
    /// as a <b>static class with no <c>using UnityEngine</c> at all</b>. That is what makes it
    /// testable in a few milliseconds by <c>dotnet test</c>, with no engine and no build: the
    /// <c>MonoBehaviour</c>s delegate here and keep only the engine work.</para>
    ///
    /// <para>A class in <c>Rules/</c> that would need the engine is a sign of a bad split: doing the
    /// engine part is the caller's job.</para>
    /// </summary>
    public static class ExampleRule
    {
        /// <summary>Points needed to reach the given level (level 1 = 0 points).</summary>
        /// <remarks>
        /// A gentle quadratic curve: the step grows without ever doubling from one level to the
        /// next, which avoids the progression wall in the middle of a run.
        /// </remarks>
        public static int LevelThreshold(int level)
        {
            if (level <= 1) return 0;
            int n = level - 1;
            return 5 * n * n + 10 * n;
        }
    }
}
