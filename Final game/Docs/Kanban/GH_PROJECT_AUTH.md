# GitHub `gh` — Project permissions

You need **`project`** and **`read:project`** on your `gh` token to create a Project board, add issues, and set Status (e.g. Done).

## Method 1: `gh auth refresh` (easiest)

In PowerShell:

```powershell
gh auth refresh -h github.com -s project,read:project
```

1. Copy the **one-time code** from the terminal (e.g. `ABCD-1234`).
2. Open https://github.com/login/device within **15 minutes**.
3. Paste the code and approve access.

Verify:

```powershell
gh auth status
```

Under `Token scopes` you should see `project` and `read:project` (plus `repo`, etc.).

Then:

```powershell
cd "Final game\scripts"
.\bootstrap-github-kanban.ps1 -SetupProjectOnly
.\bootstrap-github-kanban.ps1
```

## Method 2: Personal Access Token (for Actions or if refresh fails)

1. GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)** or fine-grained.
2. Create a token with:
   - **Classic**: scope **`project`** (full control of projects) or at least read + write for projects.
   - **Fine-grained**: Repository access `Rhythm-Runner` + Account permissions → **Projects: Read and write**.
3. **For local `gh`**:

```powershell
gh auth login -h github.com
# Choose GitHub.com → Paste an authentication token → paste the PAT
```

4. **For GitHub Actions**: repo **Settings** → **Secrets and variables** → **Actions** → New secret:
   - Name: `PROJECT_SETUP_TOKEN`
   - Value: the PAT

Then run workflow **Setup Kanban project** from the Actions tab.

## What each scope does

| Scope | Purpose |
|-------|---------|
| `repo` | Create/edit/close Issues, labels, milestones |
| `read:project` | List projects, read Status field options |
| `project` | Create project, link repo, add items, set Status |

`GITHUB_TOKEN` in Actions has **`repo`** but **not** user Project v2 APIs — use `PROJECT_SETUP_TOKEN` for board automation.

## Troubleshooting

| Error | Fix |
|-------|-----|
| `missing scopes [read:project]` | Run `gh auth refresh` again; complete device flow before code expires |
| `device_code has expired` | Code unused for 15 min — run refresh again |
| Workflow fails on “Check token” | Add `PROJECT_SETUP_TOKEN` secret |
| Status column missing | In Project settings, add Backlog / Ready / Review / Done / Blocked or rename Todo → Backlog |
