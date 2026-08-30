using UnityEngine;
using UnityEngine.UI;

namespace SnakeSnack
{
    /// <summary>
    /// Writes the build stamp into its text, at startup.
    ///
    /// <para>Placed by <c>SceneBuilder.BuildStampCanvas</c> on its own canvas: the HUD goes dark as
    /// soon as a menu opens, and that is exactly where screenshots are taken.</para>
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class BuildStampLabel : MonoBehaviour
    {
        void Awake() => GetComponent<Text>().text = BuildInfo.Label;
    }
}
