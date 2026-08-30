using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SnakeSnack.EditorTools
{
    /// <summary>
    /// Enables the Universal Render Pipeline on <b>every</b> quality level.
    ///
    /// <para>The Built-in Render Pipeline has been deprecated since Unity 6; a 2D game goes through
    /// URP's 2D Renderer, which draws sprites and opens access to 2D lighting.</para>
    /// </summary>
    /// <remarks>
    /// ⚠ Unity stores the active pipeline in <c>QualitySettings</c> <b>level by level</b>: filling in
    /// only <c>GraphicsSettings.defaultRenderPipeline</c> leaves the other levels on Built-in, and the
    /// game switches pipeline as soon as the player changes quality — with no error.
    ///
    /// ⚠ Under the 2D Renderer, sprites take <c>Sprite-Lit-Default</c>: without a global
    /// <c>Light2D</c> in the scene, the whole set is rendered <b>black</b>. <see cref="SceneBuilder"/>
    /// places one.
    /// </remarks>
    public static class RenderPipelineSetup
    {
        public const string PipelineAssetPath = "Assets/Settings/UniversalRP.asset";
        public const string GlobalSettingsPath = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset";

        [MenuItem("Snake Snack/Enable the URP pipeline")]
        public static void Apply()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                Debug.LogError("URP pipeline not found: " + PipelineAssetPath);
                return;
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;

            int previousLevel = QualitySettings.GetQualityLevel();
            int levelCount = QualitySettings.names.Length;
            for (int level = 0; level < levelCount; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(previousLevel, false);

            // The global settings notably carry the default volume profile. Without this explicit
            // assignment, the editor would fabricate one on first launch.
            // UniversalRenderPipelineGlobalSettings is internal: we go through the base class.
            var globalSettings = AssetDatabase.LoadAssetAtPath<RenderPipelineGlobalSettings>(GlobalSettingsPath);
            if (globalSettings != null)
            {
                EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(globalSettings);
            }
            else
            {
                Debug.LogWarning("URP global settings not found: " + GlobalSettingsPath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"URP active on {levelCount} quality level(s): {PipelineAssetPath}");
        }
    }
}
