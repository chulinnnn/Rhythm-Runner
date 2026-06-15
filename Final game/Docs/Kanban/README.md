# Rhythm Runner Kanban

日常在 GitHub Project 上拖卡；`BOARD.md` 周会或离线时改一眼。

## GitHub

- Issues（RR 号搜）：https://github.com/chulinnnn/Rhythm-Runner/issues?q=label%3Akanban
Project 看板（二选一）：

1. **本地 gh**（推荐，拖卡 + 脚本自动摆列）  
   ```powershell
   gh auth refresh -h github.com -s project,read:project
   cd "Final game\scripts"
   .\bootstrap-github-kanban.ps1 -SetupProjectOnly
   .\bootstrap-github-kanban.ps1
   ```  
   跑完 `Docs/Kanban/kanban-config.json` 里会有 project 号和 URL。

2. **网页手动**  
   [你的 Projects](https://github.com/users/chulinnnn/projects) → New project → Board → 标题 `Rhythm Runner M8` → 关联本仓库 → Add items 搜 `label:kanban` → Status 列改成和 BOARD 一致（可把 Todo 当 Backlog）。

Actions 里的 **Setup Kanban project** 需要仓库 Secret `PROJECT_SETUP_TOKEN`（PAT 带 `project` 权限），`GITHUB_TOKEN` 建不了用户 Project。

## 本地文件

| 文件 | 用途 |
|------|------|
| [BOARD.md](./BOARD.md) | 列快照，站会扫一眼 |
| [CARDS.md](./CARDS.md) | 每张卡写清楚要做啥、咋算做完 |
| [CHARTER.md](./CHARTER.md) | WIP、Done 约定 |
| [kanban-config.json](./kanban-config.json) | 脚本记的 project 号（生成后出现） |

新卡：先在 `CARDS.md` 加一段，再开 Issue 或网页新建（会自动进 Backlog，见 workflow）。

## 卡片号

`RR-###`，Epic 用标签 `E-01-shared` … `E-06-qa`。

当前目标：**M8 Submission build**（GDD §15），重点是试玩收尾、许可证、演示稿。
