# Script Reference

This document maps every active script in the `Final game` Unity project after the script-folder reorganization.

## How To Read This

- `Scene-attached`: the script is attached to an object saved in a scene.
- `Auto-entry`: the script registers itself with `RuntimeInitializeOnLoadMethod` or creates a runtime object automatically.
- `Runtime-created`: the script is added to generated objects during play.
- `Data/helper`: the script mainly defines shared data, settings, or helper behavior.
- `Editor-only`: the script is under `Assets/Editor` and should not run in builds.

The project style is: scene Hierarchy owns editable UI/world structure, while runtime code binds objects by known names/paths and controls behavior.

## Start

### `Assets/Scripts/Start/Menu/StartMenuController.cs`

- Type: `Scene-attached`, `Auto-entry`
- Owner scene: `Start`
- Purpose: Main menu controller. Binds the editable `StartMenuCanvas`, wires the four mode cards, Settings/Records/About panels, and scene navigation to `OceanRhythm`, `VerticalRunner`, `AdvancedRunner`, and `WorldMusicExplorer`.
- Important dependencies: `RuntimeScenePolicy`, `SceneTransitionManager`, UI object names under `StartMenuCanvas`.
- Safe to rename: No. Unity component class/file name and auto-entry behavior should stay stable.

### `Assets/Scripts/Start/Visuals/StartMenuMusicVisualizer.cs`

- Type: `Scene-attached`, `Runtime-created helpers`
- Owner scene: `Start`
- Purpose: Animates hierarchy-owned note/icon templates under `StartMenuCanvas/Root/music` into non-interactive staff/wave music visuals.
- Important dependencies: `menuMusic` AudioSource when available, UI object names under `Root/music`.
- Safe to rename: No. The Start scene and bakers reference this component by GUID/type.

### `Assets/Scripts/Start/Records/LeaderboardManager.cs`

- Type: `Data/helper`
- Owner scene: Shared by Start RecordsPanel and score-saving callers.
- Purpose: Stores and reads Easy/Hard leaderboard scores using `PlayerPrefs`; visual presentation comes from the hierarchy-owned `RecordsPanel` templates.
- Important dependencies: `LeaderboardMode`, active scene name for mode detection.
- Safe to rename: Usually yes from scene-reference perspective, but update code references if renamed.

## OceanRhythm

### `Assets/Scripts/OceanRhythm/OceanRhythmManager.cs`

- Type: `Scene-attached`, `Auto-entry`
- Owner scene: `OceanRhythm`
- Purpose: Main controller for Little Rhythm Ocean. Owns phase flow, fish/rhythm lessons, track library, timing, input handling, score/catch state, music setup, and transition back to Start.
- Important dependencies: `OceanRhythmUIController`, `OceanRhythmData`, `SimpleMetronomeAudio`, `RuntimeScenePolicy`, `SceneTransitionManager`.
- Safe to rename: No. It is a core scene component and auto-entry point.

### `Assets/Scripts/OceanRhythm/OceanRhythmUIController.cs`

- Type: `Scene-attached`, `Runtime-created helpers`
- Owner scene: `OceanRhythm`
- Purpose: Binds the editable `OceanRhythmCanvas`, controls Ocean UI text/buttons/overlays, creates pond animals, beat bubbles, bucket album UI, sound match UI, and decoration items.
- Important dependencies: `OceanRhythmManager`, `OceanPondAnimal`, `OceanAnimalController`, `OceanNetCursor`, `OceanBucketSlot`, `OceanDecorationDragItem`, `WaterRippleController`, scene object paths under `OceanRoot`.
- Safe to rename: No. It is referenced and created by `OceanRhythmManager`.

### `Assets/Scripts/OceanRhythm/OceanRhythmData.cs`

- Type: `Data/helper`
- Owner scene: `OceanRhythm`
- Purpose: Defines ocean data types: fish types, decoration rewards, bucket slots, unlock requirements, and `OceanBucketInventory` PlayerPrefs persistence.
- Important dependencies: `PlayerPrefs`, ocean UI/manager code.
- Safe to rename: Usually yes from scene-reference perspective, but update code references if renamed.

### `Assets/Scripts/OceanRhythm/OceanPondAnimal.cs`

- Type: `Scene-attached`, `Runtime-created`
- Owner scene: `OceanRhythm`
- Purpose: Represents an interactive pond animal/fish. Handles lesson assignment, selection visuals, capture progress bubbles, movement, and click interaction.
- Important dependencies: `OceanRhythmManager`, `OceanRhythmUIController`, `OceanLesson`, `OceanFishType`.
- Safe to rename: No, because UI generation adds it by type.

### `Assets/Scripts/OceanRhythm/OceanAnimalController.cs`

- Type: `Scene-attached`, `Runtime-created`
- Owner scene: `OceanRhythm`
- Purpose: Controls a guided ocean animal UI object, including fallback image/text setup and sprite animation.
- Important dependencies: `OceanSpriteAnimator`, Unity UI Image/Text.
- Safe to rename: No if generated guided animal behavior remains.

### `Assets/Scripts/OceanRhythm/OceanNetCursor.cs`

- Type: `Scene-attached`, `Runtime-created`
- Owner scene: `OceanRhythm`
- Purpose: Visual and positional controller for the fishing net cursor.
- Important dependencies: Unity UI Image, pointer/mouse position from ocean UI flow.
- Safe to rename: No if scene/generated net cursor binding remains.

### `Assets/Scripts/OceanRhythm/OceanBucketSlot.cs`

- Type: `Scene-attached`, `Runtime-created`
- Owner scene: `OceanRhythm`
- Purpose: Represents one bucket decoration slot. Handles slot visuals, labels, click/pointer interaction, and placed decoration display.
- Important dependencies: `OceanRhythmUIController`, `OceanBucketSlotId`, `OceanDecorationReward`.
- Safe to rename: No if bucket album generation remains.

### `Assets/Scripts/OceanRhythm/OceanDecorationDragItem.cs`

- Type: `Runtime-created`
- Owner scene: `OceanRhythm`
- Purpose: Drag/click behavior for a decoration item in the bucket album. Supports unlock gating, drag highlight, drop placement, and info display.
- Important dependencies: `OceanRhythmUIController`, `OceanDecorationReward`, Unity event interfaces.
- Safe to rename: No. It is added by `OceanRhythmUIController`.

### `Assets/Scripts/OceanRhythm/OceanSpriteAnimator.cs`

- Type: `Scene-attached`, `Runtime-created`
- Owner scene: `OceanRhythm`
- Purpose: Lightweight frame-based sprite animator for ocean animal images.
- Important dependencies: Unity UI Image, sprite frame arrays.
- Safe to rename: No if ocean animal setup still creates it by type.

### `Assets/Scripts/OceanRhythm/SimpleMetronomeAudio.cs`

- Type: `Scene-attached`, `Runtime-created`
- Owner scene: `OceanRhythm`
- Purpose: Simple beat/metronome audio helper used by Ocean rhythm playback.
- Important dependencies: Unity `AudioSource`.
- Safe to rename: No if `OceanRhythmManager` continues creating it by type.

### `Assets/Scripts/OceanRhythm/WaterRippleController.cs`

- Type: `Scene-attached`, `Runtime-created`
- Owner scene: `OceanRhythm`
- Purpose: Creates expanding UI ripple effects from pointer/click positions.
- Important dependencies: Unity UI Image/RectTransform.
- Safe to rename: No if ocean UI root continues adding it by type.

## VerticalRunner

### `Assets/Scripts/VerticalRunner/VerticalRunnerManager.cs`

- Type: `Scene-attached`, `Auto-entry`
- Owner scene: `VerticalRunner`
- Purpose: Main controller for vertical runner. Owns tutorial/game mode flow, countdown, input handling, score/miss/combo state, route/player/camera/UI setup, and reset/retry behavior.
- Important dependencies: `VerticalBeatSpawner`, `VerticalRunnerUI`, `VerticalRunnerPlayer`, `VerticalRunnerCamera`, `VerticalRunnerTemplates`, `RhythmManager`, `RuntimeScenePolicy`.
- Safe to rename: No. It is a scene component and auto-entry point.

### `Assets/Scripts/VerticalRunner/VerticalBeatSpawner.cs`

- Type: `Scene-attached`, `Runtime-created helpers`
- Owner scene: `VerticalRunner`
- Purpose: Builds the vertical route: platforms, long platforms, bananas, parrot obstacles, finish object, and generated gameplay objects.
- Important dependencies: `VerticalRunnerSettings`, `VerticalRunnerTemplates`, `VerticalRunnerObjects`, `RuntimeScenePolicy`.
- Safe to rename: No if manager/baker references remain.

### `Assets/Scripts/VerticalRunner/VerticalRunnerUI.cs`

- Type: `Scene-attached`, `Runtime-created fallback`
- Owner scene: `VerticalRunner`
- Purpose: Binds `VerticalRunnerCanvas`, controls HUD text, beat lane, tutorial/game overlays, result screen, damage flash, game controls, and button listeners.
- Important dependencies: `VerticalRunnerManager`, `SceneTransitionManager`, UI object paths under `VerticalRunnerCanvas`.
- Safe to rename: No. It is attached/created by manager and baker.

### `Assets/Scripts/VerticalRunner/VerticalRunnerPlayer.cs`

- Type: `Scene-attached`, `Runtime-created`
- Owner scene: `VerticalRunner`
- Purpose: Player movement and state. Handles jump arcs, recover behavior, branch movement, collision with pickups/obstacles, and player visuals/colliders.
- Important dependencies: `VerticalRunnerPlatform`, `VerticalRunnerPickup`, `VerticalRunnerObstacle`.
- Safe to rename: No. It is attached to the player template and may be added by spawner/manager.

### `Assets/Scripts/VerticalRunner/VerticalRunnerCamera.cs`

- Type: `Scene-attached`, `Runtime-created`
- Owner scene: `VerticalRunner`
- Purpose: Camera follow helper for the vertical runner player/world.
- Important dependencies: `VerticalRunnerManager`, scene camera.
- Safe to rename: No if manager/baker references remain.

### `Assets/Scripts/VerticalRunner/VerticalRunnerSettings.cs`

- Type: `Data/helper`
- Owner scene: `VerticalRunner`
- Purpose: Defines vertical runner modes, tutorial step types, and serialized gameplay/timing/score/sprite/color settings.
- Important dependencies: `VerticalRunnerManager`, `VerticalBeatSpawner`.
- Safe to rename: Usually yes from scene-reference perspective, but update code references if renamed.

### `Assets/Scripts/VerticalRunner/VerticalRunnerTemplates.cs`

- Type: `Scene-attached`
- Owner scene: `VerticalRunner`
- Purpose: Stores editable hierarchy template references for player, platforms, pickups, obstacles, finish, and runtime root.
- Important dependencies: `VerticalBeatSpawner`, scene object `VerticalRunnerTemplates`.
- Safe to rename: No if scene/baker references remain.

### `Assets/Scripts/VerticalRunner/VerticalRunnerObjects.cs`

- Type: `Runtime-created`, `Data/helper`
- Owner scene: `VerticalRunner`
- Purpose: Defines runtime components for generated vertical objects: platform route nodes, pickups, obstacles, and beat-pulse visuals.
- Important dependencies: `RhythmManager`, `VerticalBeatSpawner`, `VerticalRunnerPlayer`.
- Safe to rename: Usually yes from scene-reference perspective, but update code references if renamed.

### `Assets/Scripts/VerticalRunner/VerticalScrollingBackground.cs`

- Type: `Scene-attached`
- Owner scene: `VerticalRunner`
- Purpose: Creates and moves vertical background tiles so the background covers the camera and scrolls smoothly without obvious seams.
- Important dependencies: Camera, SpriteRenderer, background sprite object named `vertical`.
- Safe to rename: No if scene/baker references remain.

## AdvancedRunner

### `Assets/Scripts/AdvancedRunner/AdvancedRunner.cs`

- Type: `Auto-entry`, main implementation
- Owner scene: `AdvancedRunner`
- Purpose: Main advanced runner implementation. Contains the bulk of `AdvancedRunnerManager`, `AdvancedRunnerPlayer`, and `AdvancedRunnerUI` partial class logic plus advanced settings, feedback style, target chart, music stages, input timing, scoring, game-over flow, and scene binding.
- Important dependencies: `AdvancedRunnerManager.cs`, `AdvancedRunnerPlayer.cs`, `AdvancedRunnerUI.cs` bridge files, `AdvancedRunnerFeedbackConfig`, `AdvancedRunnerMusicConfig`, `RhythmManager`, `SceneTransitionManager`, `RuntimeScenePolicy`.
- Safe to rename: No. It contains the partial implementation and auto-entry hook.

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerManager.cs`

- Type: `Scene-attached`, partial bridge
- Owner scene: `AdvancedRunner`
- Purpose: Unity same-name MonoBehaviour bridge for `AdvancedRunnerManager`. The actual logic lives in `AdvancedRunner.cs`.
- Important dependencies: Must stay partial with class name `AdvancedRunnerManager`.
- Safe to rename: No. Unity component stability depends on this file/class name.

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerPlayer.cs`

- Type: `Scene-attached`, partial bridge
- Owner scene: `AdvancedRunner`
- Purpose: Unity same-name MonoBehaviour bridge for `AdvancedRunnerPlayer`. The actual player logic lives in `AdvancedRunner.cs`.
- Important dependencies: Must stay partial with class name `AdvancedRunnerPlayer`.
- Safe to rename: No. Unity component stability depends on this file/class name.

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerUI.cs`

- Type: partial bridge
- Owner scene: `AdvancedRunner`
- Purpose: Unity same-name MonoBehaviour bridge for `AdvancedRunnerUI`. The actual UI binding and result/overlay logic lives in `AdvancedRunner.cs`.
- Important dependencies: Must stay partial with class name `AdvancedRunnerUI`.
- Safe to rename: No. Unity component stability depends on this file/class name.

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerFeedbackConfig.cs`

- Type: `Scene-attached`, config
- Owner scene: `AdvancedRunner`
- Purpose: Hierarchy-editable feedback config wrapper. Exposes `AdvancedFeedbackStyle` so designers can edit feedback text, color, font, and pulse behavior from a scene object.
- Important dependencies: `AdvancedFeedbackStyle` in `AdvancedRunner.cs`.
- Safe to rename: No if scene config object references remain.

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerMusicConfig.cs`

- Type: `Scene-attached`, config
- Owner scene: `AdvancedRunner`
- Purpose: Hierarchy-editable music config wrapper for a phase's BGM clip and BPM.
- Important dependencies: `AdvancedRunnerManager` reads Scene/Tutorial/Game config objects from `AdvancedRunnerConfig/Music`.
- Safe to rename: No if scene config object references remain.

## Shared

### `Assets/Scripts/Shared/Navigation/ChangeScene.cs`

- Type: `Scene-attached`
- Owner scene: `Start`, `VerticalRunner`
- Purpose: Generic button helper for loading scenes by build index or scene name.
- Important dependencies: `SceneTransitionManager`, Unity UI Button.
- Safe to rename: No if scene buttons still have this component.

### `Assets/Scripts/Shared/Navigation/SceneTransitionManager.cs`

- Type: `Runtime-created singleton`
- Owner scene: Shared
- Purpose: Central scene loading helper with fade overlay. Creates itself and a persistent transition canvas when `LoadScene` is called.
- Important dependencies: `SceneManager`, Unity UI Canvas/Image/CanvasGroup.
- Safe to rename: No if callers continue using `SceneTransitionManager.LoadScene`.

### `Assets/Scripts/Shared/Runtime/RuntimeScenePolicy.cs`

- Type: `Data/helper`
- Owner scene: Shared
- Purpose: Serializable policy used by managers to decide whether to use existing scene objects, auto-create missing objects, rebuild UI, preserve image overrides, and where runtime-generated objects go.
- Important dependencies: `StartMenuController`, `OceanRhythmManager`, `VerticalRunnerManager`, `AdvancedRunnerManager`, spawners/UI controllers.
- Safe to rename: Usually yes from scene-reference perspective, but update code references if renamed.

### `Assets/Scripts/Shared/Rhythm/RhythmManager.cs`

- Type: `Scene-attached`, `Runtime-created fallback`
- Owner scene: Shared, used strongly by `VerticalRunner` and `AdvancedRunner`
- Purpose: General rhythm timing manager. Tracks BPM/audio timing, reports input timing quality, manages beat visualization/debug UI, timing windows, and optional fallback music source.
- Important dependencies: `AudioSource`, Unity UI, `VerticalRunnerManager`, `AdvancedRunnerManager`, `VerticalRunnerBeatPulse`.
- Safe to rename: No if scene references and generated fallback setup remain.

## Editor / HierarchyBakers

### `Assets/Editor/HierarchyBakers/AllSceneHierarchyBaker.cs`

- Type: `Editor-only`
- Owner scene: Project maintenance tool
- Purpose: Main all-scene baker for creating/repairing editable scene hierarchy contracts across Start, OceanRhythm, VerticalRunner, and AdvancedRunner. Also maintains UI defaults, templates, config objects, backgrounds, and runtime policy defaults.
- Important dependencies: UnityEditor APIs, active scene paths, project hierarchy naming contract.
- Safe to rename: Usually yes from runtime perspective, but menu/tool references and documentation should be updated.

### `Assets/Editor/HierarchyBakers/SceneHierarchyBaker.cs`

- Type: `Editor-only`
- Owner scene: Start + OceanRhythm maintenance
- Purpose: Smaller baker focused on rebuilding Start and OceanRhythm hierarchy contracts.
- Important dependencies: UnityEditor APIs, `StartMenuController`, `OceanRhythmManager`.
- Safe to rename: Usually yes from runtime perspective, but menu/tool references and documentation should be updated.

### `Assets/Editor/HierarchyBakers/VerticalSceneHierarchyBaker.cs`

- Type: `Editor-only`
- Owner scene: VerticalRunner maintenance
- Purpose: VerticalRunner-specific baker for manager setup, templates, background, canvas, HUD, overlays, result UI, game controls, and beat lane.
- Important dependencies: UnityEditor APIs, `VerticalRunnerManager`, `VerticalBeatSpawner`, `VerticalRunnerUI`, `VerticalRunnerTemplates`.
- Safe to rename: Usually yes from runtime perspective, but menu/tool references and documentation should be updated.

## Notes About Deleted Legacy/Unused Scripts

These scripts were intentionally removed before this document was created because static checks found no active scene, asset, code, or startup references:

- `Bonus`
- old `SoundManager`
- old `LoginManager`
- `RhythmGeneratedObstacle`

Do not restore legacy bunny runner files from `../BunnyLegacyArchive` unless explicitly requested.
