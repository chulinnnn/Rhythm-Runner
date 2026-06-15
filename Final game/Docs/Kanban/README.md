# Rhythm Runner Kanban

Day-to-day: drag cards on the GitHub Project. `BOARD.md` is a snapshot for stand-up or offline review.

## GitHub

- **Project board:** [Rhythm Runner M8](https://github.com/users/chulinnnn/projects/3) (user project #3)
- **Issues (`label:kanban`):** https://github.com/chulinnnn/Rhythm-Runner/issues?q=label%3Akanban
- **Planning snapshot (offline):** [BOARD.md](./BOARD.md) · [CARDS.md](./CARDS.md)

If Issues list is empty, run bootstrap (below) or see [`MANUAL_CHECKLIST.md`](../../MANUAL_CHECKLIST.md).

### Project board setup

**Option A — local `gh` (recommended)**

1. Grant Project scope (see [GH_PROJECT_AUTH.md](./GH_PROJECT_AUTH.md)).
2. Run:

```powershell
cd "Final game\scripts"
.\bootstrap-github-kanban.ps1 -SetupProjectOnly
.\bootstrap-github-kanban.ps1
```

`Docs/Kanban/kanban-config.json` will store the project number and URL.

**Option B — web UI**

[Your Projects](https://github.com/users/chulinnnn/projects) → New project → Board → title `Rhythm Runner M8` → link `Rhythm-Runner` → Add items with `label:kanban` → set Status columns to match BOARD.

**Actions workflow** `Setup Kanban project` needs repo secret `PROJECT_SETUP_TOKEN` (PAT with `project` scope). `GITHUB_TOKEN` cannot create user Projects v2.

## Local files

| File | Use |
|------|-----|
| [BOARD.md](./BOARD.md) | Column snapshot |
| [CARDS.md](./CARDS.md) | What / done notes per card |
| [CHARTER.md](./CHARTER.md) | WIP limits and definitions |
| [GH_PROJECT_AUTH.md](./GH_PROJECT_AUTH.md) | How to add `project` scope to `gh` |
| [kanban-config.json](./kanban-config.json) | Project id from bootstrap (after setup) |

New work: add a section in `CARDS.md`, then open an Issue (template or script).

## Card IDs

`RR-###`. Epics use labels `E-01-shared` … `E-06-qa`.

Release target: **M8 Submission build** (GDD §15).
