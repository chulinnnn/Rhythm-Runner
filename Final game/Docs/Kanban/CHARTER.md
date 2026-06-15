# Kanban charter

## Purpose

Track delivery of the four playable modes plus shared systems, without losing scene/UI contracts or license compliance.

## Board columns

| Column | Meaning | Entry rule |
|--------|---------|------------|
| **Backlog** | Agreed work, not scheduled | Card exists in CARDS.md |
| **Ready** | Can start today | DoR met; no unresolved dependency |
| **In Progress** | Someone is actively working | WIP limit respected |
| **Review** | Code/scene done; needs playtest or peer check | PR or local test notes attached |
| **Done** | Shipped to `main` / saved scene on agreed branch | DoD met |

## WIP limits

| Column | Limit |
|--------|-------|
| Ready | 8 |
| In Progress | **3** |
| Review | 4 |

If In Progress is full, finish or park work before pulling from Ready.

## Definition of Ready (DoR)

- Card ID and title in CARDS.md  
- Owner assigned (or `unassigned` with date)  
- Acceptance criteria listed (min. 2 bullets)  
- Known dependencies named (scene, script, asset)  
- Priority P0–P2 set  

## Definition of Done (DoD)

- Change runs in Unity Editor Play mode for affected scene(s)  
- No new Console errors in that path  
- `PROJECT_MEMORY.md` updated if code/scene/project settings changed  
- Scene YAML checked if Hierarchy paths touched (no missing script refs)  
- License impact noted if new external asset added  

EditMode tests: run when touching scene contracts; not required for pure doc changes.

## Ceremonies

| Ceremony | When | Output |
|----------|------|--------|
| Board refresh | Start of session | BOARD.md matches reality |
| Playtest pass | Before Review → Done for gameplay cards | 1-line result in card |
| Weekly scope check | Once per week | Backlog reprioritized; cut list updated |

## Labels

| Label | Meaning |
|-------|---------|
| `P0` | Blocks submission or breaks Start → mode flow |
| `P1` | Required for M8 vertical slice quality |
| `P2` | Polish / nice-to-have |
| `code` | Scripts |
| `scene` | `.unity` Hierarchy / Inspector |
| `art` | Sprites, audio, fonts |
| `doc` | GDD, licenses, Kanban |
| `test` | EditMode / playtest |
| `blocked` | Waiting on external input |

## Swimlanes (epics)

| Epic | Scope |
|------|--------|
| E-01 Shared | Start, RhythmManager, transitions, settings |
| E-02 WorldMusicExplorer | 0–5 mode |
| E-03 OceanRhythm | 5–10 pond + bucket |
| E-04 VerticalRunner | Climb mode |
| E-05 AdvancedRunner | Lane runner |
| E-06 QA & submission | Tests, build, docs, demo |
