using TidyUpFramework.Input;
using TidyUpFramework.Levels;
using TidyUpFramework.Objectives;
using TidyUpFramework.Rules;
using TidyUpFramework.Tutorial;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyRoomLevel
    {
        public const string LevelPath = TidyDemoPaths.Levels + "/Level_04_CozyRoom.asset";

        public static TidyLevelDefinition Build(TidyDemoContext context, TidyLevelDefinition previous)
        {
            if (!context.HasDefinition("room_sponge"))
            {
                return null;
            }

            var clean = CleanObjective();
            var clutter = ClutterObjective();
            var clothes = TidyDemoLevels.Place(context, "Room_Clothes", "Fold clothes into the wardrobe {current}/{target}", "T-shirts and sweaters each have their own shelf.", 120, TidyDemoLevels.Filter(context.Category("RoomTops"), context.Category("RoomKnitwear")), 0);
            var laundry = TidyDemoLevels.Place(context, "Room_Laundry", "Toss the socks into the laundry basket {current}/{target}", "Every stray pair belongs in the wicker basket.", 60, TidyDemoLevels.Filter(context.Category("RoomSocks")), 0);
            var shoes = TidyDemoLevels.Place(context, "Room_Shoes", "Line the shoes up on the rack {current}/{target}", "The shoe rack is by the door.", 80, TidyDemoLevels.Filter(context.Category("RoomShoes")), 0);
            var toys = TidyDemoLevels.Place(context, "Room_Toys", "Put the toys in the toy chest {current}/{target}", "Plushies and blocks all go in the chest.", 80, TidyDemoLevels.Filter(context.Category("RoomToys")), 0);
            var dishes = TidyDemoLevels.Place(context, "Room_Dishes", "Return the dishes to the kitchenette {current}/{target}", "Mugs on the tray, plates stacked on the board.", 80, TidyDemoLevels.Filter(context.Category("RoomDishes")), 0);
            var books = TidyDemoLevels.Place(context, "Room_Books", "Shelve the books {current}/{target}", "The bookshelf has a free shelf waiting.", 60, TidyDemoLevels.Filter(context.Category("RoomBooks")), 0);
            var keepsakes = FindObjective();
            var careful = NoMistakesObjective();
            var level = TidyAssetFactory.CreateOrLoad<TidyLevelDefinition>(LevelPath);
            level.ConfigureIdentity("cozy_room", "Moving-in Day", "Boxes unpacked, chaos everywhere. Scrub the stains, vacuum the dust bunnies, fold the laundry and find your little keepsakes until this room finally feels like home.", TidyDemoPaths.CozyRoomScene, TidyDifficulty.Relaxed, new Color32(244, 170, 140, 255));
            level.ConfigureObjectives(new[]
            {
                TidyDemoLevels.Entry(clean, false),
                TidyDemoLevels.Entry(clutter, false),
                TidyDemoLevels.Entry(clothes, false),
                TidyDemoLevels.Entry(laundry, false),
                TidyDemoLevels.Entry(shoes, false),
                TidyDemoLevels.Entry(toys, false),
                TidyDemoLevels.Entry(dishes, false),
                TidyDemoLevels.Entry(books, false),
                TidyDemoLevels.Entry(keepsakes, true),
                TidyDemoLevels.Entry(careful, true)
            });
            level.ConfigureTiming(0f, false, TidyStarMetric.Mistakes, new Vector2(3f, 0f));
            level.ConfigureSystems(context.Score, context.Combo, context.Hints, Tutorial(context));
            level.ConfigureAudio(context.Cue("Music_Room"), context.Cue("Ambience_Room"));
            level.ConfigureProgression(new TidyUnlockRequirement { UnlockedByDefault = previous == null, RequiredLevel = previous }, 150, "House-Warming Plant");
            EditorUtility.SetDirty(level);
            return level;
        }

        static TidyCleanSurfacesObjective CleanObjective()
        {
            var objective = TidyAssetFactory.CreateOrLoad<TidyCleanSurfacesObjective>(TidyDemoPaths.Objectives + "/Objective_Room_Clean.asset");
            objective.Configure(string.Empty, 0);
            objective.ConfigurePresentation("room_clean", "Scrub away the stains {current}/{target}", "Grab the sponge from the kitchenette and scrub every grubby mark.", 150);
            EditorUtility.SetDirty(objective);
            return objective;
        }

        static TidyClearClutterObjective ClutterObjective()
        {
            var objective = TidyAssetFactory.CreateOrLoad<TidyClearClutterObjective>(TidyDemoPaths.Objectives + "/Objective_Room_Clutter.asset");
            objective.ConfigurePresentation("room_clutter", "Clear the rubbish and dust {current}/{target}", "Bin the rubbish and vacuum up every dust bunny.", 150);
            EditorUtility.SetDirty(objective);
            return objective;
        }

        static TidyFindObjectsObjective FindObjective()
        {
            var objective = TidyAssetFactory.CreateOrLoad<TidyFindObjectsObjective>(TidyDemoPaths.Objectives + "/Objective_Room_Keepsakes.asset");
            objective.ConfigurePresentation("room_keepsakes", "Find the lost keepsakes {current}/{target}", "A key, a photo and a little velvet box are hiding somewhere. Try under the bed and behind the plants.", 200);
            EditorUtility.SetDirty(objective);
            return objective;
        }

        static TidyNoMistakesObjective NoMistakesObjective()
        {
            var objective = TidyAssetFactory.CreateOrLoad<TidyNoMistakesObjective>(TidyDemoPaths.Objectives + "/Objective_Room_NoMistakes.asset");
            objective.ConfigurePresentation("room_no_mistakes", "Tidy without a single mistake", "Everything in its right place, first time.", 150);
            objective.ConfigureCounterVisibility(true);
            EditorUtility.SetDirty(objective);
            return objective;
        }

        static TidyTutorialDefinition Tutorial(TidyDemoContext context)
        {
            var tools = TidyMatchFilter.ForDefinitions(new[] { context.Definition("room_sponge") });
            var vacuum = TidyMatchFilter.ForDefinitions(new[] { context.Definition("room_vacuum") });
            var trash = TidyMatchFilter.Create(null, new[] { context.Tag("Trash") });
            var tutorial = TidyAssetFactory.CreateOrLoad<TidyTutorialDefinition>(TidyDemoPaths.Levels + "/Tutorial_CozyRoom.asset");
            tutorial.Configure("tutorial_cozy_room", true, new[]
            {
                TidyDemoLevels.Step("Moving-in day! Take a look at your new room with {Look}.", TidyInputAction.Look, TidyTutorialCompletion.LookAround),
                TidyDemoLevels.Step("Grab the yellow sponge on the kitchenette counter with {Interact}.", TidyInputAction.Interact, TidyTutorialCompletion.PickUpMatching, tools),
                TidyDemoLevels.Step("Aim at a grubby stain and hold {Interact} to scrub it away.", TidyInputAction.Interact, TidyTutorialCompletion.CleanAny),
                TidyDemoLevels.Step("Press {Drop} to put the sponge down again.", TidyInputAction.Drop, TidyTutorialCompletion.Drop),
                TidyDemoLevels.Step("Rubbish belongs in a bin. Pick up a crumpled note or a can and drop it into a bin.", TidyInputAction.Interact, TidyTutorialCompletion.PlaceCorrectly, trash),
                TidyDemoLevels.Step("Dust bunnies everywhere! Pick up the handheld vacuum by the door.", TidyInputAction.Interact, TidyTutorialCompletion.PickUpMatching, vacuum),
                TidyDemoLevels.Step("Aim at the floor near dust and hold {Interact} to suck it up. Then fold, shelve and stash everything else. Make it home!", TidyInputAction.Interact, TidyTutorialCompletion.PressContinue)
            });
            EditorUtility.SetDirty(tutorial);
            return tutorial;
        }
    }
}
