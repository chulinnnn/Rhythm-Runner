# Testing and Bug-Fixing Log

Testing record for **Rhythm Playground**.

## 1. Manual smoke tests

Run from **`Start.unity`**. **Last full pass:** 2026-06-16

| # | Test | Expected | Pass |
|---|------|----------|------|
| 1 | Start → Global Music → any key switches track → Back | Music and visuals change; return to menu | ☑ |
| 2 | Start → Ocean Rhythm → select fish → preview → Space on beat | Perfect/Good advances capture; bucket updates | ☑ |
| 3 | Start → Climbing Monkey → tutorial → main level | 8-beat countdown; miss/score HUD; Retry/Back | ☑ |
| 4 | Start → Dropping Ink → Start Game → play chart | Targets fall; one miss ends run; score on result | ☑ |
| 5 | Build Settings scenes all load | No missing scene errors | ☑ |
| 6 | Standalone build (Windows) | Same four flows as Editor | ☑ |

Screenshots: [screenshots/](./screenshots/)

## 2. Automated tests (EditMode)

**Location:** `Assets/Tests/EditMode/SceneContractEditModeTests.cs`

**Run:** Unity → Window → General → Test Runner → EditMode → Run All

| Test area | What it checks |
|-----------|----------------|
| Build Settings | Required scenes registered |
| Start / Ocean / Vertical / Advanced / WorldMusic | Key Hierarchy paths exist |
| ControlRhythmPrompt | UI images do not block raycasts |

**Last run:** 2026-06-11 — screenshot: [`screenshots/2026-06-11_editmode-tests.png`](./screenshots/2026-06-11_editmode-tests.png)

Unity Test Runner is the authoritative automated check on this project (not `dotnet build`).

## 3. Bug fixes (sample)

| Bug | Symptom | Fix | Reference |
|-----|---------|-----|-----------|
| BucketAlbum click-through | Clicks hit pond fish under album | Overlay check; fish `raycastTarget=false` | Jun 10 OceanRhythmUIController |
| Advanced Retry stuck | Retry did not restart game | `BeginGame()` clears `runEnded` | Jun 8 AdvancedRunner |
| Advanced background over chart | Gameplay lines hidden | World-space SpriteRenderer sorting | Jun 6 AdvancedRunner |
| Advanced clock drift | Targets and HUD out of sync | Single `AudioSource.time` clock | Jun 7–8 AdvancedRunner |
| Vertical prompt flicker | Wrong key changed prompt | Prompt is demo-only | Jun 10 VerticalRunner |
| Ocean beat too soon after select | Hard to catch rhythm | Listen-first preview | Jun 4–5 OceanRhythm |
| Ocean UI node loss | Missing overlays after refactor | Partial restore + EditMode tests | Jun 11 |

More: [`PROJECT_MEMORY.md`](../PROJECT_MEMORY.md).

## 4. Playtests

Friends and one child playtest (7 y/o, consent) — see [`FEEDBACK.md`](FEEDBACK.md).

## 5. Out of scope for submission

PlayMode automated suite not implemented; acceptable for this module given EditMode + manual smoke tests.
