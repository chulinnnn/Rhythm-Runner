# Rhythm Runner 节奏跑酷 完整版设计说明文档

\*\*文档用途\*\*：可直接按此文档在Unity搭建全场景、全UI、全预制体 → 复制文档给Cursor生成完整代码 → 一键运行可玩完整游戏

\*\*核心玩法\*\*：2D横版音乐节奏自动跑酷，按音乐节拍跳跃/点击道具得分，超长横向地图，多页面自由跳转，支持难度选择和Blind模式

\*\*页面总数\*\*：5个（满足多界面交互需求）

\*\*单页面按钮数\*\*：3\-5个（按钮名称全固定，无歧义，新增难度选择、Blind模式相关按钮）

# 一、游戏基础信息

1\.  \*\*游戏名称\*\*：Rhythm Runner（节奏跑酷）

2\.  \*\*游戏引擎\*\*：Unity 2022\+ 2D Core

3\.  \*\*核心机制\*\*

- 玩家\*\*自动持续向右奔跑\*\*，无需控制移动

- 3个功能键位：跳、蹲、加速，均需按音乐节拍判定Perfect/Good/Miss，对应不同功能效果

- 鼠标左键点击掉落道具：仅节拍内点击生效，加分/加血；Blind模式下得分翻倍

- 碰撞障碍物扣血，血量为0游戏结束

- 支持3档音乐难度选择，支持Blind模式（隐藏可视化节奏，得分翻倍）；Blind模式可在游戏中途（积分达标后）触发选择切换

4\.  \*\*技术强制约束\*\*

- 节奏判定唯一来源：\`AudioSource\.time\`

- 禁止使用\`Time\.time\`做节奏计算

- 所有节奏行为必须通过\`BeatManager\`

# 二、完整5页面设计（含固定按钮名\+功能\+跳转，新增难度/Blind相关）

所有页面统一放在\`UIRoot\`空物体下，\*\*仅开始主页面默认显示\*\*，其余页面默认隐藏；新增的难度选择、Blind模式按钮均按固定命名设计，确保与代码完美匹配。

## 页面1：开始主页面（MainMenu）

\-  \*\*页面作用\*\*：游戏启动默认页面，核心入口，包含难度选择和模式切换

\-  \*\*显示内容\*\*：游戏背景图、标题文字、难度选择区、功能按钮、Blind模式切换按钮

\-  \*\*按钮数量\*\*：5个（新增难度选择融合至按钮，不额外增加数量）

\-  \*\*固定按钮名称\+功能\*\*

- \`Btn\_StartGame\`：开始游戏 → 隐藏当前页，显示游戏内页，启动跑酷\+播放音乐（使用当前选择的难度）

- \`Btn\_Profile\`：个人资料 → 隐藏当前页，显示个人资料页

- \`Btn\_Difficulty\`：难度选择 → 点击弹出下拉菜单（低/中/高），默认选中“中”难度

- \`Btn\_BlindMode\`：Blind模式切换 → 点击切换开启/关闭（默认关闭，显示“开启Blind模式”；开启后显示“关闭Blind模式”）

- \`Btn\_ExitGame\`：退出游戏 → 关闭Unity程序

补充：难度选择下拉菜单选项固定命名：\`Difficulty\_Low\`（低）、\`Difficulty\_Mid\`（中）、\`Difficulty\_High\`（高）

## 页面2：游戏内页面（GamePlay）

\-  \*\*页面作用\*\*：核心游戏运行页面，支持游戏中切换Blind模式

\-  \*\*显示内容\*\*：游戏背景、实时分数、实时血量、可视化节奏元素（默认显示，Blind模式隐藏）、功能按钮

\-  \*\*按钮数量\*\*：4个（新增Blind模式切换按钮）

\-  \*\*固定按钮名称\+功能\*\*

- \`Btn\_PauseGame\`：暂停游戏 → 冻结游戏，显示暂停弹窗

- \`Btn\_BackToMain\`：返回主页 → 停止音乐，重置游戏，回到开始主页面

- \`Btn\_GoToProfile\`：个人资料 → 隐藏当前页，显示个人资料页

- \`Btn\_ToggleBlind\`：切换Blind模式 → 仅游戏积分达标后可点击（未达标时置灰），点击后弹出选择弹窗，确认后切换模式

- \`BlindTogglePopup\`：Blind模式切换弹窗（默认隐藏，积分达标后点击按钮显示），包含两个按钮：\`Btn\_ConfirmBlind\`（确认切换）、\`Btn\_CancelBlind\`（取消切换）

补充：可视化节奏元素（默认显示）：屏幕中间横向节奏线\+节拍点（随音乐节拍闪烁），Blind模式开启后，该元素隐藏；节奏元素固定命名：\`Rhythm\_Visual\`（父物体，包含节奏线和节拍点）

## 页面3：游戏结算页面（GameOver）

\-  \*\*页面作用\*\*：玩家血量为0时触发，展示最终成绩，包含模式和难度标识

\-  \*\*显示内容\*\*：结束标题、最终分数（标注Blind模式翻倍加成）、本局最高连击、当前选择难度、功能按钮

\-  \*\*按钮数量\*\*：4个

\-  \*\*固定按钮名称\+功能\*\*

- \`Btn\_RestartGame\`：重新开始 → 重置血量/分数/玩家位置，回到游戏内页（沿用之前选择的难度和模式）

- \`Btn\_BackToMain\`：返回主页 → 停止音乐，重置游戏，回到开始主页面

- \`Btn\_GoToProfile\`：个人资料 → 隐藏当前页，显示个人资料页

- \`Btn\_ShareScore\`：分享成绩 → 触发分享弹窗（UI占位，显示最终分数、难度、模式）

## 页面4：个人资料页面（Profile）

\-  \*\*页面作用\*\*：全局个人中心，查看游戏数据，区分难度和模式的成绩

\-  \*\*显示内容\*\*：玩家头像、总游戏次数、各难度历史最高分（低/中/高）、Blind模式历史最高分、总得分、成就展示区（\`AchievementPanel\`）、功能按钮

\-  \*\*按钮数量\*\*：3个

\-  \*\*固定按钮名称\+功能\*\*

- \`Btn\_BackLastPage\`：返回上一页 → 回到跳转前的页面

- \`Btn\_ClearData\`：清空数据 → 重置本地所有游戏数据（含各难度、各模式成绩）

- \`Btn\_GoToMain\`：返回主页 → 直接回到开始主页面

## 页面5：游戏设置页面（Settings）

\-  \*\*页面作用\*\*：调整游戏基础参数，不影响难度和模式设置

\-  \*\*显示内容\*\*：音乐音量、音效音量、震动开关、功能按钮

\-  \*\*按钮数量\*\*：3个

\-  \*\*固定按钮名称\+功能\*\*

- \`Btn\_BackLastPage\`：返回上一页 → 回到跳转前的页面

- \`Btn\_ResetSettings\`：重置设置 → 恢复默认音量/开关（不影响难度和Blind模式）

- \`Btn\_GoToMain\`：返回主页 → 直接回到开始主页面

# 三、音乐难度设计（新增，分低中高三档）

难度核心差异：BPM（节拍速度）、节奏判定窗口（难度越高，窗口越窄）、道具生成速度、障碍密度，具体参数固定如下，确保Cursor生成代码时可直接调用：

|难度档位|BPM（节拍速度）|Perfect窗口|Good窗口|道具生成间隔|障碍密度|
|---|---|---|---|---|---|
|低（Low）|100|≤0\.15s|≤0\.3s|Random\(1\.5s, 2\.5s\)|低（每5秒1个）|
|中（Mid）|120（默认）|≤0\.1s|≤0\.25s|Random\(0\.8s, 2\.0s\)|中（每3秒1个）|
|高（High）|140|≤0\.08s|≤0\.2s|Random\(0\.5s, 1\.5s\)|高（每2秒1个）|

补充：难度切换逻辑：在开始主页面选择难度后，点击“开始游戏”生效；游戏运行中无法切换难度，需返回主页重新选择。

# 四、Blind模式设计（新增）

1\.  模式核心规则

- 默认模式（Normal Mode）：显示可视化节奏元素（节奏线\+节拍点），得分按基础规则计算（道具\+100分，Perfect跳跃额外\+50分）

- Blind模式（Blind Mode）：隐藏所有可视化节奏元素，仅靠音乐节拍判断操作，得分翻倍（道具\+200分，Perfect跳跃额外\+100分）

- 模式切换：1\. 开始主页面可自由切换（默认关闭）；2\. 游戏中途切换：当游戏积分达到1000分（固定触发阈值，可在代码中修改）后，\`Btn\_ToggleBlind\`解锁，点击后弹出选择弹窗，玩家可选择“确认切换”或“取消切换”，确认后立即生效，取消则保持当前模式；游戏内切换不影响当前分数和游戏进度

- 模式标识：游戏内页面、结算页面显示当前模式（“Normal Mode”/“Blind Mode”），结算页面标注“Blind模式得分翻倍”；积分达标后，游戏内页面显示“积分达标，可切换Blind模式”提示

2\.  可视化节奏元素细节（固定命名，便于代码绑定）

- 父物体：\`Rhythm\_Visual\`（挂载在GamePlay页面下）

- 子元素1：\`Rhythm\_Line\`（横向节奏线，位于屏幕中间）

- 子元素2：\`Rhythm\_Dot\`（节拍点，随音乐节拍闪烁，沿节奏线排列）

- 控制逻辑：Blind模式开启 → \`Rhythm\_Visual\` 设为隐藏；关闭 → 设为显示

# 五、Unity完整搭建步骤（纯手动，一步到底，新增难度/Blind相关元素）

## 1\. 工程文件夹创建

在\`Assets\`下新建固定文件夹：

- \`Scripts\`（代码）

- \`Sprites\`（图片/角色/道具/障碍/UI/节奏可视化元素）

- \`Scripts\`（代码）

- \`Sprites\`（图片/角色/道具/障碍/UI/节奏可视化元素）

- \`Audio\`（背景音乐、点击音效，按难度分类存放：Audio/Low、Audio/Mid、Audio/High；新增Audio/Sound文件夹，存放道具/障碍/成就专属音效）

- \`Prefabs\`（道具、障碍预制体，按类型分类：Prefabs/Item、Prefabs/Obstacle）

- \`Scenes\`（游戏场景）

- \`Resources\`（资源文件夹，存放玩家皮肤、解锁的音效文件，便于代码调用）

- \`Prefabs\`（道具、障碍预制体）

- \`Scenes\`（游戏场景）

## 2\. 场景基础物体（Hierarchy固定列表，新增节奏可视化元素）

\`\`\`

Main Camera（Tag = MainCamera）

Player（玩家角色）

Ground（超长横向地面）

MusicPlayer（音乐管理）

GameManager（核心逻辑空物体）

UIRoot（UI总容器）

→ MainMenu（开始主页面）

→ GamePlay（游戏内页面）

→ Rhythm\_Visual（节奏可视化父物体，默认显示）

→ Rhythm\_Line（节奏线）

→ Rhythm\_Dot（节拍点）

→ GameOver（结算页面）

→ Profile（个人资料页）

→ Settings（设置页）

\`\`\`

## 3\. 基础物体参数设置

- \*\*Player\*\*：2D Sprite → Square，Layer = Player；组件：\`Rigidbody2D\`（Gravity=3，Freeze Rotation Z）、\`BoxCollider2D\`

- \*\*Ground\*\*：2D Sprite → Square，Scale = \(50,1,1\)（超长横向）；组件：\`BoxCollider2D\`（无刚体），位置：屏幕底部

- \*\*MusicPlayer\*\*：组件：\`AudioSource\`，取消\`Play On Awake\`；需绑定对应难度的背景音乐（低/中/高）

- \*\*Rhythm\_Visual\*\*：默认勾选显示，Blind模式下隐藏；Rhythm\_Line设为横向长条，Rhythm\_Dot设为圆形Sprite（初始隐藏，随节拍闪烁显示）

- \*\*GameManager/UIRoot\*\*：空物体，仅用于挂载脚本

## 4\. 图层固定设置（Edit→Project Settings→Tags and Layers）

- Layer 8：Player

- Layer 9：Item

- Layer 10：Obstacle

## 5\. 预制体制作（放入Prefabs文件夹）

- \*\*ItemPrefab（道具预制体）\*\*：共5个，分别为LifePrefab、ScorePrefab、InvinciblePrefab、ComboMultiplierPrefab、SlowDownPrefab；均为2D Sprite，Layer = Item；组件：\`BoxCollider2D\`、\`SpriteRenderer\`，绑定对应音效

- \*\*ObstaclePrefab（障碍预制体）\*\*：共4个，分别为StaticObstaclePrefab、MovingObstaclePrefab、HighObstaclePrefab、InvisibleObstaclePrefab；均为2D Sprite，Layer = Obstacle；组件：\`BoxCollider2D\`，MovingObstaclePrefab额外添加\`Rigidbody2D\`（冻结Y轴移动和旋转），绑定对应音效

## 6\. UI文本固定命名（代码匹配用，新增难度/模式相关文本）

- 游戏内分数：\`Txt\_CurrentScore\`

- 游戏内血量：\`Txt\_CurrentHP\`

- 游戏内当前难度：\`Txt\_CurrentDifficulty\`（显示“低/中/高”）

- 游戏内当前模式：\`Txt\_CurrentMode\`（显示“Normal/Blind”）

- 游戏内Blind模式触发提示：\`Txt\_BlindUnlockTip\`（默认隐藏，积分≥1000分时显示，内容：“积分达标，可切换Blind模式！”）

- 结算页最终分：\`Txt\_FinalScore\`

- 结算页难度标识：\`Txt\_GameDifficulty\`

- 结算页模式标识：\`Txt\_GameMode\`

- 资料页低难度最高分：\`Txt\_LowHighScore\`

- 资料页中难度最高分：\`Txt\_MidHighScore\`

- 资料页高难度最高分：\`Txt\_HighHighScore\`

- 资料页Blind模式最高分：\`Txt\_BlindHighScore\`

- 成就相关文本：\`Txt\_AchievementUnlock\`（成就解锁弹窗标题）、\`Txt\_AchievementDesc\`（成就解锁弹窗描述）、\`Txt\_AchievementReward\`（成就解锁弹窗奖励）；个人资料页成就展示区文本均以\`Txt\_Achievement\_\`\+成就命名（如\`Txt\_Achievement\_StartGame\`）

# 六、游戏核心系统设计（继承原版\+完整版扩展，新增难度/Blind模式逻辑）

## 1\. 节奏核心系统（BeatManager，新增难度适配）

- 接口：\`GetAudioTime\(\)\`、\`IsInBeatWindow\(\)\`、\`GetNearestBeatTime\(\)\`、\`SetDifficulty\(string difficulty\)\`（设置难度，切换BPM和判定窗口）

- 判定规则：随难度变化（详见第三部分难度参数）

- BPM控制：根据选择的难度自动切换，默认中难度（120）

- 音乐切换：难度切换后，自动加载对应难度文件夹下的背景音乐（Audio/Low、Audio/Mid、Audio/High）

## 2\. 玩家系统（不变，适配难度节奏）

- \`PlayerController\`：自动向右匀速奔跑（速度随难度微调：低=3，中=4，高=5），监听3个功能键位（空格=跳、S=蹲、D=加速）和鼠标左键输入；3个键位功能独立，均需通过节拍判定触发对应效果

- \`JumpSystem\`：节拍判定核心，修复掉落/穿地/空中连跳BUG；3个键位功能定义：① 跳（空格）：按节拍判定Perfect→高跳、Good→普通跳、Miss→弱跳，用于躲避低处障碍；② 蹲（S）：按节拍判定触发，短暂降低玩家高度（持续0\.5秒），用于躲避高处障碍，Perfect判定额外获得0\.3秒无敌帧；③ 加速（D）：按节拍判定触发，临时提升奔跑速度（基础速度×1\.5，持续2秒），冷却时间5秒，Perfect判定冷却时间缩短至3秒，Miss判定无加速效果且触发短暂减速（0\.5秒）

- 触发规则：无论哪个键位，均需在节拍窗口内按下才生效，Miss判定仅触发基础弱效果（跳=弱跳、蹲=无无敌帧、加速=无效果）

## 3\. 道具系统（适配难度，新增Blind模式得分翻倍）

- 生成间隔：随难度变化（详见第三部分难度参数），与音乐节拍联动（道具生成时间贴合节拍点，难度越高，节拍联动越紧密）

- 类型：共5种，分基础道具和功能性道具，所有道具仅节拍内鼠标左键点击生效，Blind模式下得分/效果翻倍，具体如下：
            基础道具1：Life（生命道具）→ 加血\+1，上限3；Blind模式下加血\+2，生成概率随难度降低（低难度25%、中难度20%、高难度15%）

- 基础道具2：Score（分数道具）→ 基础\+100分，Blind模式\+200分；生成概率随难度提升（低难度20%、中难度25%、高难度30%）

- 功能性道具1：Invincible（无敌道具）→ 点击后获得3秒无敌时间（可免疫障碍碰撞扣血），Perfect判定额外延长1秒无敌；Blind模式下无敌时间翻倍至6秒，生成概率固定15%（全难度一致）

- 功能性道具2：ComboMultiplier（连击倍率道具）→ 点击后连击倍率直接提升至×3，持续5秒；Perfect判定持续时间延长至7秒，Miss判定无效果；Blind模式下持续时间翻倍至10秒，生成概率固定15%（全难度一致）

- 功能性道具3：SlowDown（减速道具）→ 点击后玩家奔跑速度降低至基础速度的0\.7倍，障碍生成速度同步降低，持续4秒；Perfect判定持续时间延长至6秒，适合高难度躲避密集障碍；Blind模式下减速效果不变，生成概率固定15%（全难度一致）

- 交互：鼠标左键\+节拍内点击生效，Blind模式下得分/效果自动翻倍，无需额外操作；道具生成后3秒内未点击则自动消失，道具之间不可叠加生效（同一时间仅能触发一种功能性道具效果）

- 道具可视化与命名：所有道具预制体命名固定（LifePrefab、ScorePrefab、InvinciblePrefab、ComboMultiplierPrefab、SlowDownPrefab），Sprite区分明显（生命=红色爱心、分数=黄色星星、无敌=蓝色护盾、连击=紫色倍数、减速=绿色时钟），掉落时伴随专属节拍音效

## 4\. 障碍系统（适配难度）

- 碰撞扣血：HP\-1，无节奏保护；若触发无敌道具效果，碰撞后不扣血、不触发无敌帧

- 无敌帧：0\.5s，防止连续扣血（仅非无敌状态下碰撞生效）

- 障碍密度：随难度变化（详见第三部分难度参数），难度越高，障碍生成越频繁，且与音乐节拍联动（障碍出现时间贴合节拍点，提升节奏关联性）

- 障碍类型：共4种，分基础障碍和特殊障碍，视觉区分明显，适配3个键位功能，具体如下：
基础障碍：StaticObstacle（静态障碍）→ 固定在地面，高度中等，需按节拍跳（空格）躲避；预制体命名StaticObstaclePrefab，密度随难度提升（低难度每5秒1个、中难度每3秒1个、高难度每2秒1个）

- 特殊障碍1：MovingObstacle（移动障碍）→ 左右横向移动（移动速度随难度提升：低=1、中=1\.5、高=2），高度中等，需按节拍跳（空格）或加速（D键）躲避；预制体命名MovingObstaclePrefab，生成概率随难度提升（低难度10%、中难度20%、高难度30%）

- 特殊障碍2：HighObstacle（高空障碍）→ 高度较高，固定在地面，需按节拍蹲（S键）躲避；Perfect判定蹲姿持续时间延长0\.2秒，Miss判定蹲姿无效（会碰撞扣血）；预制体命名HighObstaclePrefab，生成概率随难度提升（低难度5%、中难度15%、高难度25%）

- 特殊障碍3：InvisibleObstacle（隐形障碍）→ 初始隐形，仅在节拍点前0\.3秒短暂显示（Blind模式下完全不显示），需凭节奏记忆按节拍跳/蹲/加速躲避；碰撞后扣血\+触发短暂减速（0\.5秒）；预制体命名InvisibleObstaclePrefab，生成概率固定10%（全难度一致），仅中、高难度出现（低难度不生成）

- 障碍交互补充：所有障碍碰撞后均触发专属音效，隐形障碍碰撞后额外显示1秒碰撞特效；障碍生成后超出屏幕左侧自动销毁，避免内存占用

## 5\. 数值系统（新增Blind模式得分翻倍逻辑）

- \`HealthSystem\`：初始HP=3，HP=0触发游戏结束，与难度、模式无关

- \`ScoreSystem\`：基础得分规则不变，新增模式加成；Blind模式开启时，所有得分（道具、Perfect跳跃）自动×2；难度不影响得分基数，仅影响节奏判定和生成频率

- 连击倍率：支持连击（连续Perfect/Good操作），倍率最高×3，与难度、模式叠加（例：Blind模式\+连击×3，道具得分=100×2×3=600）

## 6\. 摄像机系统（不变）

自动跟随玩家向右移动，保持玩家在屏幕中间

## 7\. 难度与Blind模式管理系统（新增，封装在GameManager中）

- 难度管理：记录当前选择的难度，提供难度切换接口，同步更新BeatManager的BPM、判定窗口和音乐

- Blind模式管理：记录当前模式状态（开启/关闭）和游戏积分，当积分≥1000分时，解锁游戏内Blind模式切换按钮并显示提示；提供模式切换接口，接收玩家选择（确认/取消），控制可视化节奏元素的显示/隐藏，同步更新ScoreSystem的得分倍率

- 数据保存：记录各难度、各模式的历史最高分，同步到Profile页面显示

## 7\. 难度与Blind模式管理系统（新增，封装在GameManager中）

- 难度管理：记录当前选择的难度，提供难度切换接口，同步更新BeatManager的BPM、判定窗口和音乐

- Blind模式管理：记录当前模式状态（开启/关闭）和游戏积分，当积分≥1000分时，解锁游戏内Blind模式切换按钮并显示提示；提供模式切换接口，接收玩家选择（确认/取消），控制可视化节奏元素的显示/隐藏，同步更新ScoreSystem的得分倍率

- 数据保存：记录各难度、各模式的历史最高分，同步到Profile页面显示

## 8\. 成就系统设计（新增，提升玩家粘性与挑战欲）

- 成就核心规则：成就分基础成就和挑战成就，解锁后永久保存，同步显示在个人资料页；解锁时触发弹窗提示（显示成就名称、解锁条件、奖励），所有成就均与游戏核心玩法（节奏判定、键位操作、难度、Blind模式）联动

- 成就分类与具体设计（固定命名，便于代码绑定）：
            基础成就（共6个，新手易解锁，引导熟悉玩法）：
\`Achievement\_StartGame\`（初次启程）：解锁条件：首次启动游戏并进入游戏内页；奖励：解锁基础玩家皮肤1款

- \`Achievement\_Perfect5\`（节奏达人）：解锁条件：单次游戏内获得5次连续Perfect判定；奖励：解锁节拍提示音1款

- \`Achievement\_CollectLife\`（生命守护）：解锁条件：单次游戏内收集3个Life道具；奖励：初始HP永久\+1（上限4）

- \`Achievement\_Combo3\`（连击大师）：解锁条件：单次游戏内连击倍率达到×3；奖励：Score道具基础得分\+20

- \`Achievement\_BlindUnlock\`（盲打入门）：解锁条件：首次在游戏中途（积分≥1000分）解锁Blind模式切换权限；奖励：Blind模式得分额外\+10%

- \`Achievement\_ClearLow\`（初露锋芒）：解锁条件：低难度下游戏得分达到1500分；奖励：解锁低难度专属背景音乐1首

挑战成就（共8个，进阶挑战，提升长期粘性）：
               \`Achievement\_Perfect20\`（节奏大神）：解锁条件：单次游戏内获得20次连续Perfect判定；奖励：解锁高级玩家皮肤1款

\`Achievement\_Blind2000\`（盲打高手）：解锁条件：Blind模式下游戏得分达到2000分；奖励：Blind模式得分额外\+20%

\`Achievement\_NoMiss\`（零失误挑战）：解锁条件：单次游戏内无Miss判定（仅低/中难度可解锁）；奖励：无敌道具生成概率\+5%

\`Achievement\_ClearHigh\`（巅峰挑战）：解锁条件：高难度下游戏得分达到3000分；奖励：解锁高难度专属背景音乐1首

\`Achievement\_Invincible3\`（无敌王者）：解锁条件：单次游戏内连续触发3次无敌道具效果；奖励：无敌道具持续时间\+1秒

\`Achievement\_DodgeInvisible\`（隐形克星）：解锁条件：单次游戏内成功躲避5个隐形障碍；奖励：隐形障碍显示时间延长0\.1秒

\`Achievement\_TotalScore10000\`（积分霸主）：解锁条件：累计游戏总得分达到10000分；奖励：所有道具基础效果\+10%

\`Achievement\_AllDifficulty\`（全难度大师）：解锁条件：低、中、高难度均获得2000分以上成绩；奖励：解锁全部背景音乐和玩家皮肤

成就显示与奖励：个人资料页新增成就展示区（固定命名\`AchievementPanel\`），显示已解锁/未解锁成就（未解锁显示灰色，解锁显示彩色）；奖励自动生效，无需手动领取，皮肤/音效可在设置页切换

成就数据：与游戏其他数据同步存档，清空数据时同步清空成就进度；成就解锁进度实时更新，结算页面显示本局解锁的成就（若有）

# 七、脚本模块清单（共14个，新增2个道具/成就相关，全固定）

- Core：\`GameManager\`、\`BeatManager\`、\`UIManager\`、\`ModeDifficultyManager\`（新增，管理难度和Blind模式）

- Player：\`PlayerController\`、\`JumpSystem\`

- Items：\`ItemSpawner\`、\`FallingItem\`、\`ItemInteractionSystem\`、\`ItemEffectManager\`（新增，管理所有道具效果触发与叠加）

- Obstacles：\`Obstacle\`、\`ObstacleSpawner\`（新增，管理不同类型障碍生成与联动节拍）

- Systems：\`HealthSystem\`、\`ScoreSystem\`、\`AchievementSystem\`（新增，管理成就解锁、进度保存与奖励生效）

- Core：\`GameManager\`、\`BeatManager\`、\`UIManager\`、\`ModeDifficultyManager\`（新增，管理难度和Blind模式）

- Player：\`PlayerController\`、\`JumpSystem\`

- Items：\`ItemSpawner\`、\`FallingItem\`、\`ItemInteractionSystem\`

- Obstacles：\`Obstacle\`

- Systems：\`HealthSystem\`、\`ScoreSystem\`

# 八、脚本挂载\+引用绑定清单（新增难度/Blind相关绑定）

|脚本名称|挂载物体|关键引用绑定|
|---|---|---|
|BeatManager|MusicPlayer|拖拽低/中/高难度背景音乐Clip，关联ModeDifficultyManager|
|GameManager|GameManager|关联BeatManager、UIManager、ModeDifficultyManager、HealthSystem、ScoreSystem、AchievementSystem、ItemEffectManager、ObstacleSpawner|
|UIManager|UIRoot|绑定5个页面、所有按钮/文本、Rhythm\_Visual（节奏可视化元素）、BlindTogglePopup（切换弹窗）及弹窗内两个按钮、AchievementPanel（成就展示区）、成就解锁弹窗|
|ModeDifficultyManager|GameManager|关联BeatManager、ScoreSystem、UIManager（同步难度/模式显示）、ItemSpawner、ObstacleSpawner|
|PlayerController|Player|关联BeatManager、JumpSystem、ModeDifficultyManager（获取难度对应的奔跑速度）、ItemEffectManager（接收道具效果）|
|JumpSystem|Player|Ground Layer=Default，绑定Rigidbody2D、BeatManager|
|ItemSpawner|GameManager|拖拽所有道具预制体（5种），关联ModeDifficultyManager（获取难度对应的生成间隔）、BeatManager（联动节拍生成）|
|ItemInteractionSystem|GameManager|Item Layer=Item，关联BeatManager、ScoreSystem、ModeDifficultyManager（判断是否为Blind模式）、ItemEffectManager（触发道具效果）|
|ItemEffectManager|GameManager|关联PlayerController、HealthSystem、ScoreSystem，管理道具效果叠加与失效逻辑|
|FallingItem|ItemPrefab（所有道具预制体）|绑定对应道具Sprite和音效，关联ItemInteractionSystem|
|Obstacle|ObstaclePrefab（所有障碍预制体）|关联HealthSystem、ModeDifficultyManager（获取难度对应的障碍密度），绑定对应障碍Sprite和音效|
|ObstacleSpawner|GameManager|拖拽所有障碍预制体（4种），关联ModeDifficultyManager（获取难度对应的密度和移动速度）、BeatManager（联动节拍生成）|
|HealthSystem|GameManager|关联UIManager、GameManager（触发游戏结束）、ItemEffectManager（接收无敌道具效果）|
|ScoreSystem|GameManager|关联UIManager、ModeDifficultyManager（获取模式加成）、AchievementSystem（同步成就进度）|
|AchievementSystem|GameManager|关联UIManager（显示成就和解锁弹窗）、ScoreSystem、HealthSystem、PlayerController（监测成就解锁条件），同步存档成就数据|

# 九、游戏完整运行流程（新增难度/Blind模式流程）

1. 启动游戏 → 显示\*\*开始主页面\*\*，默认难度“中”，默认模式“Normal”

2. （可选）点击\`Btn\_Difficulty\`选择难度（低/中/高），点击\`Btn\_BlindMode\`切换模式

3. 点击\`Btn\_StartGame\` → 进入\*\*游戏内页\*\*，加载对应难度的背景音乐，玩家自动跑、5种道具/4种障碍按难度\+节拍生成，可视化节奏元素显示（Blind模式隐藏）

4. 游戏内按空格（跳）、S键（蹲）、D键（加速）执行对应功能，均需按节拍判定效果；鼠标点击道具得分/触发功能（Blind模式下得分/效果翻倍），躲避4种不同类型障碍

5. 当游戏积分≥1000分时，\`Txt\_BlindUnlockTip\`显示，\`Btn\_ToggleBlind\`解锁；点击该按钮弹出\`BlindTogglePopup\`，玩家可选择\`Btn\_ConfirmBlind\`（切换模式）或\`Btn\_CancelBlind\`（不切换）

6. 确认切换Blind模式后，立即隐藏可视化节奏元素，得分翻倍；取消则保持原模式，后续可再次点击按钮选择

7. 空格（跳）、S键（蹲）、D键（加速）节拍触发对应功能 \+ 鼠标点击道具得分/触发功能（Blind模式得分/效果翻倍），过程中监测成就解锁条件，解锁后弹出成就提示

8. 碰撞障碍扣血（无敌道具生效时不扣血）→ HP=0 → 自动跳转\*\*结算页面\*\*，显示最终分数、难度、模式及加成，同步显示本局解锁的成就（若有）

9. 结算页可点击重新开始（沿用之前的难度和模式）、返回主页、进入个人资料页

10. 个人资料页可查看所有成就解锁进度、各难度/模式历史最高分；任意页面可跳转\*\*设置页\*\*，设置页可切换解锁的皮肤和音效

# 十、Cursor代码生成指令（直接复制全选发送，新增难度/Blind相关要求）

\`\`\`

基于此《Rhythm Runner 节奏跑酷 完整版设计说明文档》，生成Unity 2D可直接运行的完整C\#代码，严格遵守以下要求：

1\.  严格遵循技术约束：仅用AudioSource\.time做节奏判定，禁止使用Time\.time，所有节奏行为依赖BeatManager

2\.  修复核心BUG：玩家开局不掉落、不穿地、正常落地、空中无法连跳

3\.  实现玩家自动向右匀速奔跑（速度随难度微调）、超长横向地面、摄像机自动跟随；实现3个功能键位（空格=跳、S=蹲、D=加速）控制，每个键位对应独立功能，均需通过节拍判定触发对应效果；实现4种障碍（静态/移动/高空/隐形）、5种道具（基础/功能性）的生成、交互与效果，道具/障碍生成与音乐节拍联动

4\.  完整实现5个页面：MainMenu、GamePlay、GameOver、Profile、Settings，所有固定名称按钮的点击、跳转、功能逻辑全部实现

5\.  完整实现3档音乐难度：低/中/高，按文档固定参数（BPM、判定窗口、生成间隔、障碍密度）实现，支持难度切换并同步更新音乐和游戏参数

7\.  完整实现核心系统：节拍触发功能（3个键位：跳、蹲、加速对应不同效果，适配难度判定窗口）、道具系统（5种道具，含基础/功能性，适配难度和模式，联动节拍生成）、障碍系统（4种障碍，适配难度和键位，联动节拍生成）、障碍扣血（适配难度密度，支持无敌道具免疫）、血量3点上限（可通过成就提升至4点）、分数/连击系统（叠加Blind模式加成）、成就系统（14个成就，含基础/挑战，解锁条件、奖励、弹窗提示、存档同步全部实现）

7\.  完整实现核心系统：节拍跳跃（适配难度判定窗口）、道具随机生成/点击判定（适配难度和模式得分）、障碍扣血（适配难度密度）、血量3点上限、分数/连击系统（叠加Blind模式加成）

8\.  新增ModeDifficultyManager、ItemEffectManager、ObstacleSpawner、AchievementSystem 4个脚本，分别实现难度和Blind模式管理、道具效果管理、障碍生成管理、成就管理，同步更新所有关联系统和UI显示

9\.  代码按模块拆分，明确标注每个脚本的挂载物体、所有引用拖拽绑定方式，包含难度/Blind模式、道具、障碍、成就相关的引用绑定；明确5种道具、4种障碍的预制体命名和功能逻辑，明确14个成就的解锁条件和奖励逻辑

10\. 代码直接可复制粘贴使用，无需修改，完全匹配我搭建的Unity场景（含节奏可视化元素、难度分类音乐、所有按钮和文本命名）

\`\`\`

# 十一、运行前必检清单（100%无报错，新增难度/Blind相关检查项）

- 1\. Player：Rigidbody2D重力=3，冻结Z旋转，Layer=Player

- 2\. Ground：有碰撞、无刚体、Scale\(50,1,1\)，位置在屏幕底部

- 3\. MusicPlayer：已绑定低/中/高难度背景音乐，未勾选自动播放，关联BeatManager

- 4\. 节奏可视化元素：Rhythm\_Visual默认显示，包含Rhythm\_Line和Rhythm\_Dot，命名正确

- 5\. UI：仅MainMenu显示，其余4页隐藏；所有按钮、文本命名与文档一致（含难度/Blind相关、键位相关、成就相关）；Blind模式切换弹窗、成就解锁弹窗及提示文本命名正确、默认隐藏

- 6\. 图层：Player/Item/Obstacle设置正确

- 7\. 预制体：5种道具、4种障碍预制体已创建，命名正确，绑定对应Sprite和音效，已绑定生成器

- 8\. 音乐文件夹：按难度分类（Low/Mid/High），存放对应背景音乐；新增道具/障碍/成就专属音效，存放于Audio/Sound文件夹

- 9\. 脚本：所有14个脚本已挂载到对应物体，所有引用绑定完成（含ModeDifficultyManager、ItemEffectManager、ObstacleSpawner、AchievementSystem的关联）

- 10\. 摄像机Tag=MainCamera，可正常跟随玩家

- 11\. 成就系统：AchievementPanel（成就展示区）已绑定到个人资料页，成就解锁弹窗命名正确，奖励相关逻辑绑定完成

# 十二、文档使用说明

1. 先按\*\*第五部分\*\*在Unity搭建完整场景、UI、预制体（含5种道具、4种障碍、节奏可视化元素），按要求分类存放背景音乐和音效（新增Audio/Sound文件夹存放道具/障碍/成就音效）

2. 复制\*\*第十部分\*\*指令\+本完整文档发给Cursor，生成全部代码（含新增的4个脚本）

3. 按\*\*第八部分\*\*挂载所有14个脚本并绑定引用（重点绑定道具、障碍、成就相关的引用）

4. 按\*\*第十一部分\*\*检查无误后，点击运行 → 直接玩完整游戏（支持难度选择、Blind模式切换、道具/障碍多样性、成就解锁）

5. 复制\*\*第十部分\*\*指令\+本完整文档发给Cursor，生成全部代码

6. 按\*\*第八部分\*\*挂载所有脚本并绑定引用（重点绑定难度/Blind模式相关的引用）

7. 按\*\*第十一部分\*\*检查无误后，点击运行 → 直接玩完整游戏（支持难度选择、Blind模式切换）

> （注：文档部分内容可能由 AI 生成）
