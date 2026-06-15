# Testing and Bug-Fixing Log

Testing evidence for **Rhythm Playground** capstone submission.

## 1. Manual smoke tests

Run from **`Start.unity`** after each major change. Last full pass: _pending — add date after you run before submission_.

| # | Test | Expected | Pass |
|---|------|----------|------|
| 1 | Start → Global Music → any key switches track → Back | Music and visuals change; return to menu | ☐ |
| 2 | Start → Ocean Rhythm → select fish → preview → Space on beat | Perfect/Good advances capture; bucket updates | ☐ |
| 3 | Start → Climbing Monkey → tutorial → main level | 8-beat countdown; miss/score HUD; Retry/Back | ☐ |
| 4 | Start → Dropping Ink → Start Game → play chart | Targets fall; one miss ends run; score on result | ☐ |
| 5 | Build Settings scenes all load | No missing scene errors | ☐ |
| 6 | Standalone build (Windows) | Same four flows as Editor | ☐ |

Screenshot checklist: [`screenshots/README.md`](screenshots/README.md).

## 2. Automated tests (EditMode)

**Location:** `Assets/Tests/EditMode/SceneContractEditModeTests.cs`

**Run:** Unity → Window → General → Test Runner → EditMode → Run All

| Test area | What it checks |
|-----------|----------------|
| Build Settings | Required scenes registered |
| Start / Ocean / Vertical / Advanced / WorldMusic | Key Hierarchy paths exist |
| ControlRhythmPrompt | UI images do not block raycasts |

**Last run result:** _pending — screenshot to `screenshots/2026-06-11_editmode-tests.png`_

Note: `dotnet build Assembly-CSharp.csproj` is not used on this machine (missing .NET Framework refs); Unity Test Runner is the authoritative automated check.

## 3. Bug fixes (sample)

| Bug | Symptom | Fix | Reference |
|-----|---------|-----|-----------|
| BucketAlbum click-through | Clicks hit pond fish under album | Overlay active check; fish `raycastTarget=false`; selective UI raycast | Jun 10 `OceanRhythmUIController` |
| Advanced Retry stuck | Retry did not restart game | `BeginGame()` clears `runEnded`, replays BGM | Jun 8 AdvancedRunner |
| Advanced background over chart | Gameplay lines hidden | World-space SpriteRenderer sorting | Jun 6 AdvancedRunner |
| Advanced clock drift | Targets and HUD out of sync | Single `AudioSource.time` gameplay clock | Jun 7–8 AdvancedRunner |
| Vertical prompt flicker | Wrong key changed prompt | Prompt is demonstration-only, not input-driven | Jun 10 VerticalRunner |
| Ocean beat too soon after select | Could not catch rhythm | Listen-first preview + `ResetBeatClock` | Jun 4–5 OceanRhythm |
| Scene node loss after UI refactor | Missing CompleteOverlay etc. | Partial restore from git; scene contract tests added | Jun 11 EditMode tests |

More entries: [`PROJECT_MEMORY.md`](../PROJECT_MEMORY.md) Change Log.

## 4. Peer and child playtests

- **Friends:** cold-start usability for ages 0–15 (see [`FEEDBACK.md`](FEEDBACK.md)).
- **Child (7 y/o, parental consent):** engagement and clarity spot-check.

## 5. Known open items

- Full PlayMode automated suite not implemented.
- Some verification entries in `PROJECT_MEMORY` note “Unity Play pending” when Editor was locked — re-run before final demo.
