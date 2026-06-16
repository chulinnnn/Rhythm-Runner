# Known Limitations and Mitigation

Honest scope boundaries for **Rhythm Playground** submission.

## Design and content

| Limitation | Mitigation |
|------------|------------|
| **Global Music** has fewer audio-visual sets than Ocean / Monkey / Ink | Listed as could-have in GDD MoSCoW; core stability rests on the other three modes |
| **No localization** | Short English UI; beat teaching via visuals and cards |
| **No in-game chart editor** | Charts built in code/data tables |

## Input and accessibility

| Limitation | Mitigation |
|------------|------------|
| **Keyboard only** | Documented in GDD §14; on-screen key/hand prompts |
| **Colour-blind beat markers** | Could-have; wider lanes and motion cues implemented |
| **Mute play** | Visual beat lanes and prompts |

## Technical

| Limitation | Mitigation |
|------------|------------|
| **No PlayMode automated test suite** | EditMode scene contract tests; manual smoke checklist in [`TESTING.md`](TESTING.md) |
| **`dotnet build` csproj fails** on my dev PC | Use Unity Editor / Test Runner instead |
| **Unity batchmode blocked** when Editor is open | Manual Play verification + EditMode tests |
| **Jun 9 git history rewrite** | Oversized audio removed; baseline documented in [`COMMITS.md`](COMMITS.md) |

## Legal and assets

| Limitation | Mitigation |
|------------|------------|
| **Course submission only** — CC0 declarations are for this module, not a commercial release audit | Full list in [`ASSET_LICENSES.md`](../Assets/AssetLicenses/ASSET_LICENSES.md) |
| ChatGPT-generated visuals | Disclosed in [`AI_USAGE.md`](AI_USAGE.md); edited before use |

## Networking and security

| Limitation | Mitigation |
|------------|------------|
| **Offline only** | No accounts or network calls in gameplay; local leaderboard only |

## Cut-first features (not in submission)

Extra Advanced lanes, online accounts, long cinematics, procedural music, legacy bunny restoration — see GDD §10.
