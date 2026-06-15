# Rhythm Runner Kanban

日常在 GitHub Project 上拖卡；`BOARD.md` 周会或离线时改一眼。

## GitHub

- Issues（RR 号搜）：https://github.com/chulinnnn/Rhythm-Runner/issues?q=label%3Akanban
- Project 看板：推完代码后去 Actions 跑 **Setup Kanban project**，跑完终端里会印 Project URL；或本地有 `project` 权限时：

```powershell
cd "Final game\scripts"
.\bootstrap-github-kanban.ps1 -SetupProjectOnly
.\bootstrap-github-kanban.ps1
```

本地 `gh` 若缺 `read:project`，先：

```powershell
gh auth refresh -h github.com -s project,read:project
```

Project 的 Status 列建议改成：`Backlog`、`Ready`、`In Progress`、`Review`、`Done`、`Blocked`（和 BOARD 一致；默认 Todo 可当 Backlog 用）。

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
