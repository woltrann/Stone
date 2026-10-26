using TidyUpFramework.Cleaning;
using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Feedback;
using TidyUpFramework.Hints;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using TidyUpFramework.Levels;
using TidyUpFramework.Player;
using TidyUpFramework.Tutorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyCorePrefabBuilder
    {
        const string InputActionsPath = TidyDemoPaths.Settings + "/TidyUpInputActions.inputactions";
        const string OutlineShaderPath = TidyDemoPaths.Root + "/Shaders/TidyOutline.shader";
        const string GhostShaderPath = TidyDemoPaths.Root + "/Shaders/TidyGhost.shader";
        const float EyeHeight = 1.63f;

        public static void BuildAll(TidyDemoContext context)
        {
            var arrow = BuildArrowAssets(out var hintMaterial, out var tutorialMaterial);
            BuildPlayer();
            BuildSystems(context, arrow, hintMaterial, tutorialMaterial);
            TidyAssetFactory.SavePrefab(TidyHudBuilder.Build(), TidyDemoPaths.CorePrefabs + "/TidyHUD.prefab");
            TidyAssetFactory.SavePrefab(TidyMenuBuilder.Build(), TidyDemoPaths.CorePrefabs + "/TidyMainMenu.prefab");
            AssetDatabase.SaveAssets();
        }

        static Mesh BuildArrowAssets(out Material hintMaterial, out Material tutorialMaterial)
        {
            var builder = new TidyMeshBuilder();
            builder.AddLathe(new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0.09f, 0.11f),
                new Vector2(0.04f, 0.11f),
                new Vector2(0.04f, 0.24f),
                new Vector2(0f, 0.24f)
            }, 20);
            var mesh = TidyMeshAssets.SaveMesh(builder.Build("Tidy_HintArrow"), TidyDemoPaths.CoreMeshes + "/Tidy_HintArrow.asset");
            hintMaterial = TidyMaterialFactory.Lit(TidyDemoPaths.CoreMaterials + "/Tidy_HintArrow.mat", new TidyMaterialSpec
            {
                BaseColor = new Color(0.45f, 0.85f, 1f),
                Emission = new Color(0.6f, 1.4f, 2.2f),
                Smoothness = 0.6f
            });
            tutorialMaterial = TidyMaterialFactory.Lit(TidyDemoPaths.CoreMaterials + "/Tidy_TutorialArrow.mat", new TidyMaterialSpec
            {
                BaseColor = new Color(1f, 0.8f, 0.4f),
                Emission = new Color(2.2f, 1.5f, 0.5f),
                Smoothness = 0.6f
            });
            return mesh;
        }

        static void BuildPlayer()
        {
            var root = new GameObject("TidyPlayer");
            var controllerBody = root.AddComponent<CharacterController>();
            controllerBody.height = 1.75f;
            controllerBody.radius = 0.3f;
            controllerBody.center = new Vector3(0f, 0.875f, 0f);
            controllerBody.stepOffset = 0.3f;
            controllerBody.skinWidth = 0.03f;
            var reader = root.AddComponent<TidyInputReader>();
            reader.AssignActions(AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            TidySerialization.Set(reader, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            root.AddComponent<TidyCursorController>();

            var head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            head.localPosition = new Vector3(0f, EyeHeight, 0f);
            var controller = root.AddComponent<TidyFirstPersonController>();
            TidySerialization.Set(controller, "head", head);

            var cameraObject = new GameObject("Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(head, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 120f;
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraObject.AddComponent<AudioListener>();
            var interactor = cameraObject.AddComponent<TidyInteractor>();
            TidySerialization.Set(interactor, "aimCamera", camera);
            TidySerialization.SetInt(interactor, "carryCapacity", 5);
            var preview = cameraObject.AddComponent<TidyPlacementPreview>();
            TidySerialization.Set(preview, "ghostShader", AssetDatabase.LoadAssetAtPath<Shader>(GhostShaderPath));
            cameraObject.AddComponent<TidyCleaningInteractor>();
            cameraObject.AddComponent<TidyCameraPunch>();
            TidyAssetFactory.SavePrefab(root, TidyDemoPaths.CorePrefabs + "/TidyPlayer.prefab");
        }

        static void BuildSystems(TidyDemoContext context, Mesh arrow, Material hintMaterial, Material tutorialMaterial)
        {
            var root = new GameObject("TidyLevelSystems");
            root.AddComponent<TidyLevelManager>();
            var highlighter = root.AddComponent<TidyOutlineHighlighter>();
            TidySerialization.Set(highlighter, "outlineShader", AssetDatabase.LoadAssetAtPath<Shader>(OutlineShaderPath));

            var hints = root.AddComponent<TidyHintSystem>();
            var objectIndicator = Indicator("Hint Object Indicator", root.transform, arrow, hintMaterial);
            var destinationIndicator = Indicator("Hint Destination Indicator", root.transform, arrow, hintMaterial);
            TidySerialization.Set(hints, "objectIndicator", objectIndicator);
            TidySerialization.Set(hints, "destinationIndicator", destinationIndicator);

            var feedback = root.AddComponent<TidyFeedbackPlayer>();
            feedback.AssignDefinition(context.Feedback);
            TidySerialization.Set(feedback, "definition", context.Feedback);

            var tutorial = root.AddComponent<TidyTutorialRunner>();
            var tutorialIndicator = Indicator("Tutorial Indicator", root.transform, arrow, tutorialMaterial);
            TidySerialization.Set(tutorial, "targetIndicator", tutorialIndicator);
            TidyAssetFactory.SavePrefab(root, TidyDemoPaths.CorePrefabs + "/TidyLevelSystems.prefab");
        }

        static TidyHintIndicator Indicator(string indicatorName, Transform parent, Mesh arrow, Material material)
        {
            var holder = new GameObject(indicatorName);
            holder.transform.SetParent(parent, false);
            var visual = TidyPropFactory.Visual("Arrow", arrow, material);
            visual.transform.SetParent(holder.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var light = new GameObject("Glow").AddComponent<Light>();
            light.transform.SetParent(holder.transform, false);
            light.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            light.type = LightType.Point;
            light.range = 0.8f;
            light.intensity = 1.2f;
            light.color = material.GetColor("_BaseColor");
            light.shadows = LightShadows.None;
            var indicator = holder.AddComponent<TidyHintIndicator>();
            holder.SetActive(false);
            return indicator;
        }
    }
}
