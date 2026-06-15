# Project File Architecture Map

High-level map of the `Final game` Unity project. Use with `GAME_DESIGN_DOCUMENT.md` Section 11–13.

## Scenes (Build Settings)

| Scene | Role |
|-------|------|
| `Assets/Scenes/Start.unity` | Main menu, mode selection, settings |
| `Assets/Scenes/WorldMusicExplorer.unity` | Ages 0–5: listen + image, any key switches track |
| `Assets/Scenes/OceanRhythm.unity` | Ages 5–10: Little Rhythm Ocean, Free Pond, Bucket |
| `Assets/Scenes/VerticalRunner.unity` | Ages 5–10 / vertical climb: tutorial + game in one scene |
| `Assets/Scenes/AdvancedRunner.unity` | Ages 10–15+ / challenge: multi-lane rhythm runner |

## Scripts (`Assets/Scripts/`)

| Folder | Responsibility |
|--------|----------------|
| `Start/` | Menu, settings, scene transition, title visuals |
| `WorldMusicExplorer/` | World music explorer controller |
| `OceanRhythm/` | Manager, UI, pond animals, bucket album |
| `VerticalRunner/` | Core, Player, World (spawner), UI |
| `AdvancedRunner/` | Advanced runner (single large implementation file + bridges) |
| `Shared/` | RhythmManager, scene policy, navigation, leaderboard |

## Editor (`Assets/Editor/`)

| Folder | Responsibility |
|--------|----------------|
| `HierarchyBakers/` | Scene hierarchy contract repair (`Tools → Rhythm Runner`) |

## Tests (`Assets/Tests/`)

| Folder | Responsibility |
|--------|----------------|
| `EditMode/` | Scene contract tests (Build Settings, hierarchy paths) |

## Key art / audio roots

| Path | Typical use |
|------|-------------|
| `Assets/Scenes/*.unity` | Hierarchy-owned UI and templates |
| `Assets/vertical/` | Vertical runner visuals |
| `Assets/Background/` | Backgrounds, scrolling vertical bg |
| `Assets/fishes/`, `Assets/mushroom/` | Ocean / platform art (Kenney CC0) |
| `Assets/globalmusic/`, `Assets/music/`, etc. | Music pools per mode |
| `Assets/Editor/button/` | UI pack (Kenney CC0) |

## Archive (not in build)

| Path | Role |
|------|------|
| `../BunnyLegacyArchive/` | Legacy horizontal bunny runner (isolated) |
