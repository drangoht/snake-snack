using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SnakeSnack.EditorTools
{
    /// <summary>
    /// Active le Universal Render Pipeline sur <b>tous</b> les niveaux de qualité.
    ///
    /// <para>Le Built-in Render Pipeline est déprécié depuis Unity 6 ; un jeu 2D passe par le
    /// Renderer 2D d'URP, qui rend les sprites et ouvre l'accès à l'éclairage 2D.</para>
    /// </summary>
    /// <remarks>
    /// ⚠ Unity range le pipeline actif dans <c>QualitySettings</c> <b>niveau par niveau</b> : ne
    /// renseigner que <c>GraphicsSettings.defaultRenderPipeline</c> laisse les autres niveaux en
    /// Built-in, et le jeu bascule de pipeline dès que le joueur change de qualité — sans erreur.
    ///
    /// ⚠ Sous le Renderer 2D, les sprites prennent <c>Sprite-Lit-Default</c> : sans une
    /// <c>Light2D</c> globale dans la scène, tout le décor est rendu <b>noir</b>.
    /// <see cref="SceneBuilder"/> en pose une.
    /// </remarks>
    public static class RenderPipelineSetup
    {
        public const string PipelineAssetPath = "Assets/Settings/UniversalRP.asset";
        public const string GlobalSettingsPath = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset";

        [MenuItem("Snake Snack/Activer le pipeline URP")]
        public static void Apply()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                Debug.LogError("Pipeline URP introuvable : " + PipelineAssetPath);
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

            // Les réglages globaux portent notamment le volume profile par défaut. Sans cette
            // affectation explicite, l'éditeur en fabriquerait un au premier lancement.
            // UniversalRenderPipelineGlobalSettings est internal : on passe par la classe de base.
            var globalSettings = AssetDatabase.LoadAssetAtPath<RenderPipelineGlobalSettings>(GlobalSettingsPath);
            if (globalSettings != null)
            {
                EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(globalSettings);
            }
            else
            {
                Debug.LogWarning("Reglages globaux URP introuvables : " + GlobalSettingsPath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"URP actif sur {levelCount} niveau(x) de qualite : {PipelineAssetPath}");
        }
    }
}
