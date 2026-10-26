using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TidyUpFramework.Editor
{
    [InitializeOnLoad]
    public sealed class TidyWelcomeWindow : EditorWindow
    {
        const string ShownPreferenceKey = "TidyUpFramework.WelcomeShown.v1";
        const string DocumentationPath = TidyEditorUtility.RootFolder + "/Documentation/index.html";
        const string DemoSceneFolder = TidyEditorUtility.RootFolder + "/Demo/Scenes";

        static readonly string[] QuickStartSteps =
        {
            "Open a demo scene below and press Play to feel the interaction.",
            "In a new scene choose GameObject > Tidy Up Framework > Complete Level Setup.",
            "Create a Category and an Object Definition from Create > Tidy Up Framework.",
            "Select your props and run Tools > Tidy Up Framework > Make Selected Objects Interactive.",
            "Add a Shelf Row or Container from GameObject > Tidy Up Framework > Placement and set its accepted category.",
            "Create a Level Definition with a 'Place Objects' objective and assign it to the Level Manager.",
            "Run the Level Validator, then press Play."
        };

        Vector2 scroll;
        GUIStyle titleStyle;
        GUIStyle bodyStyle;

        static TidyWelcomeWindow()
        {
            EditorApplication.delayCall += ShowOnFirstLoad;
        }

        [MenuItem(TidyEditorUtility.MenuRoot + "Welcome", false, 0)]
        public static void Open()
        {
            var window = GetWindow<TidyWelcomeWindow>(true, "Tidy Up Framework", true);
            window.minSize = new Vector2(520f, 560f);
        }

        static void ShowOnFirstLoad()
        {
            if (EditorPrefs.GetBool(ShownPreferenceKey, false) || Application.isBatchMode)
            {
                return;
            }

            EditorPrefs.SetBool(ShownPreferenceKey, true);
            Open();
        }

        void OnGUI()
        {
            titleStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, wordWrap = true };
            bodyStyle ??= new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Space(8f);
            GUILayout.Label("Tidy Up + Sorting + Organizing + Placement + Satisfying Interaction Framework", titleStyle);
            GUILayout.Label("Build tidy-up, sorting and organising games by configuring data instead of writing gameplay code.", bodyStyle);
            GUILayout.Space(10f);
            DrawDemoScenes();
            GUILayout.Space(10f);
            DrawQuickStart();
            GUILayout.Space(10f);
            DrawLinks();
            EditorGUILayout.EndScrollView();
        }

        static void DrawDemoScenes()
        {
            EditorGUILayout.LabelField("Demo Scenes", EditorStyles.boldLabel);
            if (!AssetDatabase.IsValidFolder(DemoSceneFolder))
            {
                EditorGUILayout.HelpBox("Demo scenes were not found in " + DemoSceneFolder + ".", MessageType.Info);
                return;
            }

            var scenes = AssetDatabase.FindAssets("t:Scene", new[] { DemoSceneFolder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToList();
            if (scenes.Count == 0)
            {
                EditorGUILayout.HelpBox("Demo scenes were not found in " + DemoSceneFolder + ".", MessageType.Info);
                return;
            }

            foreach (var scenePath in scenes)
            {
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(scenePath).Replace("TidyUp_", string.Empty).Replace('_', ' '), GUILayout.Height(26f)) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
        }

        void DrawQuickStart()
        {
            EditorGUILayout.LabelField("Quick Start", EditorStyles.boldLabel);
            for (var index = 0; index < QuickStartSteps.Length; index++)
            {
                GUILayout.Label("<b>" + (index + 1) + ".</b>  " + QuickStartSteps[index], bodyStyle);
            }
        }

        static void DrawLinks()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Documentation", GUILayout.Height(30f)))
                {
                    var fullPath = Path.GetFullPath(DocumentationPath);
                    if (File.Exists(fullPath))
                    {
                        Application.OpenURL("file:///" + fullPath.Replace('\\', '/'));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Tidy Up Framework", "Documentation was not found at " + DocumentationPath, "OK");
                    }
                }

                if (GUILayout.Button("Level Validator", GUILayout.Height(30f)))
                {
                    TidyLevelValidatorWindow.Open();
                }
            }
        }
    }
}
