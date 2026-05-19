# Rhythm Runner

本仓库是游戏开发课程相关作品的**总集**：从课程初期的玩法设想，到随堂完成的 2D 射击项目，再到课堂活动记录，以及最终独立项目 **Rhythm Runner（节奏跑酷）**。

**远程仓库：** [github.com/chulinnnn/Rhythm-Runner](https://github.com/chulinnnn/Rhythm-Runner)

---

## 仓库结构

| 目录 | 说明 |
|------|------|
| [`2Dinclass/`](2Dinclass/) | 随堂跟做的 **Unity 2D 飞机射击** 项目（课堂主线练习） |
| [`prototype/`](prototype/) | 课程**最初**的游戏设想与原型素材 |
| [`inclass activity/`](inclass%20activity/) | 全学期**课堂活动**记录（作业、讨论、输出文档等） |
| [`Final game/`](Final%20game/) | **最终项目**目录（Rhythm Runner 可玩版本将放在此处） |

---

## 2Dinclass — 课堂 2D 飞机游戏

Unity **2022.3 LTS** 2D 项目，随课程进度实现射击、敌人、UI、分数与难度递进等系统。

- **打开方式：** 用 Unity Hub 添加并打开 `2Dinclass/` 文件夹。
- **主场景：** `Assets/Scenes/MainMenu.unity`、`Level1.unity`、`Level2.unity`
- **详细说明：** 见 [`2Dinclass/README.md`](2Dinclass/README.md)（含各次课堂玩法更新记录）。

课程后期在 `2Dinclass/` 中还保留了最终项目的设计文档，便于对照实现：

- [`2Dinclass/Rhythm Runner Design.md`](2Dinclass/Rhythm%20Runner%20Design.md) — Rhythm Runner 完整设计说明（页面、节拍、难度、Blind 模式等）

---

## prototype — 早期游戏设想

课程开始阶段的玩法与关卡构思，尚未进入完整 Unity 工程阶段。

当前内容示例：

- `prototype/escaping/` — 「逃脱」类玩法相关草图（如 `map-overview.jpg`、`room-detail.jpg`）

后续若新增其他原型分支，可在此目录下按主题分子文件夹存放。

---

## inclass activity — 课堂活动记录

按**上课日期**整理的文档与产出，用于归档老师布置的活动、小组材料与要求输出。

当前结构示例：

```
inclass activity/
└── 5.19/                    # 例如 5 月 19 日课堂
    ├── activity/            # 课堂活动相关文件（个人/小组提交等）
    └── output/              # 要求提交的最终产出
```

文件格式可能包含 `.docx`、`.pdf` 等，请在对应日期文件夹内查看。

---

## Final game — 最终项目（Rhythm Runner）

**Rhythm Runner** 为本仓库命名的核心作品：2D 横版**音乐节奏跑酷**——自动向右奔跑，按节拍跳跃/蹲伏/加速，点击道具得分，多页面 UI，支持难度与 Blind 模式等（详见设计文档）。

- **设计文档：** [`2Dinclass/Rhythm Runner Design.md`](2Dinclass/Rhythm%20Runner%20Design.md)
- **工程位置：** 可玩 Unity 项目将置于 `Final game/`（与课堂射击项目 `2Dinclass/` 分离，便于分别维护与提交）。

> `Final game/` 目录用于存放最终可构建、可运行的独立工程；若尚未迁入，以该目录下实际文件为准。

---

## 环境要求

| 项目 | 建议版本 |
|------|----------|
| Unity | 2022.3.x LTS（`2Dinclass` 当前为 **2022.3.62f3c1**） |
| 模板 | 2D Core |

克隆仓库后，请勿提交 `Library/`、`Temp/` 等 Unity 生成目录（`2Dinclass/.gitignore` 已配置忽略规则）。

---

## 快速开始

```bash
git clone git@github.com:chulinnnn/Rhythm-Runner.git
cd Rhythm-Runner
```

1. **课堂射击游戏：** Unity Hub → Open → 选择 `2Dinclass/`
2. **最终项目：** Unity Hub → Open → 选择 `Final game/`（工程就绪后）
3. **课堂记录：** 直接浏览 `inclass activity/` 下对应日期文件夹

---

## 相关链接

- GitHub：[chulinnnn/Rhythm-Runner](https://github.com/chulinnnn/Rhythm-Runner)
- 课堂项目说明：[2Dinclass/README.md](2Dinclass/README.md)
- 最终游戏设计：[2Dinclass/Rhythm Runner Design.md](2Dinclass/Rhythm%20Runner%20Design.md)
