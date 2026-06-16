# Card register

Pair with [BOARD.md](./BOARD.md). Add a card here before opening a GitHub Issue.

---

## RR-001 Archive legacy bunny to BunnyLegacyArchive

E-06 / P1 / Done / 2026-06-04

Moved old horizontal bunny scripts, prefabs, and backgrounds out of `Assets` into `../BunnyLegacyArchive` so Unity no longer compiles that stack.

Done: no legacy bunny under `Assets`; Editor opens `Start` cleanly.

---

## RR-002 Start routes to four mode scenes

E-01 / P1 / Done / 2026-06-05

Each mode card on `Start` loads Ocean, Vertical, Advanced, or WorldMusicExplorer.

Done: every card loads the right scene; Back returns to `Start`.

---

## RR-003 WorldMusicExplorer scene

E-02 / P1 / Done / 2026-06-11

Added `WorldMusicExplorer` scene and `WorldMusicExplorerController`: keyboard item switch, AudioSource playback, visual layer toggles.

Done: items cycle and play in scene; Back to `Start`; Hierarchy art stays editor-owned.

---

## RR-004 Ocean Free Pond and track pools

E-03 / P1 / Done / 2026-06-04

Free Pond uses separate `tutorialTracks` and `gameplayTracks`; fish pick songs by meter.

Done: pond play works; track logic lives in the manager.

---

## RR-005 Ocean Bucket album UI

E-03 / P1 / Done / 2026-06-11

Bucket album paging and decoration drag match the scene hierarchy contract.

Done: page flip and drag work without blocking play; bakers only fill missing nodes.

---

## RR-006 Vertical tutorial and game in one scene

E-04 / P1 / Done / 2026-06-05

`VerticalRunner.unity` runs tutorial then formal mode without loading `Game`.

Done: tutorial completion rebuilds route and resets score in the same scene.

---

## RR-007 VerticalBeatSpawner route generation

E-04 / P1 / Done / 2026-06-04

Platforms, bananas, and parrot branches spawn from beat rules; tutorial and game builders.

Done: rebuilding from settings keeps jump spacing and branch timing correct.

---

## RR-008 Vertical miss counter instead of heart death

E-04 / P1 / Done / 2026-06-05

Formal mode tracks misses instead of heart-based death; HUD shows score, bananas, combo, misses.

Done: miss flow is separate from tutorial heart feedback.

---

## RR-009 Advanced single scene and chart table

E-05 / P1 / Done / 2026-06-05

Tutorial and game share `AdvancedRunner.unity`; charts come from the table.

Done: full run works; targets spawn from templates.

---

## RR-010 Advanced dual beat clock with visual delay

E-05 / P1 / Done / 2026-06-07

Judgment beat clock is separate from `visualBeatDelaySeconds`; prompt column follows visual timing.

Done: changing delay moves prompts only; judgment still follows the music.

---

## RR-011 Four-column rhythm prompts

E-01 / P1 / Done / 2026-06-10

Hierarchy-owned `ControlRhythmPrompt` columns on Vertical and Advanced; shows key/hand cues per beat slot.

Done: prompts visible during gameplay; gameplay judgment unchanged. (An earlier Settings toggle was removed — prompts stay on.)

---

## RR-012 Vertical/Advanced Inspector tuning

E-04/E-05 / P1 / Done / 2026-06-11

`perfectBeatFraction`, `goodBeatFraction`, window seconds, etc. live in settings and scenes.

Done: Inspector changes affect feel without hard-coded edits.

---

## RR-013 EditMode scene contract tests

E-06 / P1 / Done / 2026-06-11

`Assets/Tests/EditMode` checks Build Settings and key hierarchy paths.

Done: scene contract tests pass in Test Runner; comments are bilingual.

---

## RR-014 Hierarchy Bakers

E-06 / P1 / Done / 2026-06-04

`Tools → Rhythm Runner` fills missing nodes without overwriting existing UI.

Done: per-scene bakers run; missing-only paths preserve designer layout.

---

## RR-015 Script folders and SCRIPT_REFERENCE

E-06 / P1 / Done / 2026-06-08

Scripts grouped by Core/UI/World; reference docs updated.

Done: SCRIPT_REFERENCE points to main loop entry points.

---

## RR-016 AGENTS.md and PROJECT_MEMORY workflow

E-06 / P1 / Done / 2026-06-04

Read memory before edits; append Change Log after code/scene changes.

Done: `AGENTS.md` and `PROJECT_MEMORY.md` document the workflow.

---

## RR-017 Ocean intro card and pond button layout

E-03 / P1 / Done / 2026-06-04

Ocean opens with an info card; Back, ?, Bucket, TAP positions are set on the pond.

Done: layout lives in scene YAML; scripts only bind events.

---

## RR-018 Vertical scrolling background seam fix

E-04 / P1 / Done / 2026-06-04

`VerticalScrollingBackground` tiles align to the camera; no seam on scene start.

Done: Play mode shows a continuous background; tile height matches the camera.

---

## RR-019 Advanced world layer and target templates

E-05 / P1 / Done / 2026-06-06

`AdvancedRunnerRuntime` world nodes and target templates are editable in the scene.

Done: template/prefab edits affect spawns; no missing refs.

---

## RR-020 Asset license summary

E-06 / P1 / Done / 2026-06-11

`ASSET_LICENSES.md` covers Kenney CC0, music statement, and pending folders.

Done: each listed Assets folder has a source or owner-drawn note.

---

## RR-030 GDD and file map

E-06 / P1 / Done / 2026-06-11

`Docs/GAME_DESIGN_DOCUMENT.md` and `PROJECT_FILE_MAP.md` complete; indexed from `Docs/README.md`.

Done: required sections present for course submission format.

---

## RR-031 Vertical Back/Retry in tutorial

E-04 / P1 / Done / 2026-06-11

`GameControls` (Back, Retry) show after the listening countdown in tutorial mode too.

Done: tutorial Play shows usable bottom controls; merged in `VerticalRunnerManager`.

---

## RR-032 Vertical tutorial images in game mode

E-04 / P1 / Done / 2026-06-11

Formal climb keeps `TutorialImages` cycling by song progress instead of hiding them.

Done: six images rotate during game mode; Play path verified.

---

## RR-033 Windows submission build settings

E-06 / P0 / Done / 2026-06-11

Player Settings and Build Settings produce a Windows standalone for submission.

Done: local build reaches `Start` and all four modes; resolution and size reasonable.

---

## RR-034 License folder gaps in ASSET_LICENSES

E-06 / P1 / Done / 2026-06-11

Remaining “needs confirmation” folders in `ASSET_LICENSES.md` researched or labeled.

Done: each flagged folder has a source link or owner-made note.

---

## RR-035 Five-minute demo script

E-06 / P1 / Done / 2026-06-10

Five-minute demo script: Start through each mode with talk track.

Done: script fits ~5 minutes for screen recording.

---

## RR-036 Font license check

E-06 / P1 / Done / 2026-06-11

Fonts under `Assets/inks` and keyboard assets traced in the license doc.

Done: used fonts listed in `ASSET_LICENSES.md`; no unknown TTFs.

---

## RR-037 EditMode tests via batchmode or Test Runner

E-06 / P1 / Done / 2026-06-11

EditMode tests run via Unity Test Runner; batchmode blocked when Editor is open.

Done: manual Test Runner run documented; scene contracts green.

---

## RR-038 Vertical Retry skips briefing (optional)

E-04 / P2 / Done / 2026-06-10

Optional UX: Retry may skip briefing; kept current flow after playtest.

Done: documented as keep-current; no regression on Retry path.

---

## RR-039 Start Records after Vertical run

E-01 / P1 / Done / 2026-06-11

After a formal Vertical run, `Start` Records shows a sensible Easy row.

Done: one Vertical game → Records shows the new score.

---

## RR-040 GitHub Kanban board

E-06 / P1 / Done / 2026-06-11

GitHub Project and Issues aligned with local BOARD; labels and bootstrap script in repo.

Done: RR issues searchable; README documents setup.

---

## RR-042 Start card text replaces placeholder

E-01 / P2 / Done / 2026-06-11

World Music Explorer card on `Start` uses real copy, not placeholder text.

Done: card text matches the mode; no new Console errors.

---

## RR-043 Second song per meter in gameplayTracks

E-03 / P2 / Done / 2026-06-11

Each meter in `gameplayTracks` has a second song for Free Pond rotation.

Done: same-meter fish can switch clips without breaking pond UI.

---

## RR-044 Advanced optional tutorial path

E-05 / P2 / Done / 2026-06-11

Advanced offers a full tutorial path instead of only Skip.

Done: new players can follow tutorial; veterans can skip.

---

## RR-045 Vertical key rebinding

E-04 / P2 / Done / 2026-06-11

Space, Down, and arrow keys configurable (Settings or in-run menu).

Done: rebound keys still follow beat rules for jump, banana, parrot.

---

## RR-046 UI string pass

E-06 / P2 / Done / 2026-06-11

UI English strings reviewed across Start and all four modes.

Done: short child-friendly tone; no awkward or oversized labels.

---

## RR-047 Baker EnsureMissing-only for ObjectivePanel

E-06 / P1 / Done / 2026-06-11

Vertical baker uses EnsureMissing-only for `ObjectivePanel` tutorial images.

Done: baker run does not overwrite designer Objective layout.

---

## RR-048 Singing Shell difficulty tuning

E-03 / P2 / Done / 2026-06-11

Singing Shell hit targets tuned after playtest vs Free Pond difficulty.

Done: Shell difficulty fits the main pond curve; playtest notes recorded.
