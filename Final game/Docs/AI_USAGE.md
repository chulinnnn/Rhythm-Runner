# AI and External Resources

Declaration for **Rhythm Playground** (`Final game`). I used AI only as a helper; design, beat rules, playtest changes, and final build approval are mine. Everything listed below was checked in Unity before I kept it.

---

## External resources (course template)

### Kenney.nl asset packs (CC0)

| Field | Detail |
|-------|--------|
| **Name** | Kenney UI Pack 2.0, Fish Pack 2.0, Background Elements, Animal Pack, Emotes, Mushroom Land, Cursor Pixel Pack |
| **Type** | asset |
| **Source** | https://kenney.nl |
| **Licence** | CC0 1.0 — `Assets/*/License.txt` |
| **What it provided** | UI buttons, fish sprites, backgrounds, icons, platform tiles, cursors |
| **Used unchanged** | Most Kenney sprites and UI sounds as imported |
| **Modified** | Recoloured or scaled in Unity Inspector for Start, Ocean, Vertical, Advanced scenes |
| **Created myself** | Scene layout, mode routing, beat gameplay on top of art |
| **Where in game** | Start menu, OceanRhythm pond/fish, shared UI, mushroom/background elements |
| **How credited** | [`ASSET_LICENSES.md`](../Assets/AssetLicenses/ASSET_LICENSES.md), [`CREDITS.md`](CREDITS.md) |

### Project music and original art

| Field | Detail |
|-------|--------|
| **Name** | Mode BGM folders + hand-made / photographed backgrounds |
| **Type** | audio / image |
| **Source** | Author-owned files under `Assets/globalmusic`, `ocean_music`, `monkeymusic`, `inkmusic`, etc. |
| **Licence** | CC0 declared by author (course submission) |
| **What it provided** | BGM per mode; some custom backgrounds |
| **Used unchanged** | Clips assigned to AudioSource in scenes |
| **Modified** | Trimmed levels in Unity where needed |
| **Created myself** | Selection, placement, BPM values in Inspector |
| **Where in game** | All four modes + Start menu music |
| **How credited** | ASSET_LICENSES, CREDITS |

### ChatGPT-generated visuals

| Field | Detail |
|-------|--------|
| **Name** | Selected UI / background concepts |
| **Type** | AI / image |
| **Source** | ChatGPT (image generation from my prompts) |
| **Licence** | Generated content; edited by author before use |
| **What it provided** | Starting ideas for some menu or background art |
| **Used unchanged** | None kept raw — all edited or rejected |
| **Modified** | Cropped, colour-adjusted, composited in Unity Hierarchy |
| **Created myself** | Prompts, selection, final placement |
| **Where in game** | Start and mode backgrounds (where applied) |
| **How credited** | This file + CREDITS |

### Unity / TextMesh Pro

| Field | Detail |
|-------|--------|
| **Name** | Unity 2022.3 LTS, TextMesh Pro, EmojiOne attribution |
| **Type** | template / engine |
| **Source** | Unity Hub install |
| **Licence** | Unity ToS; TMP third-party notices in Assets |
| **What it provided** | Engine, UI text, default project structure |
| **Used unchanged** | TMP components and default fonts where assigned |
| **Modified** | Custom hierarchy UI in each scene |
| **Created myself** | Gameplay scripts, scenes, beat systems |
| **Where in game** | Whole project |
| **How credited** | CREDITS |

---

## AI assistance (course template)

### Cursor AI, codex — code and debugging

| Field | Detail |
|-------|--------|
| **Tool used** | Cursor AI, codex |
| **What I asked** | Help fixing UI raycasts, rhythm timing bugs, hierarchy baker scripts, and refactoring script folders |
| **What output I used** | Partial method drafts and bug-fix suggestions — only after I traced the scene hierarchy myself |
| **What I changed** | Renamed variables to match my project, rewrote logic to match my beat rules, removed suggestions that broke playtests |
| **How I tested** | Play mode in Unity for each mode; EditMode tests for scene paths |
| **What I understand** | How my Vertical/Advanced beat clocks, spawners, and UI binding work because I stepped through them in the Editor |
| **What I still do not fully understand** | Some Unity YAML edge cases when merging scenes — I fixed these with small restores + tests rather than bulk auto-edits |
| **Where in project** | `Assets/Scripts/**`, `Assets/Editor/HierarchyBakers/**`, scene contract tests |

### ChatGPT — visuals and occasional Q&A

| Field | Detail |
|-------|--------|
| **Tool used** | ChatGPT |
| **What I asked** | Image ideas for backgrounds/UI; sometimes Unity API questions when stuck |
| **What output I used** | Images I manually approved; short answers I verified against Unity docs |
| **What I changed** | All images edited or replaced; code snippets rewritten to fit my class names |
| **How I tested** | Visual check in Game view; compile + Play |
| **What I understand** | Which assets are mine vs Kenney vs generated |
| **What I still do not fully understand** | N/A for submission scope |
| **Where in project** | Selected sprites/backgrounds; no auto-generated gameplay logic kept without review |

### Cursor / ChatGPT — English comments on scripts

| Field | Detail |
|-------|--------|
| **Tool used** | Cursor AI, ChatGPT |
| **What I asked** | Turn my Chinese notes into English XML summaries on existing scripts |
| **What output I used** | Comment text only — no gameplay logic |
| **What I changed** | Fixed wording where comments did not match behaviour after playtesting |
| **How I tested** | Read against actual method behaviour in Play mode |
| **What I understand** | My own code flow; comments are helpers for markers |
| **What I still do not fully understand** | N/A |
| **Where in project** | `OceanRhythmManager`, `VerticalRunner*`, `AdvancedRunner*`, EditMode tests |

### Cursor — EditMode test draft

| Field | Detail |
|-------|--------|
| **Tool used** | Cursor AI |
| **What I asked** | Draft NUnit tests for Build Settings and key Hierarchy paths |
| **What output I used** | Starting structure for `SceneContractEditModeTests.cs` |
| **What I changed** | Paths, assertions, and bilingual notes to match my saved scenes |
| **How I tested** | Unity Test Runner → EditMode → Run All |
| **What I understand** | Tests only check saved scene contracts, not full gameplay |
| **What I still do not fully understand** | PlayMode automation — not implemented in time |
| **Where in project** | `Assets/Tests/EditMode/` |

---

## What AI did not decide

Four-mode age split, MoSCoW cuts, Ocean Free Pond flow, Vertical beat-slot rules, Advanced one-miss scoring, asset licensing statements, and whether the build is submission-ready.

## Related

- [`CREDITS.md`](CREDITS.md)
- [`Assets/AssetLicenses/ASSET_LICENSES.md`](../Assets/AssetLicenses/ASSET_LICENSES.md)
