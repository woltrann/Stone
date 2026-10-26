using TidyUpFramework.Cleaning;
using TidyUpFramework.Levels;
using TidyUpFramework.Placement;
using TidyUpFramework.Transformation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TidyUpFramework.Editor
{
    public static class TidyCreateMenu
    {
        const string Root = TidyEditorUtility.GameObjectMenuRoot;
        const int Priority = TidyEditorUtility.GameObjectMenuPriority;
        const string PlayerPrefab = "TidyPlayer";
        const string SystemsPrefab = "TidyLevelSystems";
        const string HudPrefab = "TidyHUD";

        [MenuItem(Root + "Complete Level Setup", false, Priority)]
        static void CreateCompleteSetup(MenuCommand command)
        {
            var systems = TidyEditorUtility.InstantiatePrefab(SystemsPrefab, null) ?? CreateMinimalSystems();
            var player = TidyEditorUtility.InstantiatePrefab(PlayerPrefab, null);
            TidyEditorUtility.InstantiatePrefab(HudPrefab, null);
            EnsureEventSystem();
            if (player == null)
            {
                Debug.LogWarning("Tidy Up Framework: the TidyPlayer prefab was not found. Run 'Tools/Tidy Up Framework/Demo Content/3. Core Prefabs (Player, Systems, HUD, Menu)' or re-import the TidyUpFramework/Prefabs folder.");
            }
            else if (SceneView.lastActiveSceneView != null)
            {
                player.transform.position = new Vector3(SceneView.lastActiveSceneView.pivot.x, 0f, SceneView.lastActiveSceneView.pivot.z);
            }

            Selection.activeGameObject = systems;
        }

        [MenuItem(Root + "Player", false, Priority + 1)]
        static void CreatePlayer(MenuCommand command)
        {
            if (TidyEditorUtility.InstantiatePrefab(PlayerPrefab, command) == null)
            {
                Debug.LogWarning("Tidy Up Framework: TidyPlayer prefab not found.");
            }
        }

        [MenuItem(Root + "Level Systems", false, Priority + 2)]
        static void CreateSystems(MenuCommand command)
        {
            if (TidyEditorUtility.InstantiatePrefab(SystemsPrefab, command) == null)
            {
                CreateMinimalSystems();
            }
        }

        [MenuItem(Root + "HUD", false, Priority + 3)]
        static void CreateHud(MenuCommand command)
        {
            if (TidyEditorUtility.InstantiatePrefab(HudPrefab, command) != null)
            {
                EnsureEventSystem();
            }
        }

        [MenuItem(Root + "Placement/Snap Slot", false, Priority + 20)]
        static void CreateSnapSlot(MenuCommand command)
        {
            var created = TidyEditorUtility.CreateObject("Snap Slot", command);
            created.AddComponent<TidySnapSlot>();
            TidyEditorUtility.EnsureAimTrigger(created, new Vector3(0f, 0.1f, 0f), new Vector3(0.25f, 0.2f, 0.25f));
        }

        [MenuItem(Root + "Placement/Shelf Row (Packed)", false, Priority + 21)]
        static void CreateRow(MenuCommand command)
        {
            var created = TidyEditorUtility.CreateObject("Shelf Row", command);
            var row = created.AddComponent<TidyRowZone>();
            row.ConfigureRow(1f, 0.004f, 24, new Vector3(0f, 90f, 0f), TidyRowAlignment.Start);
            TidyEditorUtility.EnsureAimTrigger(created, new Vector3(0.5f, 0.15f, 0f), new Vector3(1f, 0.3f, 0.25f));
        }

        [MenuItem(Root + "Placement/Slot Grid", false, Priority + 22)]
        static void CreateSlotGroup(MenuCommand command)
        {
            var created = TidyEditorUtility.CreateObject("Slot Grid", command);
            var group = created.AddComponent<TidySlotGroup>();
            group.ConfigureGrid(5, 1, new Vector2(0.18f, 0.32f), TidyGridPlane.Vertical);
            TidyEditorUtility.EnsureAimTrigger(created, new Vector3(0f, 0.12f, 0f), new Vector3(0.95f, 0.25f, 0.25f));
        }

        [MenuItem(Root + "Placement/Container", false, Priority + 23)]
        static void CreateContainer(MenuCommand command)
        {
            var created = TidyEditorUtility.CreateObject("Container", command);
            created.AddComponent<TidyContainer>();
            TidyEditorUtility.EnsureAimTrigger(created, new Vector3(0f, 0.15f, 0f), new Vector3(0.45f, 0.3f, 0.35f));
        }

        [MenuItem(Root + "Placement/Disposal Bin", false, Priority + 24)]
        static void CreateDisposal(MenuCommand command)
        {
            var created = TidyEditorUtility.CreateObject("Disposal Bin", command);
            var container = created.AddComponent<TidyContainer>();
            container.ConfigureContainer(1, true, new Vector3(0.35f, 0.4f, 0.35f), Vector3.zero, TidyContainerLayout.Hidden, true);
            TidyEditorUtility.EnsureAimTrigger(created, new Vector3(0f, 0.2f, 0f), new Vector3(0.45f, 0.45f, 0.45f));
        }

        [MenuItem(Root + "Placement/Free Placement Zone", false, Priority + 25)]
        static void CreateFreeZone(MenuCommand command)
        {
            var created = TidyEditorUtility.CreateObject("Free Placement Zone", command);
            created.AddComponent<TidyFreePlacementZone>();
            TidyEditorUtility.EnsureAimTrigger(created, new Vector3(0f, 0.03f, 0f), new Vector3(1f, 0.06f, 0.6f));
        }

        [MenuItem(Root + "Placement/Stack Point", false, Priority + 26)]
        static void CreateStack(MenuCommand command)
        {
            var created = TidyEditorUtility.CreateObject("Stack Point", command);
            created.AddComponent<TidyStackPoint>();
            TidyEditorUtility.EnsureAimTrigger(created, new Vector3(0f, 0.2f, 0f), new Vector3(0.3f, 0.4f, 0.3f));
        }

        [MenuItem(Root + "Cleaning/Cleanable Stain", false, Priority + 40)]
        static void CreateStain(MenuCommand command)
        {
            var created = TidyEditorUtility.CreateObject("Cleanable Stain", command);
            created.AddComponent<TidyCleanable>();
            TidyEditorUtility.EnsureAimTrigger(created, new Vector3(0f, 0.01f, 0f), new Vector3(0.5f, 0.02f, 0.5f));
        }

        [MenuItem(Root + "Before After/Transformation Controller", false, Priority + 50)]
        static void CreateTransformation(MenuCommand command)
        {
            var created = TidyEditorUtility.CreateObject("Before After", command, false);
            created.AddComponent<TidyTransformationController>();
        }

        static GameObject CreateMinimalSystems()
        {
            var systems = new GameObject("Tidy Level Systems");
            systems.AddComponent<TidyLevelManager>();
            systems.AddComponent<Hints.TidyHintSystem>();
            systems.AddComponent<Feedback.TidyFeedbackPlayer>();
            systems.AddComponent<Tutorial.TidyTutorialRunner>();
            systems.AddComponent<Interaction.TidyOutlineHighlighter>();
            Undo.RegisterCreatedObjectUndo(systems, "Create Tidy Level Systems");
            return systems;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }
    }
}
