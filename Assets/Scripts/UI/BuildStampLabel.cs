using UnityEngine;
using UnityEngine.UI;

namespace SnakeSnack
{
    /// <summary>
    /// Ecrit le tampon de build dans son texte, au lancement.
    ///
    /// <para>Pose par <c>SceneBuilder.BuildStampCanvas</c> sur son propre canevas : le HUD s'eteint
    /// des qu'un menu s'ouvre, et c'est justement la que les captures d'ecran sont prises.</para>
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class BuildStampLabel : MonoBehaviour
    {
        void Awake() => GetComponent<Text>().text = BuildInfo.Label;
    }
}
