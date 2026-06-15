# 卡片登记

和 [BOARD.md](./BOARD.md) 对表用。新卡先写这里，再拖 GitHub Project。

---

## RR-001 老 bunny 归档到 BunnyLegacyArchive

E-06 / P1 / Done / 2026-06-04

旧横版 bunny 的脚本、prefab、背景从 `Assets` 挪到 `../BunnyLegacyArchive`，Unity 别再编译那套。

做完：Assets 里搜不到 legacy bunny；Editor 能正常进 Start。

---

## RR-002 Start 四模式进场景

E-01 / P1 / Done / 2026-06-05

Start 上四个模式卡片分别进 Ocean、Vertical、Advanced、WorldMusicExplorer。

做完：点每张卡都能加载对应场景，Back 能回 Start。

---

## RR-003 WorldMusicExplorer 场景

E-02 / P1 / Done / 2026-06-11

新建 `WorldMusicExplorer` 场景和 `WorldMusicExplorerController`，键盘切换条目、播 AudioSource、切视觉层。

做完：进场景能换条目听歌，Back 回 Start；不碰 Hierarchy 里摆好的图。

---

## RR-004 Ocean Free Pond + 曲目库

E-03 / P1 / Done / 2026-06-04

Free Pond 用 `gameplayTracks` / `tutorialTracks` 分池，鱼按 meter 选歌。

做完：进 Pond 能选鱼开玩，换歌逻辑在 manager 里可读。

---

## RR-005 Ocean Bucket 相册 UI

E-03 / P1 / Done / 2026-06-11

Bucket 相册分页、装饰拖拽的 Hierarchy 契约和脚本绑好。

做完：翻页、拖装饰不挡玩法；缺节点时 baker 只补不盖。

---

## RR-006 Vertical 教程和正式合一场景

E-04 / P1 / Done / 2026-06-05

`VerticalRunner.unity` 教程打完不切 `Game`，同场景进正式模式。

做完：教程通关后路线重建、分数重置，无多余场景跳转。

---

## RR-007 VerticalBeatSpawner 路线生成

E-04 / P1 / Done / 2026-06-04

平台/香蕉/鹦鹉分支按 beat 规则生成，教程和正式两套 builder。

做完：改 settings 重建路线，跳拍和分支间隔符合设计。

---

## RR-008 Vertical 用 miss 计数不用爱心死

E-04 / P1 / Done / 2026-06-05

正式模式记 miss 而不是扣爱心死亡；HUD 显示分数、香蕉、combo、miss。

做完：连续失误走 miss 逻辑，和教程心形反馈分开。

---

## RR-009 Advanced 单场景 + 谱面表

E-05 / P1 / Done / 2026-06-05

Advanced 教程和正式同在 `AdvancedRunner.unity`，谱面走 chart table。

做完：进场景能完整跑一局，目标从模板 spawn。

---

## RR-010 Advanced 双节拍钟（视觉延迟）

E-05 / P1 / Done / 2026-06-07

判定节拍和 `visualBeatDelaySeconds` 分开，提示列跟视觉拍对齐。

做完：改 delay 只动提示，判定窗口仍跟音乐。

---

## RR-011 Start 节拍辅助开关

E-01 / P1 / Done / 2026-06-10

Settings 里 beat-assist 开关持久化，关的时候 Vertical/Advanced 藏四列 `ControlRhythmPrompt`。

做完：开关状态保存，runner 里提示显隐正确，玩法不受影响。

---

## RR-012 Vertical/Advanced Inspector 调参

E-04/E-05 / P1 / Done / 2026-06-11

`perfectBeatFraction`、`goodBeatFraction`、窗口秒数等进 settings，场景里能改。

做完：调 Inspector 能改变判定手感，不用改硬编码。

---

## RR-013 EditMode 场景契约测试

E-06 / P1 / Done / 2026-06-11

`Assets/Tests/EditMode` 检查 Build Settings 和关键 Hierarchy 路径。

做完：Test Runner 里场景契约测试绿；有双语注释。

---

## RR-014 Hierarchy Bakers

E-06 / P1 / Done / 2026-06-04

`Tools → Rhythm Runner` 菜单补缺失节点，不覆盖已有 UI。

做完：各场景 baker 能跑，缺啥补啥，不动设计师摆好的 Rect。

---

## RR-015 脚本目录 + SCRIPT_REFERENCE

E-06 / P1 / Done / 2026-06-08

按 Core/UI/World 分文件夹，参考文档跟上。

做完：新同学能从 SCRIPT_REFERENCE 找到主循环入口。

---

## RR-016 AGENTS.md + PROJECT_MEMORY 流程

E-06 / P1 / Done / 2026-06-04

改代码前先读 memory，改完写 Change Log。

做完：`AGENTS.md` 和 `PROJECT_MEMORY.md` 在仓库里，规则清楚。

---

## RR-017 Ocean 开场卡 + Pond 按钮布局

E-03 / P1 / Done / 2026-06-04

进 Ocean 先信息卡，Pond 上 Back/?/Bucket/TAP 位置定好。

做完：布局在场景 YAML 里可改，脚本只绑事件。

---

## RR-018 Vertical 滚动背景接缝

E-04 / P1 / Done / 2026-06-04

`VerticalScrollingBackground` 铺砖对齐相机，开场不见中间缝。

做完：Play 开头背景连续，tile 高度跟相机匹配。

---

## RR-019 Advanced 世界层 + 目标模板

E-05 / P1 / Done / 2026-06-06

`AdvancedRunnerRuntime` 世界节点和目标 template 在场景里可编辑。

做完：改模板 prefab/层级能影响 spawn，不缺引用。

---

## RR-020 资源许可证摘要

E-06 / P1 / Done / 2026-06-11

`ASSET_LICENSES.md` 汇总 Kenney CC0、音乐声明和待确认文件夹。

做完：文档列出已确认和还缺的 Assets 子目录。

---

## RR-030 GDD + 文件地图

E-06 / P1 / Review

`Docs/GAME_DESIGN_DOCUMENT.md` 和 `PROJECT_FILE_MAP.md` 写完，等课程格式检查。

做完：导师要的章节齐全；链接能从 `Docs/README.md` 找到。

---

## RR-031 Vertical 教程里显示 Back/Retry

E-04 / P1 / Review

教程倒计时结束后也要出 `GameControls`（Back、Retry）。

做完：教程模式 Play 一遍，底部按钮可见可用；代码已合，待实机确认。

---

## RR-032 Vertical 正式模式保留教程图

E-04 / P1 / Review

正式爬树时 `TutorialImages` 按进度轮播，不藏。

做完：正式局里六张图会换；待 Play 确认节奏是否合适。

---

## RR-033 Windows 提交包设置

E-06 / P0 / In Progress

Player Settings、Build Settings 打成可交的 Windows standalone。

做完：本机打出包能进 Start 并跑四个模式；体积和分辨率合理。

---

## RR-034 许可证缺口补全

E-06 / P1 / In Progress

`ASSET_LICENSES.md` 里还标「待确认」的文件夹逐个核实或标注来源。

做完：每个列出的文件夹有结论（来源链接或「自摄/自绘」）。

---

## RR-035 五分钟演示脚本

E-06 / P1 / Ready

写一份 5 分钟 demo 路线：Start → 四模式各展示啥、讲啥。

做完：照着稿子能录屏交作业，时间卡在五分钟左右。

---

## RR-036 字体许可证核对

E-06 / P1 / Ready

`Assets/inks`、keyboard 字体等查来源，写进许可证文档。

做完：用到的字体在 `ASSET_LICENSES.md` 有条目，无未知 TTF。

---

## RR-037 Batchmode 跑 EditMode 测试

E-06 / P1 / Blocked

想用 batchmode/CI 跑 EditMode；本机 Editor 常开着，政策也不鼓励 `dotnet build`。

做完：要么 CI 日志里有测试结果，要么文档写明用手动 Test Runner。当前卡在环境和 Editor 占用。

---

## RR-038 Vertical Retry 跳过 briefing（可选）

E-04 / P2 / Ready

Retry 后是否跳过开场 briefing，看试玩反馈再定。

做完：若做——Retry 路径和首次进入不一致且 UX 更顺；若不做——在卡上记「保持现状」。

---

## RR-039 Start Records 榜验证

E-01 / P1 / Ready

Vertical 正式局打完，Start Records 里 Easy 榜有行、分数合理。

做完：打一局 Vertical 回 Start 打开 Records，能看到新记录。

---

## RR-040 GitHub Kanban

E-06 / P1 / Review

GitHub Project + Issues 和本地 BOARD 对齐，日常在网页拖卡。

做完：看板列齐全，RR 号能搜到；README 有 Project 链接。

---

## RR-042 Start 卡片文案换掉占位符

E-01 / P2 / Backlog

World Music Explorer 入口卡片还是占位文字，换成和场景介绍一致。

做完：Start 点该模式，文案对得上；Console 无新红字。

---

## RR-043 Ocean 每个 meter 第二首歌

E-03 / P2 / Backlog

`gameplayTracks` 里每个 meter 再配一首，Free Pond 轮换更丰富。

做完：同 meter 鱼能切到另一首 clip，不破坏现有选鱼 UI。

---

## RR-044 Advanced 可选教程（不强制 Skip）

E-05 / P2 / Backlog

Advanced 进场景可选完整教程路径，不是只能 Skip。

做完：新玩家能跟教程走一遍再进正式，老玩家仍可直玩。

---

## RR-045 Vertical 键位重绑

E-04 / P2 / Backlog

Space/Down/方向键可配置（至少 Start Settings 或局内菜单）。

做完：改键后跳跃、捡香蕉、躲鹦鹉仍跟 beat 规则一致。

---

## RR-046 UI 文案通读

E-06 / P2 / Backlog

全项目 UI 英文字符串扫一遍，别扭的、太长的改掉。

做完：四模式+Start 可见文案统一语气，儿童向简短。

---

## RR-047 Baker 只补 ObjectivePanel

E-06 / P1 / Backlog

Vertical baker 对 `ObjectivePanel` 走 EnsureMissing-only，别重刷教程图布局。

做完：跑 baker 后设计师改过的 Objective 位置和图不被盖掉。

---

## RR-048 Singing Shell 难度试玩后调

E-03 / P2 / Backlog

Ocean Singing Shell 玩几轮后按反馈调命中次数或节奏窗口。

做完：Shell 难度和 Free Pond 主玩法梯度合理，有试玩备注。
