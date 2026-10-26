# Tidy Up Framework — Architecture & Implementation Plan

Full product name: **Tidy Up + Sorting + Organizing + Placement + Satisfying Interaction Framework**
Root namespace: `TidyUpFramework` · Render pipeline: URP 17 · Unity 6000.3 · Input: Input System 1.17

## 1. Guiding principles

- Framework code never references demo content. Demo content consumes the framework through the same public API a buyer uses.
- Data lives in ScriptableObjects, behaviour lives in small components, wiring happens through C# events and a tiny service registry (no static god-managers).
- Every gameplay question ("does this object belong here?") is answered by a **Rule** asset, so new sorting logic never needs code.
- Scene objects are linked to data by string IDs (`TidyId`), so ScriptableObjects never hold scene references.
- No comments in C#. Intent is carried by naming, small classes, `[Tooltip]`s and the external documentation.

## 2. Assemblies

| Assembly | Folder | References |
|---|---|---|
| `TidyUpFramework.Runtime` | `Runtime/` | Unity.InputSystem, Unity.TextMeshPro, UnityEngine.UI, URP Runtime |
| `TidyUpFramework.Editor` | `Editor/` | Runtime (Editor only) |
| `TidyUpFramework.Demo` | `Demo/Scripts/` | Runtime |
| `TidyUpFramework.Demo.Editor` | `Demo/Editor/` | Runtime, Demo, Editor (content generators) |

## 3. Runtime modules (namespace = folder)

| Module | Key types |
|---|---|
| `Core` | `TidyServices` (registry), `TidyId`, `TidyTween` / `TidyTweenRunner`, `TidyPool<T>`, `TidyEase` |
| `Objects` | `TidyObjectDefinition`, `TidyCategoryDefinition`, `TidyTagDefinition` (+ `TidyTagGroup`: Color/Size/Shape/Material/Generic), `TidyObject`, `TidyObjectRegistry`, `TidyPhysicsProfile` |
| `Rules` | `TidyRuleDefinition` (abstract) → Category, Tag, AnyTag, Definition, SizeRange, WeightRange, Priority, All/Any/Not composite; `TidyOrderingDefinition` (Alphabetical / Priority / SortKey / Sequence) |
| `Placement` | `ITidyPlacementTarget`, `TidyPlacementSlot`, `TidySlotGroup` (shelf/grid), `TidyContainer`, `TidyFreePlacementZone`, `TidyStackPoint`, `TidyPlacementResolver`, `TidyPlacementPolicy` |
| `Interaction` | `TidyInteractor`, `TidyHoldController` (Controlled/Physics), `TidyPlacementPreview` (ghost), `TidyHighlighter` (outline), `TidyInspectMode`, `ITidyInteractable` |
| `Objectives` | `TidyObjectiveDefinition` (abstract) → PlaceMatching, SortAll, FillTarget, CompleteCategory, ClearClutter, CleanSurfaces, FindObjects, ReachScore, OrderTarget; `TidyObjectiveTracker` |
| `Levels` | `TidyLevelDefinition`, `TidyLevelCatalog`, `TidyLevelManager`, `TidyLevelState`, `TidyLevelResult`, `TidySceneLoader` |
| `Scoring` | `TidyScoreDefinition`, `TidyScoreSystem`, `TidyComboDefinition`, `TidyComboSystem` |
| `Hints` | `TidyHintDefinition`, `TidyHintSystem`, `TidyHintIndicator` |
| `Transformation` | `TidyTransformationController`, `TidyProgressReactive` (+ fade, light, color, toggle, material-blend reactors) |
| `Cleaning` | `TidyCleanable` (stain / dust), `TidyClutter`, `TidyDisposalContainer` |
| `Feedback` | `TidyFeedbackDefinition`, `TidyFeedbackPlayer`, `TidyFloatingText`, `TidyCameraPunch`, `TidyParticlePool` |
| `Player` | `TidyFirstPersonController`, `TidyPlayerLook`, `ITidyMotor` |
| `Input` | `TidyInputReader` (wraps InputActionAsset by name — no generated code), `TidyInputScheme`, `TidyInputPromptDefinition`, `TidyVirtualJoystick`, `TidyTouchLookArea` |
| `UI` | `TidyHud`, `TidyObjectiveListView`, `TidyComboView`, `TidyPauseMenu`, `TidyMainMenu`, `TidyLevelSelect`, `TidyResultsScreen`, `TidySettingsMenu`, `TidyPromptLabel`, `TidyScreenStack`, `TidySafeArea` |
| `Audio` | `TidyAudioCue`, `TidyAudioLibrary`, `TidyAudioService` (pooled sources, mixer groups) |
| `Save` | `ITidySaveStorage` (`TidyJsonFileStorage`, `TidyPlayerPrefsStorage`), `TidySaveService`, `TidyProgressData`, `TidySettingsData` |
| `Settings` | `TidySettingsService`, `TidyGraphicsApplier`, `TidyAudioApplier` |
| `Tutorial` | `TidyTutorialDefinition`, `TidyTutorialStep`, `TidyTutorialRunner` |

## 4. Data flow

```
Input → TidyInteractor ─pick/hold/place→ TidyObject ─request→ ITidyPlacementTarget
                                              │                  │ (Rule asset validates)
                                              ▼                  ▼
                                 TidyObjectRegistry  ←events─ Placement result
                                              │
                         TidyLevelManager (state hub, raises OnObjectPlaced/Removed/Cleaned)
                 ┌───────────┬──────────────┼───────────────┬──────────────┐
          Objective trackers  Score+Combo    Transformation   Hints / Tutorial
                 └───────────┴──────┬───────┴───────────────┘
                                    ▼
                           HUD / Feedback / Audio → Results → SaveService
```

## 5. Content pipeline (original assets)

All demo meshes, textures and audio are produced by editor generators in `Demo/Editor` and saved as regular project assets (`.asset` meshes, `.png` textures, `.wav` audio), so the shipped package contains plain assets and does not depend on the generators:

- Mesh kit: beveled boxes, lathe/revolve shapes (bottles, cans, candles, jars, potions), extrusions (shelves, frames, arches), faceted crystals, rolled scrolls, composite toys. Smooth normals are baked into UV3 for clean outlines.
- Texture kit: procedural covers, labels, wood/stone/tile patterns, UI sprites.
- Audio kit: synthesized pickup/place/snap/error/combo/complete/ui clips and soft ambient loops.

## 6. Demo scenes

1. **Arcane Library** — books by category/colour + alphabetical volumes, scrolls into bins, potions on stands, candles, before/after floor clutter, tutorial.
2. **Toy & Collectible Store** — hundreds of collectible figures, category + colour shelves, triplet matching, quantity objectives, combo-heavy.
3. **Warehouse** — crates/packages by colour & size, pallet stacking, zones, containers.
4. **Cozy Room** — cleaning stains, trash disposal, surface organisation, before/after.
Plus `MainMenu` and `Sandbox_Blank` (acceptance-test template).

## 7. Implementation order

1. Assemblies, Core, Objects, Rules
2. Placement, Interaction, Highlight, Tween
3. Objectives, Levels, Score, Combo, Hints, Transformation, Cleaning
4. Input, Player, UI, Save, Settings, Audio, Tutorial, Feedback
5. Editor tools (inspectors, creation menus, Level Validator)
6. Content generators → meshes, materials, textures, audio, prefabs
7. Demo scenes, lighting, post-processing
8. Documentation (HTML with screenshots and diagrams) + API reference
9. Validation pass and acceptance test
