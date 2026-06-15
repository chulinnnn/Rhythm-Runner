# Project Evolution

How **Rhythm Playground** changed from first concept to the submitted vertical slice.

## Phase 1 — Horizontal bunny rhythm runner (May 2026)

- **Concept:** Endless side-scroller; bunny runs right; jump on the beat.
- **Implementation:** Hand-placed prefabs along the route; barriers synced to rhythm incrementally.
- **Problem:** Authoring beat-aligned routes by placing prefabs manually was slow and error-prone.
- **Outcome:** Prototype proved rhythm judgment was fun but pipeline did not scale.

## Phase 2 — Template-driven generation (late May 2026)

- **Change:** Replaced hand-placed beat prefabs with **template cloning** and data-driven routes.
- **Vertical:** `VerticalBeatSpawner` builds platforms, bananas, parrot branches from step data.
- **Advanced:** Chart rows generate falling targets from lists/tables.
- **Outcome:** Designers tune data, not individual prefab positions.

## Phase 3 — Four-mode pivot (May 26–28, 2026)

- **Trigger:** Friend playtests found single-runner demo too shallow.
- **Design:** Split by age band using author’s music background:
  - **Global Music** — listen / watch (0–5)
  - **Ocean Rhythm** — meters + collection (5–10)
  - **Climbing Monkey** — story vertical climb (10–15)
  - **Dropping Ink (challenge)** — strict three-lane challenge (15+)
- **Outcome:** One Unity project, four scenes, shared beat concepts, different UX per band.

## Phase 4 — Scene consolidation (Jun 4–5, 2026)

- **Before:** Separate Tutorial + Game scenes for Vertical and Advanced.
- **After:** `VerticalRunner.unity` and `AdvancedRunner.unity` each run tutorial → main flow in one scene.
- **Legacy:** Old bunny content moved to `BunnyLegacyArchive/` (outside `Assets`).
- **Outcome:** Faster loads; less confusion for child players.

## Phase 5 — Playable vertical slice (Jun 9–11, 2026)

- **Git:** History rewritten once to drop oversized audio that blocked GitHub push (`61ab3ce`).
- **Features:** WorldMusicExplorer; four-column prompts; Ocean bucket album; EditMode scene tests; GDD in `Docs/`.
- **Milestone commit message:** `playable` (`0b44d94`).

## Phase 6 — Submission hardening (Jun 12–15, 2026)

- Code comments, Kanban/GitHub planning sync, asset license doc, submission evidence pack (this folder).
- **Still lighter than core modes:** Global Music content volume (could-have in MoSCoW).

## Design plan vs result

| Planned (early GDD) | Final build |
|---------------------|-------------|
| Single rhythm runner | Four modes by age |
| Hand-tuned levels | Data-driven routes/charts |
| Hearts / death (Vertical) | Miss count, continue playing |
| Hearts (Advanced) | One-miss score attack |
| Forced Ocean tutorial | Free Pond + optional cards |
| Global beat-assist toggle on Start | Per-mode built-in prompts/lanes |

See also [`DEVELOPMENT_LOG.md`](DEVELOPMENT_LOG.md) and [`FEEDBACK.md`](FEEDBACK.md).
