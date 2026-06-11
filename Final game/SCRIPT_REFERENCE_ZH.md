# 脚本说明文档（中文版）

本文档说明 `Final game` Unity 项目中每一个当前有效脚本的用途。脚本已经按场景和职责整理到 `Assets/Scripts` 与 `Assets/Editor/HierarchyBakers` 下。

## 阅读方式

- `场景挂载`：脚本组件直接挂在某个 `.unity` 场景里的对象上。
- `自动入口`：脚本通过 `RuntimeInitializeOnLoadMethod` 或运行时代码自动创建入口对象。
- `运行时创建`：游戏运行中由 Manager/UI/Spawner 用 `AddComponent` 添加到生成对象上。
- `数据/辅助`：主要提供枚举、配置、存档数据或通用工具。
- `编辑器工具`：只在 Unity Editor 中使用，不进入正式运行逻辑。

本项目的整体设计是：**Hierarchy 中保留可编辑 UI/世界结构，运行时代码按固定名字/路径绑定对象并接管行为。** 所以很多脚本不能随便改名，也不能随便改 Hierarchy 路径。

## Start ??

### `Assets/Scripts/Start/Menu/StartMenuController.cs`

- ???`????`?`????`
- ?????`Start`
- ???Start ????????????? `StartMenuCanvas`???? mode card?Settings/Records/About ????????????
- ?????`RuntimeScenePolicy`?`SceneTransitionManager`?`StartMenuCanvas` ???? UI ???
- ????????????? Unity ??????????????

### `Assets/Scripts/Start/Visuals/StartMenuMusicVisualizer.cs`

- ???`????`?`?????????`
- ?????`Start`
- ????? `StartMenuCanvas/Root/music` ?? Hierarchy ?????/icon ?????????????/???????
- ??????? `menuMusic` AudioSource?`Root/music` ?? Templates/Runtime/StaffLines?
- ???????????Start scene ? baker ???????/GUID ????

### `Assets/Scripts/Start/Records/LeaderboardManager.cs`

- ???`??/??`
- ?????Start RecordsPanel ???????????
- ???? `PlayerPrefs` ????? Easy/Hard ???????? Hierarchy ?? `RecordsPanel` ?????
- ?????`LeaderboardMode`???????
- ??????????????????????

## OceanRhythm 海洋节奏

### `Assets/Scripts/OceanRhythm/OceanRhythmManager.cs`

- 类型：`场景挂载`、`自动入口`
- 所属场景：`OceanRhythm`
- 用途：Little Rhythm Ocean 的玩法总控制器。负责阶段流程、鱼和节奏课程、曲库、BPM/节拍判定、输入、捕获状态、音乐播放，以及返回 Start。
- 关键依赖：`OceanRhythmUIController`、`OceanRhythmData`、`SimpleMetronomeAudio`、`RuntimeScenePolicy`、`SceneTransitionManager`。
- 是否建议改名：不建议。它是核心场景组件和自动入口。

### `Assets/Scripts/OceanRhythm/OceanRhythmUIController.cs`

- 类型：`场景挂载`、`运行时创建辅助对象`
- 所属场景：`OceanRhythm`
- 用途：绑定 `OceanRhythmCanvas`，控制所有海洋 UI 文本、按钮、弹窗、池塘鱼、节拍泡泡、桶图鉴、声音匹配界面和装饰物。
- 关键依赖：`OceanRhythmManager`、`OceanPondAnimal`、`OceanAnimalController`、`OceanNetCursor`、`OceanBucketSlot`、`OceanDecorationDragItem`、`WaterRippleController`、`OceanRoot` 下的固定 UI 路径。
- 是否建议改名：不建议。Manager 会引用/创建它。

### `Assets/Scripts/OceanRhythm/OceanRhythmData.cs`

- 类型：`数据/辅助`
- 所属场景：`OceanRhythm`
- 用途：定义海洋玩法的数据类型，包括鱼类型、装饰奖励、桶槽位、解锁条件和 `OceanBucketInventory` 存档逻辑。
- 关键依赖：`PlayerPrefs`、海洋 Manager/UI 代码。
- 是否建议改名：可以，但必须同步修改代码引用。

### `Assets/Scripts/OceanRhythm/OceanPondAnimal.cs`

- 类型：`场景挂载`、`运行时创建`
- 所属场景：`OceanRhythm`
- 用途：表示池塘里的鱼/动物。处理课程绑定、被选中状态、捕获进度泡泡、移动和点击交互。
- 关键依赖：`OceanRhythmManager`、`OceanRhythmUIController`、`OceanLesson`、`OceanFishType`。
- 是否建议改名：不建议。UI 生成池塘鱼时会按类型添加。

### `Assets/Scripts/OceanRhythm/OceanAnimalController.cs`

- 类型：`场景挂载`、`运行时创建`
- 所属场景：`OceanRhythm`
- 用途：控制引导用海洋动物 UI，包括兜底 Image/Text 和 Sprite 动画。
- 关键依赖：`OceanSpriteAnimator`、Unity UI Image/Text。
- 是否建议改名：不建议。生成引导动物时可能会按类型添加。

### `Assets/Scripts/OceanRhythm/OceanNetCursor.cs`

- 类型：`场景挂载`、`运行时创建`
- 所属场景：`OceanRhythm`
- 用途：捕鱼网光标的显示和位置控制。
- 关键依赖：Unity UI Image、鼠标/指针位置。
- 是否建议改名：不建议。场景或生成网光标会依赖它。

### `Assets/Scripts/OceanRhythm/OceanBucketSlot.cs`

- 类型：`场景挂载`、`运行时创建`
- 所属场景：`OceanRhythm`
- 用途：表示桶上的一个装饰槽位。处理槽位显示、标签、点击/指针交互和已放置装饰显示。
- 关键依赖：`OceanRhythmUIController`、`OceanBucketSlotId`、`OceanDecorationReward`。
- 是否建议改名：不建议。桶图鉴生成逻辑依赖它。

### `Assets/Scripts/OceanRhythm/OceanDecorationDragItem.cs`

- 类型：`运行时创建`
- 所属场景：`OceanRhythm`
- 用途：桶图鉴中可拖拽装饰物的行为。负责解锁状态、拖拽高亮、拖放到槽位、点击显示说明。
- 关键依赖：`OceanRhythmUIController`、`OceanDecorationReward`、Unity 拖拽/点击事件接口。
- 是否建议改名：不建议。`OceanRhythmUIController` 会按类型添加它。

### `Assets/Scripts/OceanRhythm/OceanSpriteAnimator.cs`

- 类型：`场景挂载`、`运行时创建`
- 所属场景：`OceanRhythm`
- 用途：简单的 Sprite 帧动画组件，用于海洋动物 Image。
- 关键依赖：Unity UI Image、Sprite 帧数组。
- 是否建议改名：不建议。海洋动物创建流程可能会按类型添加。

### `Assets/Scripts/OceanRhythm/SimpleMetronomeAudio.cs`

- 类型：`场景挂载`、`运行时创建`
- 所属场景：`OceanRhythm`
- 用途：简单节拍/节拍器音频辅助组件。
- 关键依赖：Unity `AudioSource`。
- 是否建议改名：不建议。`OceanRhythmManager` 会创建或引用它。

### `Assets/Scripts/OceanRhythm/WaterRippleController.cs`

- 类型：`场景挂载`、`运行时创建`
- 所属场景：`OceanRhythm`
- 用途：创建点击/鼠标位置的水波纹 UI 效果。
- 关键依赖：Unity UI Image/RectTransform。
- 是否建议改名：不建议。海洋 UI 根对象会添加它。

## VerticalRunner 竖版跑酷

### `Assets/Scripts/VerticalRunner/VerticalRunnerManager.cs`

- 类型：`场景挂载`、`自动入口`
- 所属场景：`VerticalRunner`
- 用途：竖版跑酷总控制器。负责教程/正式模式流程、倒计时、输入、分数/失误/连击、路线/玩家/相机/UI 初始化，以及 Retry/Reset。
- 关键依赖：`VerticalBeatSpawner`、`VerticalRunnerUI`、`VerticalRunnerPlayer`、`VerticalRunnerCamera`、`VerticalRunnerTemplates`、`RhythmManager`、`RuntimeScenePolicy`。
- 是否建议改名：不建议。它是场景组件和自动入口。

### `Assets/Scripts/VerticalRunner/VerticalBeatSpawner.cs`

- 类型：`场景挂载`、`运行时创建辅助对象`
- 所属场景：`VerticalRunner`
- 用途：生成竖版路线，包括平台、长平台、香蕉、鹦鹉障碍、终点和所有生成的玩法对象。
- 关键依赖：`VerticalRunnerSettings`、`VerticalRunnerTemplates`、`VerticalRunnerObjects`、`RuntimeScenePolicy`。
- 是否建议改名：不建议。Manager 和 Baker 都会引用它。

### `Assets/Scripts/VerticalRunner/VerticalRunnerUI.cs`

- 类型：`场景挂载`、`运行时兜底创建`
- 所属场景：`VerticalRunner`
- 用途：绑定 `VerticalRunnerCanvas`，控制 HUD、节拍点、教程/规则弹窗、结果界面、伤害闪屏、游戏中 Retry/Back 按钮。
- 关键依赖：`VerticalRunnerManager`、`SceneTransitionManager`、`VerticalRunnerCanvas` 下的固定 UI 路径。
- 是否建议改名：不建议。Manager 和 Baker 都会挂载/创建它。

### `Assets/Scripts/VerticalRunner/VerticalRunnerPlayer.cs`

- 类型：`场景挂载`、`运行时创建`
- 所属场景：`VerticalRunner`
- 用途：玩家移动与状态。处理跳跃弧线、恢复、分支移动、拾取物/障碍碰撞、玩家 Sprite/Collider/Rigidbody。
- 关键依赖：`VerticalRunnerPlatform`、`VerticalRunnerPickup`、`VerticalRunnerObstacle`。
- 是否建议改名：不建议。玩家模板和生成流程会用到它。

### `Assets/Scripts/VerticalRunner/VerticalRunnerCamera.cs`

- 类型：`场景挂载`、`运行时创建`
- 所属场景：`VerticalRunner`
- 用途：竖版跑酷相机跟随辅助。
- 关键依赖：`VerticalRunnerManager`、场景 Camera。
- 是否建议改名：不建议。Manager/Baker 会引用它。

### `Assets/Scripts/VerticalRunner/VerticalRunnerSettings.cs`

- 类型：`数据/辅助`
- 所属场景：`VerticalRunner`
- 用途：定义竖版跑酷模式、教程步骤类型，以及可序列化的时间、路线、规则、分数、Sprite、颜色配置。
- 关键依赖：`VerticalRunnerManager`、`VerticalBeatSpawner`。
- 是否建议改名：可以，但必须同步修改代码引用。

### `Assets/Scripts/VerticalRunner/VerticalRunnerTemplates.cs`

- 类型：`场景挂载`
- 所属场景：`VerticalRunner`
- 用途：保存 Hierarchy 中可编辑模板引用，例如玩家模板、平台模板、香蕉模板、障碍模板、终点模板和 runtime root。
- 关键依赖：`VerticalBeatSpawner`、场景对象 `VerticalRunnerTemplates`。
- 是否建议改名：不建议。场景和 Baker 引用它。

### `Assets/Scripts/VerticalRunner/VerticalRunnerObjects.cs`

- 类型：`运行时创建`、`数据/辅助`
- 所属场景：`VerticalRunner`
- 用途：定义竖版运行时生成对象组件：平台节点、拾取物、障碍物和节拍脉冲效果。
- 关键依赖：`RhythmManager`、`VerticalBeatSpawner`、`VerticalRunnerPlayer`。
- 是否建议改名：可以，但必须同步修改代码引用。

### `Assets/Scripts/VerticalRunner/VerticalScrollingBackground.cs`

- 类型：`场景挂载`
- 所属场景：`VerticalRunner`
- 用途：生成并滚动竖向背景瓦片，让背景覆盖相机视野并避免明显接缝。
- 关键依赖：Camera、SpriteRenderer、名为 `vertical` 的背景对象。
- 是否建议改名：不建议。场景和 Baker 引用它。

## AdvancedRunner 高级跑酷

### `Assets/Scripts/AdvancedRunner/AdvancedRunner.cs`

- 类型：`自动入口`、主要实现文件
- 所属场景：`AdvancedRunner`
- 用途：高级跑酷的主要逻辑文件。包含 `AdvancedRunnerManager`、`AdvancedRunnerPlayer`、`AdvancedRunnerUI` 的 partial 实现，以及高级设置、反馈样式、目标谱面、音乐阶段、输入判定、分数、Game Over 流程和场景绑定。
- 关键依赖：`AdvancedRunnerManager.cs`、`AdvancedRunnerPlayer.cs`、`AdvancedRunnerUI.cs` 三个桥接文件，`AdvancedRunnerFeedbackConfig`、`AdvancedRunnerMusicConfig`、`RhythmManager`、`SceneTransitionManager`、`RuntimeScenePolicy`。
- 是否建议改名：不建议。它包含 partial 实现和自动入口。

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerManager.cs`

- 类型：`场景挂载`、partial 桥接文件
- 所属场景：`AdvancedRunner`
- 用途：给 Unity 使用的同名 MonoBehaviour 桥接文件。真正的 Manager 逻辑在 `AdvancedRunner.cs` 中。
- 关键依赖：必须保持 `partial class AdvancedRunnerManager`。
- 是否建议改名：不建议。Unity 组件稳定性依赖这个文件名/类名。

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerPlayer.cs`

- 类型：`场景挂载`、partial 桥接文件
- 所属场景：`AdvancedRunner`
- 用途：给 Unity 使用的同名 MonoBehaviour 桥接文件。真正的 Player 逻辑在 `AdvancedRunner.cs` 中。
- 关键依赖：必须保持 `partial class AdvancedRunnerPlayer`。
- 是否建议改名：不建议。Unity 组件稳定性依赖这个文件名/类名。

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerUI.cs`

- 类型：partial 桥接文件
- 所属场景：`AdvancedRunner`
- 用途：给 Unity 使用的同名 MonoBehaviour 桥接文件。真正的 UI 绑定、弹窗和结果界面逻辑在 `AdvancedRunner.cs` 中。
- 关键依赖：必须保持 `partial class AdvancedRunnerUI`。
- 是否建议改名：不建议。Unity 组件稳定性依赖这个文件名/类名。

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerFeedbackConfig.cs`

- 类型：`场景挂载`、配置组件
- 所属场景：`AdvancedRunner`
- 用途：Hierarchy 中可编辑的反馈配置包装组件。让设计者能在场景对象上调整反馈文字、颜色、字体、脉冲等。
- 关键依赖：`AdvancedRunner.cs` 中的 `AdvancedFeedbackStyle`。
- 是否建议改名：不建议。场景配置对象引用它。

### `Assets/Scripts/AdvancedRunner/AdvancedRunnerMusicConfig.cs`

- 类型：`场景挂载`、配置组件
- 所属场景：`AdvancedRunner`
- 用途：Hierarchy 中可编辑的音乐配置包装组件，用来设置不同阶段的 BGM 和 BPM。
- 关键依赖：`AdvancedRunnerManager` 会读取 `AdvancedRunnerConfig/Music/Scene|Tutorial|Game` 下的配置对象。
- 是否建议改名：不建议。场景配置对象引用它。

## Shared 通用脚本

### `Assets/Scripts/Shared/Navigation/ChangeScene.cs`

- 类型：`场景挂载`
- 所属场景：`Start`、`VerticalRunner`
- 用途：按钮场景跳转辅助脚本，支持按 Build Index 或场景名加载。
- 关键依赖：`SceneTransitionManager`、Unity UI Button。
- 是否建议改名：不建议。场景按钮上挂着这个组件。

### `Assets/Scripts/Shared/Navigation/SceneTransitionManager.cs`

- 类型：`运行时创建单例`
- 所属模块：Shared
- 用途：统一场景加载工具，带淡入淡出遮罩。调用 `LoadScene` 时会自动创建持久化的过渡 Canvas。
- 关键依赖：`SceneManager`、Unity UI Canvas/Image/CanvasGroup。
- 是否建议改名：不建议。很多脚本调用 `SceneTransitionManager.LoadScene`。

### `Assets/Scripts/Shared/Runtime/RuntimeScenePolicy.cs`

- 类型：`数据/辅助`
- 所属模块：Shared
- 用途：可序列化运行策略。决定是否使用场景对象、是否自动创建缺失对象、是否重建 UI、是否保留图片覆盖、运行时生成对象放到哪个 root。
- 关键依赖：`StartMenuController`、`OceanRhythmManager`、`VerticalRunnerManager`、`AdvancedRunnerManager`、各类 UI/Spawner。
- 是否建议改名：可以，但必须同步修改代码引用。

### `Assets/Scripts/Shared/Rhythm/RhythmManager.cs`

- 类型：`场景挂载`、`运行时兜底创建`
- 所属模块：Shared，主要被 `VerticalRunner` 和 `AdvancedRunner` 使用
- 用途：通用节奏管理器。负责 BPM/音频时间、输入时机判定、节拍可视化、调试 UI、判定窗口和兜底音乐源。
- 关键依赖：`AudioSource`、Unity UI、`VerticalRunnerManager`、`AdvancedRunnerManager`、`VerticalRunnerBeatPulse`。
- 是否建议改名：不建议。场景引用和运行时兜底逻辑都依赖它。

## Editor / HierarchyBakers 编辑器工具

### `Assets/Editor/HierarchyBakers/AllSceneHierarchyBaker.cs`

- 类型：`编辑器工具`
- 所属模块：项目维护工具
- 用途：全场景 Hierarchy 合约维护工具。用于创建/修复 Start、OceanRhythm、VerticalRunner、AdvancedRunner 的可编辑 UI/世界结构、默认 UI、模板、配置对象、背景和 runtime policy。
- 关键依赖：UnityEditor API、活动场景路径、项目 Hierarchy 命名合约。
- 是否建议改名：运行时不受影响，但改名后应同步更新菜单/文档。

### `Assets/Editor/HierarchyBakers/SceneHierarchyBaker.cs`

- 类型：`编辑器工具`
- 所属模块：Start + OceanRhythm 维护
- 用途：较小的 Baker，重点用于重建 Start 和 OceanRhythm 的 Hierarchy 合约。
- 关键依赖：UnityEditor API、`StartMenuController`、`OceanRhythmManager`。
- 是否建议改名：运行时不受影响，但改名后应同步更新菜单/文档。

### `Assets/Editor/HierarchyBakers/VerticalSceneHierarchyBaker.cs`

- 类型：`编辑器工具`
- 所属模块：VerticalRunner 维护
- 用途：VerticalRunner 专用 Baker。维护 Manager、模板、背景、Canvas、HUD、弹窗、结果 UI、GameControls 和节拍点。
- 关键依赖：UnityEditor API、`VerticalRunnerManager`、`VerticalBeatSpawner`、`VerticalRunnerUI`、`VerticalRunnerTemplates`。
- 是否建议改名：运行时不受影响，但改名后应同步更新菜单/文档。

## 已删除的旧脚本/无用脚本

这些脚本已经被删除，因为静态检查显示它们没有活动场景、资源、代码或启动引用：

- `Bonus`
- 旧 `SoundManager`
- 旧 `LoginManager`
- `RhythmGeneratedObstacle`

不要从 `../BunnyLegacyArchive` 恢复旧 bunny runner 文件，除非明确要求。
