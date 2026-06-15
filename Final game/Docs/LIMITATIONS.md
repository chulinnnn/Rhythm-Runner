# Known Limitations and Mitigation

Honest scope boundaries for **Rhythm Playground** submission.

## Design and content

| Limitation | Mitigation |
|------------|------------|
| **Global Music** has fewer audio-visual sets than Ocean / Monkey / Ink | Listed as could-have in GDD MoSCoW; core vertical slice stability rests on other three modes |
| **No localization** | Short English UI; beat teaching via visuals and cards |
| **No in-game chart editor** | Cut-first; charts built in code/data tables |

## Input and accessibility

| Limitation | Mitigation |
|------------|------------|
| **Keyboard only** | Documented in GDD §14; prompts show keys and hands |
| **Colour-blind beat markers** | Could-have; wider lanes and strong motion cues implemented |
| **Mute play** | Visual beat lanes and prompts; Ocean/Advanced still rhythm-focused |

## Technical

| Limitation | Mitigation |
|------------|------------|
| **No PlayMode automated test suite** | EditMode scene contract tests; manual smoke checklist in [`TESTING.md`](TESTING.md) |
| **`dotnet build` csproj fails** on dev PC | Documented in `PROJECT_MEMORY`; use Unity Editor / Test Runner |
| **Unity batchmode blocked** when Editor open | Static YAML checks + manual Play verification |
| **Jun 9 git history rewrite** | Oversized audio removed professionally; playable hash documented in [`COMMITS.md`](COMMITS.md) and [`REFLECTION.md`](REFLECTION.md) |

## Legal and assets

| Limitation | Mitigation |
|------------|------------|
| Some font / folder licenses **not fully verified** (`Assets/inks`, `keyboard`, etc.) | Tracked in [`ASSET_LICENSES.md`](../Assets/AssetLicenses/ASSET_LICENSES.md); replace or confirm before commercial release |
| ChatGPT-generated visuals | Disclosed in [`AI_USAGE.md`](AI_USAGE.md) and [`CREDITS.md`](CREDITS.md); edited before use |

## Networking and security

| Limitation | Mitigation |
|------------|------------|
| **Offline only** — no cloud save or multiplayer | By design for child safety; leaderboard local only |
| No accounts or network calls in core gameplay | Documented in GDD §14 |

## Cut-first features (not in submission)

Extra Advanced lanes, online accounts, long cinematics, procedural music, legacy bunny restoration — see GDD §10.
