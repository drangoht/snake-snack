using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using SnakeSnack.Gameplay;
using SnakeSnack.UI;

namespace SnakeSnack.EditorTools
{
    /// <summary>
    /// Builds the game scene <b>entirely in code</b>, then writes it to disk.
    ///
    /// <para>This is the template's structural choice: the scene is an <b>artefact</b>, regenerated
    /// on every build, and not a file edited with a mouse. In exchange, the whole game can be driven
    /// without ever opening the editor — an agent can change a position, relaunch the build in
    /// batchmode and look at the result, which a hand-edited <c>.unity</c> file makes
    /// impossible.</para>
    /// </summary>
    /// <remarks>
    /// ⚠ A consequence worth knowing: <c>Assets/Scenes/Game.unity</c> comes out <b>modified after
    /// every build</b>, because regeneration renumbers every <c>fileID</c> — thousands of diff lines
    /// for an identical scene. Discard it (<c>git checkout --</c>) unless <c>SceneBuilder.cs</c> has
    /// changed, in which case the regeneration carries a real difference.
    /// <c>BuildTools.HasLocalChanges</c> already excludes it from the working-tree cleanliness check.
    ///
    /// ⚠ Add nothing here that depends on a missing asset: a batchmode build fails on a null
    /// reference without any way to see it in the editor.
    /// </remarks>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Snake Snack/Regenerate the scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildGlobalLight();
            BuildEventSystem();
            BuildStampCanvas();

            // ---- The game starts here ------------------------------------------------------
            BuildGame();
            // ---------------------------------------------------------------------------------

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Scene regenerated: {ScenePath}");
        }

        /// <summary>
        /// Places the single game object. Everything else — playfield, snake, HUD — is built at
        /// startup by <see cref="SnakeGame"/> itself.
        /// </summary>
        /// <remarks>
        /// ⚠ Nothing is serialised in the scene beyond this component: a serialised reference lost on
        /// regeneration would raise nothing, it would only produce an incomplete screen.
        /// </remarks>
        static void BuildGame()
        {
            var go = new GameObject("Game");
            go.AddComponent<SnakeGame>();
        }

        /// <remarks>
        /// ⚠ <c>orthographicSize = 360</c> — half the height of the 720 px reference frame: one world
        /// unit is then <b>exactly one pixel</b> of that frame, which
        /// <see cref="SnakeSnack.Rules.Board"/> assumes everywhere (cell sizes, pictogram anchoring).
        /// Any other value would show a game that is "not quite the right scale", without a single
        /// calculation being wrong.
        /// </remarks>
        static void BuildCamera()
        {
            var go = new GameObject("Main Camera");
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 360f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiPalette.Background;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            go.tag = "MainCamera";

            // The camera's URP data is a separate component: without it, the camera falls back on
            // defaults and ignores the 2D renderer.
            go.AddComponent<UniversalAdditionalCameraData>();

            // ⚠ Without this, NOTHING is ever heard — and nothing says so. Every clip loads, every
            // PlayOneShot succeeds, and the mixer has no ear to deliver to; Unity logs one warning,
            // among the startup lines nobody re-reads. It was missing until 2026-08-31, which cost
            // nothing only because there was no sound yet. On the camera because that is where a
            // listener is expected to be found.
            go.AddComponent<AudioListener>();
        }

        /// <summary>
        /// ⚠ The global light is not decorative: under the 2D Renderer, a sprite in
        /// <c>Sprite-Lit-Default</c> with no <c>Light2D</c> at all is rendered <b>black</b>. The game
        /// then displays entirely dark, without the slightest console error.
        /// </summary>
        static void BuildGlobalLight()
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
        }

        /// <summary>
        /// ⚠ <c>InputSystemUIInputModule</c> and not <c>StandaloneInputModule</c>: with the Input
        /// System package active, the old module receives nothing and the UI simply stops responding.
        /// </summary>
        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>
        /// The build stamp lives on its <b>own</b> canvas rather than in the HUD: the HUD goes dark as
        /// soon as a menu opens, and menus are exactly where most screenshots are taken.
        /// </summary>
        static void BuildStampCanvas()
        {
            var canvasGo = new GameObject("Build Stamp Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var labelGo = new GameObject("Stamp");
            labelGo.transform.SetParent(canvasGo.transform, false);

            var text = labelGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.LowerRight;
            text.color = new Color(1f, 1f, 1f, 0.45f);
            text.raycastTarget = false;

            var rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-12f, 8f);
            rect.sizeDelta = new Vector2(300f, 20f);

            labelGo.AddComponent<BuildStampLabel>();
        }
    }
}
