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
    /// Construit la scène de jeu <b>entièrement par code</b>, puis l'écrit sur disque.
    ///
    /// <para>C'est le choix structurant du gabarit : la scène est un <b>artefact</b>, régénéré à
    /// chaque build, et non un fichier qu'on édite à la souris. En échange, tout le jeu se pilote
    /// sans jamais ouvrir l'éditeur — un agent peut modifier une position, relancer le build en
    /// batchmode et regarder le résultat, ce qu'un fichier <c>.unity</c> édité manuellement rend
    /// impossible.</para>
    /// </summary>
    /// <remarks>
    /// ⚠ Conséquence à connaître : <c>Assets/Scenes/Game.unity</c> ressort <b>modifié après chaque
    /// build</b>, parce que la régénération renumérote tous les <c>fileID</c> — des milliers de
    /// lignes de diff pour une scène identique. L'écarter (<c>git checkout --</c>) sauf si
    /// <c>SceneBuilder.cs</c> a changé, auquel cas la régénération porte une vraie différence.
    /// <c>BuildTools.HasLocalChanges</c> l'exclut déjà du constat de propreté de l'arbre.
    ///
    /// ⚠ Ne rien ajouter ici qui dépende d'un asset absent : un build en batchmode échoue sur une
    /// référence nulle sans qu'on puisse la voir dans l'éditeur.
    /// </remarks>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Snake Snack/Regenerer la scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildGlobalLight();
            BuildEventSystem();
            BuildStampCanvas();

            // ---- Le jeu commence ici -------------------------------------------------------
            BuildJeu();
            // ---------------------------------------------------------------------------------

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Scene regeneree : {ScenePath}");
        }

        /// <summary>
        /// Pose l'unique objet de jeu. Tout le reste — aire, serpent, HUD — est construit au
        /// démarrage par <see cref="JeuSnake"/> lui-même.
        /// </summary>
        /// <remarks>
        /// ⚠ Rien n'est sérialisé dans la scène au-delà de ce composant : une référence sérialisée
        /// perdue à la régénération ne lèverait rien, elle produirait seulement un écran incomplet.
        /// </remarks>
        static void BuildJeu()
        {
            var go = new GameObject("Jeu");
            go.AddComponent<JeuSnake>();
        }

        /// <remarks>
        /// ⚠ <c>orthographicSize = 360</c> — la moitié de la hauteur du cadre de référence 720 px :
        /// une unité monde vaut alors <b>exactement un pixel</b> de ce cadre, ce que
        /// <see cref="SnakeSnack.Rules.Plateau"/> suppose partout (tailles de case, ancrage du
        /// pictogramme). Toute autre valeur afficherait un jeu « pas tout à fait à la bonne
        /// échelle », sans qu'aucun calcul ne soit faux.
        /// </remarks>
        static void BuildCamera()
        {
            var go = new GameObject("Main Camera");
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 360f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiPalette.Fond;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            go.tag = "MainCamera";

            // Les données URP de la caméra sont un composant à part : sans lui, la caméra tombe sur
            // des valeurs par défaut et ignore le renderer 2D.
            go.AddComponent<UniversalAdditionalCameraData>();
        }

        /// <summary>
        /// ⚠ La lumière globale n'est pas décorative : sous le Renderer 2D, un sprite en
        /// <c>Sprite-Lit-Default</c> sans aucune <c>Light2D</c> est rendu <b>noir</b>. Le jeu
        /// s'affiche alors entièrement sombre, sans la moindre erreur en console.
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
        /// ⚠ <c>InputSystemUIInputModule</c> et non <c>StandaloneInputModule</c> : avec le package
        /// Input System actif, l'ancien module ne reçoit rien et l'UI cesse simplement de répondre.
        /// </summary>
        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>
        /// Le tampon de build vit sur son <b>propre</b> canevas plutôt que dans le HUD : le HUD
        /// s'éteint dès qu'un menu s'ouvre, et c'est justement sur les menus que la plupart des
        /// captures d'écran sont prises.
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
