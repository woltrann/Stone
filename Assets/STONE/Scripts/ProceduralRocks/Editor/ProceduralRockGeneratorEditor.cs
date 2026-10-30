using ProceduralRocks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralRockGenerator))]
public sealed class ProceduralRockGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var generator = (ProceduralRockGenerator)target;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();

        if (changed && generator.AutoPreviewInEditor && !Application.isPlaying)
        {
            foreach (Object item in targets)
            {
                var rock = (ProceduralRockGenerator)item;
                Generate(rock);
            }
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("ROCK GENERATOR", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(generator.SourceMesh == null))
        {
            if (GUILayout.Button("Generate / Regenerate Preview", GUILayout.Height(32)))
                Generate(generator);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Random Seed"))
                {
                    SerializedProperty property = serializedObject.FindProperty("seed");
                    property.intValue = Random.Range(1, int.MaxValue);
                    serializedObject.ApplyModifiedProperties();
                    Generate(generator);
                }

                if (GUILayout.Button("Restore Source"))
                {
                    Undo.RecordObject(generator, "Restore source rock");
                    Undo.RecordObject(generator.GetComponent<MeshFilter>(), "Restore source rock");
                    generator.RestoreSource();
                    EditorUtility.SetDirty(generator);
                    EditorUtility.SetDirty(generator.GetComponent<MeshFilter>());
                }
            }

            if (GUILayout.Button("Save Mesh Asset (.asset)", GUILayout.Height(28)))
                SaveMesh(generator);
        }

        if (!string.IsNullOrEmpty(generator.LastMessage))
        {
            MessageType type = generator.LastMessage.Contains("failed") ||
                               generator.LastMessage.Contains("not readable") ||
                               generator.LastMessage.Contains("Failed cuts")
                ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox(generator.LastMessage, type);
        }
        EditorGUILayout.HelpBox(
            "Requires a readable, CLOSED, triangulated source mesh. " +
            "Cut caps support simple loops, including concave contours, " +
            "but not nested loops/holes or non-manifold topology. " +
            "Preview is temporary; Save Mesh Asset to keep the result.",
            MessageType.None);
    }

    private static void Generate(ProceduralRockGenerator rock)
    {
        var filter = rock.GetComponent<MeshFilter>();
        var renderer = rock.GetComponent<MeshRenderer>();
        Undo.RecordObject(filter, "Generate rock preview");
        Undo.RecordObject(renderer, "Generate rock preview");
        if (rock.GeneratePreview())
        {
            EditorUtility.SetDirty(rock);
            EditorUtility.SetDirty(filter);
            EditorUtility.SetDirty(renderer);
            SceneView.RepaintAll();
        }
    }

    private static void SaveMesh(ProceduralRockGenerator rock)
    {
        var filter = rock.GetComponent<MeshFilter>();
        if (filter.sharedMesh == null || filter.sharedMesh == rock.SourceMesh)
        {
            if (!rock.GeneratePreview())
            {
                Debug.LogWarning(rock.LastMessage, rock);
                return;
            }
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save generated rock", "Rock_" + rock.gameObject.name,
            "asset", "Choose an Assets folder for the generated rock mesh.");
        if (string.IsNullOrEmpty(path)) return;

        // Never silently overwrite a previously saved mesh asset.
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        Mesh copy = Object.Instantiate(filter.sharedMesh);
        copy.name = System.IO.Path.GetFileNameWithoutExtension(path);
        copy.hideFlags = HideFlags.None;
        AssetDatabase.CreateAsset(copy, path);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(rock, "Save generated rock mesh");
        Undo.RecordObject(filter, "Save generated rock mesh");
        rock.AssignSavedMesh(copy);
        EditorUtility.SetDirty(rock);
        EditorUtility.SetDirty(filter);
        EditorUtility.SetDirty(rock.GetComponent<MeshRenderer>());
        EditorGUIUtility.PingObject(copy);
        SceneView.RepaintAll();
    }
}
