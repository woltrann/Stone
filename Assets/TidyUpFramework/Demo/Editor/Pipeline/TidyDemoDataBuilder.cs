using System.Collections.Generic;
using TidyUpFramework.Feedback;
using TidyUpFramework.Hints;
using TidyUpFramework.Input;
using TidyUpFramework.Objects;
using TidyUpFramework.Scoring;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyDemoDataBuilder
    {
        static readonly (string name, Color color)[] ColorTags =
        {
            ("Red", new Color32(214, 74, 64, 255)),
            ("Blue", new Color32(70, 120, 214, 255)),
            ("Green", new Color32(86, 170, 92, 255)),
            ("Yellow", new Color32(240, 196, 66, 255)),
            ("Purple", new Color32(146, 92, 196, 255)),
            ("Orange", new Color32(236, 132, 54, 255)),
            ("Pink", new Color32(236, 130, 170, 255)),
            ("Brown", new Color32(138, 92, 60, 255)),
            ("Teal", new Color32(60, 170, 168, 255)),
            ("White", new Color32(236, 232, 222, 255))
        };

        static readonly (string name, TidyTagGroup group, Color color)[] OtherTags =
        {
            ("Small", TidyTagGroup.Size, new Color32(180, 200, 220, 255)),
            ("Medium", TidyTagGroup.Size, new Color32(160, 180, 210, 255)),
            ("Large", TidyTagGroup.Size, new Color32(130, 150, 200, 255)),
            ("Magic", TidyTagGroup.Theme, new Color32(170, 120, 255, 255)),
            ("Fragile", TidyTagGroup.Material, new Color32(170, 230, 255, 255)),
            ("Heavy", TidyTagGroup.Material, new Color32(120, 110, 100, 255)),
            ("Trash", TidyTagGroup.Theme, new Color32(150, 140, 120, 255)),
            ("Collectible", TidyTagGroup.Theme, new Color32(255, 200, 120, 255)),
            ("Round", TidyTagGroup.Shape, new Color32(200, 200, 200, 255)),
            ("Boxy", TidyTagGroup.Shape, new Color32(200, 200, 200, 255))
        };

        public static void BuildShared(TidyDemoContext context)
        {
            BuildTags(context);
            BuildProfiles(context);
            BuildSystems(context);
            AssetDatabase.SaveAssets();
        }

        public static TidyCategoryDefinition Category(TidyDemoContext context, string categoryName, string displayName, Color color, TidyCategoryDefinition parent = null)
        {
            var category = TidyAssetFactory.CreateOrLoad<TidyCategoryDefinition>(TidyDemoPaths.Categories + "/Category_" + categoryName + ".asset");
            category.AssignId(TidyUpFramework.Editor.TidyEditorUtility.Slug(categoryName));
            category.AssignPresentation(displayName, color);
            category.AssignParent(parent);
            TidyAssetFactory.MarkDirty(category);
            context.Add(category);
            return category;
        }

        public static TidyObjectDefinition Definition(TidyDemoContext context, string theme, string id, string displayName, TidyCategoryDefinition category, IEnumerable<TidyTagDefinition> tags, float size, float weight, int priority = 0, string sortKey = null, int sequence = 0, bool stackable = false, bool physics = false, int score = 10, string inspectText = null)
        {
            var definition = TidyAssetFactory.CreateOrLoad<TidyObjectDefinition>(TidyDemoPaths.Objects + "/" + theme + "/Object_" + id + ".asset");
            definition.AssignId(id);
            definition.AssignPresentation(displayName, category != null ? category.DisplayColor : Color.white);
            definition.Configure(category, tags, size, weight, priority, sortKey, sequence);
            definition.ConfigureCapabilities(stackable, true, physics ? context.PhysicsProfile : context.ControlledProfile, score);
            definition.AssignInspectText(inspectText);
            TidyAssetFactory.MarkDirty(definition);
            context.Add(definition);
            return definition;
        }

        static void BuildTags(TidyDemoContext context)
        {
            foreach (var (tagName, color) in ColorTags)
            {
                CreateTag(context, tagName, TidyTagGroup.Color, color);
            }

            foreach (var (tagName, group, color) in OtherTags)
            {
                CreateTag(context, tagName, group, color);
            }
        }

        static void CreateTag(TidyDemoContext context, string tagName, TidyTagGroup group, Color color)
        {
            var tag = TidyAssetFactory.CreateOrLoad<TidyTagDefinition>(TidyDemoPaths.Tags + "/Tag_" + tagName + ".asset");
            tag.AssignId(TidyUpFramework.Editor.TidyEditorUtility.Slug(tagName));
            tag.AssignPresentation(tagName, color);
            tag.AssignGroup(group);
            TidyAssetFactory.MarkDirty(tag);
            context.Add(tag);
        }

        static void BuildProfiles(TidyDemoContext context)
        {
            context.ControlledProfile = TidyAssetFactory.CreateOrLoad<TidyPhysicsProfile>(TidyDemoPaths.Systems + "/PhysicsProfile_Controlled.asset");
            context.ControlledProfile.Configure(TidyInteractionMode.Controlled, 0.5f);
            TidyAssetFactory.MarkDirty(context.ControlledProfile);
            context.PhysicsProfile = TidyAssetFactory.CreateOrLoad<TidyPhysicsProfile>(TidyDemoPaths.Systems + "/PhysicsProfile_Physical.asset");
            context.PhysicsProfile.Configure(TidyInteractionMode.Physics, 1.2f);
            TidyAssetFactory.MarkDirty(context.PhysicsProfile);
        }

        static void BuildSystems(TidyDemoContext context)
        {
            context.Score = TidyAssetFactory.CreateOrLoad<TidyScoreDefinition>(TidyDemoPaths.Systems + "/Score_Default.asset");
            context.Combo = TidyAssetFactory.CreateOrLoad<TidyComboDefinition>(TidyDemoPaths.Systems + "/Combo_Default.asset");
            context.Hints = TidyAssetFactory.CreateOrLoad<TidyHintDefinition>(TidyDemoPaths.Systems + "/Hints_Default.asset");
            context.Hints.Configure(true, -1, 3, 25, 8f);
            context.Feedback = TidyAssetFactory.CreateOrLoad<TidyFeedbackDefinition>(TidyDemoPaths.Systems + "/Feedback_Default.asset");
            TidyAssetFactory.MarkDirty(context.Score);
            TidyAssetFactory.MarkDirty(context.Combo);
            TidyAssetFactory.MarkDirty(context.Hints);
            TidyAssetFactory.MarkDirty(context.Feedback);
            BuildInputPrompts();
        }

        static void BuildInputPrompts()
        {
            var prompts = TidyAssetFactory.CreateOrLoad<TidyInputPromptDefinition>(TidyDemoPaths.Settings + "/TidyUpInputPrompts.asset");
            prompts.Configure("<b><color=#FFE3A3>[{0}]</color></b>", new[]
            {
                new TidyInputPromptEntry { Action = TidyInputAction.Move, KeyboardMouseLabel = "WASD", GamepadLabel = "Left Stick", TouchLabel = "Joystick" },
                new TidyInputPromptEntry { Action = TidyInputAction.Look, KeyboardMouseLabel = "Mouse", GamepadLabel = "Right Stick", TouchLabel = "Drag" },
                new TidyInputPromptEntry { Action = TidyInputAction.Interact, KeyboardMouseLabel = "LMB", GamepadLabel = "A", TouchLabel = "Tap" },
                new TidyInputPromptEntry { Action = TidyInputAction.Drop, KeyboardMouseLabel = "RMB", GamepadLabel = "B", TouchLabel = "Drop" },
                new TidyInputPromptEntry { Action = TidyInputAction.Throw, KeyboardMouseLabel = "F", GamepadLabel = "RT", TouchLabel = "Throw" },
                new TidyInputPromptEntry { Action = TidyInputAction.Rotate, KeyboardMouseLabel = "Scroll", GamepadLabel = "LB/RB", TouchLabel = "Turn" },
                new TidyInputPromptEntry { Action = TidyInputAction.Inspect, KeyboardMouseLabel = "R", GamepadLabel = "Y", TouchLabel = "Look" },
                new TidyInputPromptEntry { Action = TidyInputAction.Sprint, KeyboardMouseLabel = "Shift", GamepadLabel = "L3", TouchLabel = "Push stick" },
                new TidyInputPromptEntry { Action = TidyInputAction.Crouch, KeyboardMouseLabel = "Ctrl", GamepadLabel = "R3" },
                new TidyInputPromptEntry { Action = TidyInputAction.Hint, KeyboardMouseLabel = "H", GamepadLabel = "D-Pad Up", TouchLabel = "Bulb" },
                new TidyInputPromptEntry { Action = TidyInputAction.Pause, KeyboardMouseLabel = "Esc", GamepadLabel = "Start", TouchLabel = "II" }
            });
            TidyAssetFactory.MarkDirty(prompts);
        }
    }
}
