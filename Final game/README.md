# Rhythm Playground (Capstone)

**Rhythm Playground** is a multi-mode rhythm-learning game for ages 0–15 and music beginners. The Unity project folder is `Final game`; the repository name remains Rhythm-Runner.

## Four modes

| Mode | Scene | Age band (design intent) |
|------|-------|--------------------------|
| **Global Music** | `WorldMusicExplorer` | 0–5 — listen and watch; any key switches tracks |
| **Ocean Rhythm** | `OceanRhythm` | 5–10 — catch fish on the beat; bucket collection |
| **Climbing Monkey** | `VerticalRunner` | 10–15 — vertical rhythm climb; bananas and parrots |
| **Dropping Ink (challenge)** | `AdvancedRunner` | 15+ — three-lane falling targets; one-miss scoring |

Entry scene: **`Assets/Scenes/Start.unity`**. Each mode card loads its scene; Back returns to Start.

## How to open

1. Unity Hub → **Open** → select this `Final game/` folder.
2. Open **`Start.unity`** and press Play.
3. Build Settings must include Start, OceanRhythm, VerticalRunner, AdvancedRunner, and WorldMusicExplorer (already registered).

Recommended: **Unity 2022.3.x LTS**, 2D Core template.

Windows standalone build output lives in `Build/windows/` locally; that folder is gitignored and is **not** on GitHub. Submit the course build zip separately.

## Controls (summary)

| Mode | Input |
|------|--------|
| Global Music | Any ordinary key → next track; Back → menu |
| Ocean Rhythm | Click fish → preview → **Space** on beat; mouse for pond UI |
| Climbing Monkey | **Space** jump; **Down/S** banana; **Space + Left/Right** parrot branches |
| Dropping Ink | **Space** jump; **Down** slide; **Left/Right** lanes |

## Legacy code

The early horizontal bunny runner was archived to [`../BunnyLegacyArchive`](../BunnyLegacyArchive) and is not imported by this project.

## Documentation

- **Documentation index:** [`Docs/README.md`](Docs/README.md)
- **Game design document:** [`Docs/GAME_DESIGN_DOCUMENT.md`](Docs/GAME_DESIGN_DOCUMENT.md)
- **Development log:** [`Docs/DEVELOPMENT_LOG.md`](Docs/DEVELOPMENT_LOG.md)
- **Asset licenses:** [`Assets/AssetLicenses/ASSET_LICENSES.md`](Assets/AssetLicenses/ASSET_LICENSES.md)
- **AI usage:** [`Docs/AI_USAGE.md`](Docs/AI_USAGE.md)

Monorepo overview: [`../README.md`](../README.md).
