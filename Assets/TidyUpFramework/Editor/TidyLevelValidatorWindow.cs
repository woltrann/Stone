using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Editor
{
    public sealed class TidyLevelValidatorWindow : EditorWindow
    {
        const float RowPadding = 4f;
        const float FixButtonWidth = 110f;

        List<TidyValidationIssue> issues = new List<TidyValidationIssue>();
        Vector2 scroll;
        bool includeProjectChecks = true;
        GUIStyle wrapStyle;

        [MenuItem(TidyEditorUtility.MenuRoot + "Level Validator", false, 1)]
        public static void Open()
        {
            var window = GetWindow<TidyLevelValidatorWindow>();
            window.titleContent = new GUIContent("Tidy Level Validator", EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image);
            window.minSize = new Vector2(420f, 300f);
            window.Run();
        }

        void Run()
        {
            issues = TidyLevelValidation.ValidateActiveScene();
            if (includeProjectChecks)
            {
                issues.AddRange(TidyLevelValidation.ValidateProject());
            }

            issues = issues.OrderByDescending(issue => issue.Severity).ToList();
            Repaint();
        }

        void OnGUI()
        {
            wrapStyle ??= new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };
            DrawToolbar();
            DrawSummary();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var issue in issues)
            {
                DrawIssue(issue);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    Run();
                }

                includeProjectChecks = GUILayout.Toggle(includeProjectChecks, "Include project checks", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Generate Scene IDs", EditorStyles.toolbarButton))
                {
                    TidyIdGenerator.GenerateSceneIds();
                    Run();
                }
            }
        }

        void DrawSummary()
        {
            var errors = issues.Count(issue => issue.Severity == TidyIssueSeverity.Error);
            var warnings = issues.Count(issue => issue.Severity == TidyIssueSeverity.Warning);
            var message = errors == 0 && warnings == 0 ? "All checks passed." : errors + " error(s), " + warnings + " warning(s).";
            EditorGUILayout.HelpBox(message, errors > 0 ? MessageType.Error : warnings > 0 ? MessageType.Warning : MessageType.Info);
        }

        void DrawIssue(TidyValidationIssue issue)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(IconFor(issue.Severity), GUILayout.Width(20f), GUILayout.Height(20f));
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(RowPadding * 0.5f);
                    GUILayout.Label(issue.Message, wrapStyle);
                }

                if (issue.Context != null && GUILayout.Button("Select", GUILayout.Width(60f)))
                {
                    Selection.activeObject = issue.Context;
                    EditorGUIUtility.PingObject(issue.Context);
                }

                if (issue.CanFix && GUILayout.Button(issue.FixLabel ?? "Fix", GUILayout.Width(FixButtonWidth)))
                {
                    issue.Fix();
                    Run();
                    GUIUtility.ExitGUI();
                }
            }
        }

        static GUIContent IconFor(TidyIssueSeverity severity)
        {
            switch (severity)
            {
                case TidyIssueSeverity.Error:
                    return EditorGUIUtility.IconContent("console.erroricon.sml");
                case TidyIssueSeverity.Warning:
                    return EditorGUIUtility.IconContent("console.warnicon.sml");
                default:
                    return EditorGUIUtility.IconContent("console.infoicon.sml");
            }
        }
    }
}
