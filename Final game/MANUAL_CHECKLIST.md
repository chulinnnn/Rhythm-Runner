# Manual Checklist — Author Actions

Items **you** must complete that cannot be fully automated. All notes should be **English** when added to repo docs.

## Required before final mark

- [ ] **Delete** `Assets/globalmusic/mail5.exe` — do **not** commit (security)
- [ ] **Screenshots** — add 6–10 PNG/JPG to [`Docs/screenshots/`](Docs/screenshots/) (see that README)
- [ ] **Unity Test Runner** — EditMode → Run All → screenshot pass → save as `Docs/screenshots/2026-06-11_editmode-tests.png`
- [ ] **Verify GitHub Project** — open [Rhythm Runner M8](https://github.com/users/chulinnnn/projects); confirm Issues/cards in Done (see Kanban README if bootstrap ran)
- [ ] **Font / asset licences** — verify or replace fonts in `Assets/inks`, `Assets/keyboard`; update [`ASSET_LICENSES.md`](Assets/AssetLicenses/ASSET_LICENSES.md) and [`Docs/CREDITS.md`](Docs/CREDITS.md)
- [ ] **Fill name** in [`Docs/CREDITS.md`](Docs/CREDITS.md) _(your name)_ placeholder
- [ ] **Submission form** — GitHub URL, final commit hash from [`Docs/COMMITS.md`](Docs/COMMITS.md), paste AI/assets summary from [`Docs/AI_USAGE.md`](Docs/AI_USAGE.md)

## Optional but recommended

- [ ] Add teacher/class feedback rows to [`Docs/FEEDBACK.md`](Docs/FEEDBACK.md)
- [ ] Upload dated files to `inclass activity/` with English `SUMMARY.md` per folder
- [ ] Screenshot GitHub Project board → `Docs/screenshots/github-project-board.png`
- [ ] 30–60s screen recording of four-mode menu flow (link in TESTING or demo txt)
- [ ] Second git commit after screenshots: `docs: add progress screenshots`

## gh CLI (if Issues empty)

```powershell
cd "Final game\scripts"
gh auth refresh -s project,read:project
.\bootstrap-github-kanban.ps1
```

See [`Docs/Kanban/GH_PROJECT_AUTH.md`](Docs/Kanban/GH_PROJECT_AUTH.md).

## After agent push

- [ ] Pull latest `main`
- [ ] Confirm root README and evidence index render on GitHub
- [ ] Run standalone build smoke test; tick table in [`Docs/TESTING.md`](Docs/TESTING.md)
