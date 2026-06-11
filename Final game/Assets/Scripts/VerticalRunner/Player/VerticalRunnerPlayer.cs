using UnityEngine;

/// <summary>
/// Handles the live player object for VerticalRunner: keyboard input, jump movement, pickup collection,
/// branch dodging, collision feedback, and recovery placement.
/// </summary>
/// <remarks>
/// 中文：这是 VerticalRunner 里真正控制玩家角色的脚本。它负责读取键盘、执行跳跃、
/// 抓香蕉、处理鹦鹉分支、碰撞和失败后的回到安全平台。节拍判定和分数仍由
/// <see cref="VerticalRunnerManager"/> 负责。
/// 核心是 Tick() 驱动的输入-动作循环；
/// 最有价值的设计是 Player 只管「做什么」，Manager 只管「做得对不对」，
/// </remarks>
public class VerticalRunnerPlayer : MonoBehaviour
{
    private VerticalRunnerManager manager;
    private VerticalRunnerSettings settings;
    private VerticalBeatSpawner spawner;
    private Sprite circleSprite;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rigidbody2d;
    private VerticalRunnerPlatform currentPlatform;
    private VerticalRunnerPlatform targetPlatform;
    private VerticalRunnerPickup promptedPickup;
    private Vector3 jumpStart;
    private Vector3 jumpTarget;
    private float jumpTimer;
    private float jumpDuration;
    private bool jumping;
    private bool inputLocked;
    private bool parrotRecoveryJump;
    private int missedJumpBeat = -1;
    private int missedParrotBeat = -1;

    /// <summary>
    /// Gets the platform the player is currently standing on.
    /// </summary>
    /// <remarks>
    /// 中文：当前玩家脚下的平台。Manager 会用它判断下一步跳跃、香蕉和鹦鹉提示。
    /// </remarks>
    public VerticalRunnerPlatform CurrentPlatform { get { return currentPlatform; } }

    /// <summary>
    /// Gets the platform the player is currently jumping toward, if a jump is active.
    /// </summary>
    /// <remarks>
    /// 中文：玩家正在跳向的目标平台；没有跳跃时通常为空。
    /// </remarks>
    public VerticalRunnerPlatform TargetPlatform { get { return targetPlatform; } }

    /// <summary>
    /// Convenience build overload used when no scene template container is supplied.
    /// </summary>
    /// <remarks>
    /// 中文：没有传入模板容器时使用的 Build 简化入口。
    /// </remarks>
    public void Build(VerticalRunnerManager manager, VerticalRunnerSettings settings, VerticalBeatSpawner spawner, Sprite circleSprite)
    {
        Build(manager, settings, spawner, circleSprite, null);
    }

    /// <summary>
    /// Initializes the runtime player with manager/settings/spawner references, visuals, collider, rigidbody,
    /// and starting platform placement.
    /// </summary>
    /// <remarks>
    /// 中文：初始化玩家。它会绑定 Manager、设置、路线生成器，配置 SpriteRenderer、
    /// Collider、Rigidbody，并把玩家放到 startBeat 对应的平台上。若场景里有 playerTemplate，
    /// 会尽量保留模板图片和样式。
    /// </remarks>
    public void Build(VerticalRunnerManager manager, VerticalRunnerSettings settings, VerticalBeatSpawner spawner, Sprite circleSprite, VerticalRunnerTemplates templates)
    {
        this.manager = manager;
        this.settings = settings;
        this.spawner = spawner;
        this.circleSprite = circleSprite;
        bool preserveTemplateVisual = templates != null && templates.playerTemplate != null;

        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        if (!preserveTemplateVisual)
        {
            spriteRenderer.sprite = circleSprite;
            spriteRenderer.color = settings.playerColor;
            spriteRenderer.sortingOrder = 5;
            transform.localScale = new Vector3(0.48f, 0.48f, 1f);
        }
        else if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = circleSprite;
        }

        CircleCollider2D collider = gameObject.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.48f;
        }
        collider.isTrigger = true;

        rigidbody2d = gameObject.GetComponent<Rigidbody2D>();
        if (rigidbody2d == null)
        {
            rigidbody2d = gameObject.AddComponent<Rigidbody2D>();
        }
        rigidbody2d.bodyType = RigidbodyType2D.Kinematic;
        rigidbody2d.gravityScale = 0f;
        rigidbody2d.freezeRotation = true;

        currentPlatform = spawner.GetPlatformForBeat(settings.startBeat);
        if (currentPlatform != null)
        {
            SnapToPlatform(currentPlatform);
        }
    }

    /// <summary>
    /// Enables or disables player input while countdowns, modals, recovery, or run-ending states are active.
    /// </summary>
    /// <remarks>
    /// 中文：锁定或解锁玩家输入。倒计时、弹窗、失败恢复、结束界面时会锁住输入。
    /// </remarks>
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }

    /// <summary>
    /// Runs the per-frame player loop for missed-action checks, keyboard input, movement, falling checks,
    /// and nearby pickup hint updates.
    /// </summary>
    /// <remarks>
    /// 中文：这是 Player 最核心的每帧函数。它读取真实键盘输入，尝试跳跃/抓香蕉/躲鹦鹉，
    /// 更新跳跃位置，检测掉落，并提示附近香蕉。视觉 icon 提示不由这里控制。
    /// </remarks>
    public void Tick()
    {
        // This is the real input loop. Visual prompts are driven by Manager and should not depend on these key reads.
        // 这里是真实输入循环。视觉提示由 Manager 驱动，不应依赖这些按键读取。
        if (!inputLocked && !jumping)
        {
            CheckMissedActions();
            if (!manager.CanContinueRun)
            {
                return;
            }
        }

        if (!inputLocked && !jumping && TryReadDirectionalChoice(out VerticalBranchChoice choice))
        {
            TryDirectionalDodge(choice);
        }
        if (!inputLocked && !jumping && Input.GetKeyDown(KeyCode.Space))
        {
            TryBeatJump();
        }
        if (!inputLocked && !jumping && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
        {
            TryCollectNearbyCoin();
        }

        if (jumping)
        {
            UpdateJump();
        }
        else if (currentPlatform != null)
        {
            Vector3 target = currentPlatform.transform.position + new Vector3(0f, 0.48f, 0f);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 7f);
        }

        if (!inputLocked && transform.position.y < manager.CameraBottomY - 1.4f)
        {
            manager.TakeDamage("Fall", "Space");
        }

        UpdateCoinPrompt();
    }

    /// <summary>
    /// Cancels active movement state and snaps the player back to the current or nearest safe platform.
    /// </summary>
    /// <remarks>
    /// 中文：失败恢复用。它会清掉跳跃和提示状态，然后把玩家放回当前平台或最近平台。
    /// </remarks>
    public void RecoverToSafePlatform()
    {
        jumping = false;
        targetPlatform = null;
        promptedPickup = null;
        parrotRecoveryJump = false;
        missedJumpBeat = -1;
        missedParrotBeat = -1;
        VerticalRunnerPlatform safe = currentPlatform != null ? currentPlatform : spawner.GetNearestPlatform(transform.position);
        if (safe == null)
        {
            safe = spawner.GetPlatformForBeat(settings.startBeat);
        }
        SnapToPlatform(safe);
    }

    /// <summary>
    /// Attempts a Space jump by finding the next platform and asking the manager to judge the rhythm timing.
    /// </summary>
    /// <remarks>
    /// 中文：处理 Space 跳跃。这里只负责找下一个平台和发起跳跃；是否踩准节拍由 Manager 判定。
    /// 如果当前平台需要鹦鹉分支选择，则不会普通跳，而是提示方向操作。
    /// </remarks>
    private void TryBeatJump()
    {
        if (jumping)
        {
            return;
        }

        if (currentPlatform != null && currentPlatform.requiresDirectionalChoice)
        {
            manager.ShowDirectionalChoiceHint();
            return;
        }

        VerticalRunnerPlatform next = currentPlatform != null && currentPlatform.defaultNext != null
            ? currentPlatform.defaultNext
            : currentPlatform != null ? spawner.GetNextPlatformAfterBeat(currentPlatform.beatIndex) : spawner.GetNearestPlatform(transform.position);
        if (next == null)
        {
            manager.CompleteRun();
            return;
        }

        RhythmTimingResult result;
        if (!manager.ReportJumpInput(next, out result))
        {
            return;
        }

        StartJump(next, result);
    }

    /// <summary>
    /// Reads a left or right directional key press for parrot branch choices.
    /// </summary>
    /// <remarks>
    /// 中文：读取左/右方向输入。LeftArrow/A 表示左，RightArrow/D 表示右。
    /// </remarks>
    private bool TryReadDirectionalChoice(out VerticalBranchChoice choice)
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            choice = VerticalBranchChoice.Left;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            choice = VerticalBranchChoice.Right;
            return true;
        }

        choice = VerticalBranchChoice.None;
        return false;
    }

    /// <summary>
    /// Attempts a parrot branch dodge by checking the selected branch and asking the manager to judge timing.
    /// </summary>
    /// <remarks>
    /// 中文：处理鹦鹉分支。这里读取是否按住 Space，并把方向、当前平台和节拍判定交给 Manager。
    /// 成功后跳向安全方向；失败只显示失败反馈，不直接改变判定规则。
    /// </remarks>
    private void TryDirectionalDodge(VerticalBranchChoice choice)
    {
        if (jumping || currentPlatform == null || !currentPlatform.requiresDirectionalChoice)
        {
            return;
        }

        bool spaceHeld = Input.GetKey(KeyCode.Space);

        VerticalRunnerPlatform next = choice == VerticalBranchChoice.Left ? currentPlatform.leftNext : currentPlatform.rightNext;
        if (next == null)
        {
            manager.TakeDamage("Parrot", "Avoid parrot");
            return;
        }

        RhythmTimingResult result;
        if (manager.ReportDirectionalDodge(currentPlatform, choice, spaceHeld, out result))
        {
            StartJump(next, result, false);
            return;
        }

    }

    /// <summary>
    /// Starts a jump arc toward the target platform using BPM-based duration and optional feedback.
    /// </summary>
    /// <remarks>
    /// 中文：开始一次跳跃动画。它记录起点、终点、跳跃时长和是否是鹦鹉恢复跳。
    /// 真正的位置移动在 UpdateJump 里逐帧完成。
    /// </remarks>
    private void StartJump(VerticalRunnerPlatform next, RhythmTimingResult result, bool showJumpFeedback = true, bool landsOnParrot = false)
    {
        targetPlatform = next;
        jumpStart = transform.position;
        jumpTarget = targetPlatform.transform.position + new Vector3(0f, 0.48f, 0f);
        jumpDuration = (60f / Mathf.Max(1f, settings.bpm)) * settings.jumpDurationBeats;
        if (targetPlatform.longJump)
        {
            jumpDuration *= 1.15f;
        }
        jumpTimer = 0f;
        jumping = true;
        parrotRecoveryJump = landsOnParrot;
        if (showJumpFeedback)
        {
            manager.ShowJumpFeedback(result, targetPlatform.longJump);
        }
    }

    /// <summary>
    /// Attempts to collect the nearest banana pickup and asks the manager to judge the action beat timing.
    /// </summary>
    /// <remarks>
    /// 中文：处理 Down/S 抓香蕉。先找附近香蕉，再交给 Manager 判断动作拍是否正确。
    /// 判定成功后才真正收集。
    /// </remarks>
    private void TryCollectNearbyCoin()
    {
        VerticalRunnerPickup pickup = spawner.GetNearestCollectibleCoin(transform.position, settings.coinCollectRadius);
        if (pickup == null)
        {
            return;
        }

        RhythmTimingResult result;
        if (!manager.ReportCoinInput(pickup, out result))
        {
            return;
        }

        CollectPickup(pickup);
    }

    /// <summary>
    /// Marks a pickup as collected, hides it, clears prompt state, and reports the reward to the manager.
    /// </summary>
    /// <remarks>
    /// 中文：真正收集香蕉。设置 collected、隐藏物体、清除当前提示，并通知 Manager 加数量和分数。
    /// </remarks>
    private void CollectPickup(VerticalRunnerPickup pickup)
    {
        if (pickup == null || pickup.collected)
        {
            return;
        }

        pickup.collected = true;
        pickup.gameObject.SetActive(false);
        if (promptedPickup == pickup)
        {
            promptedPickup = null;
        }
        manager.CollectCoin(pickup.value);
    }

    /// <summary>
    /// Shows a banana input hint when a new collectible pickup enters the collection radius.
    /// </summary>
    /// <remarks>
    /// 中文：当玩家靠近一个新的香蕉时，通知 UI 显示 Down/S 提示。
    /// 这里只是提示，不代表已经收集。
    /// </remarks>
    private void UpdateCoinPrompt()
    {
        if (inputLocked)
        {
            promptedPickup = null;
            return;
        }

        VerticalRunnerPickup pickup = spawner.GetNearestCollectibleCoin(transform.position, settings.coinCollectRadius);
        if (pickup != null && pickup != promptedPickup)
        {
            promptedPickup = pickup;
            manager.ShowCoinCollectHint();
        }
        else if (pickup == null)
        {
            promptedPickup = null;
        }
    }

    /// <summary>
    /// Advances the active jump arc and reports landing or parrot recovery damage when the arc completes.
    /// </summary>
    /// <remarks>
    /// 中文：逐帧更新跳跃弧线。跳完后落到目标平台，并通知 Manager；
    /// 如果这是撞鹦鹉后的恢复跳，则落地后触发鹦鹉失败反馈。
    /// </remarks>
    private void UpdateJump()
    {
        jumpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(jumpTimer / Mathf.Max(0.05f, jumpDuration));
        Vector3 position = Vector3.Lerp(jumpStart, jumpTarget, t);
        float arc = Mathf.Sin(t * Mathf.PI) * (targetPlatform != null && targetPlatform.longJump ? 1.2f : 0.72f);
        transform.position = position + new Vector3(0f, arc, 0f);

        if (t >= 1f)
        {
            jumping = false;
            if (targetPlatform != null)
            {
                VerticalRunnerPlatform landedPlatform = targetPlatform;
                targetPlatform = null;
                if (parrotRecoveryJump)
                {
                    parrotRecoveryJump = false;
                    manager.TakeDamage("Parrot", "Avoid parrot");
                    return;
                }

                currentPlatform = landedPlatform;
                manager.ReportPlatformLanded(currentPlatform);
            }
        }
    }

    /// <summary>
    /// Instantly places the player on a platform and clears active jump/miss tracking state.
    /// </summary>
    /// <remarks>
    /// 中文：直接把玩家放到某个平台上，通常用于开局或失败恢复。
    /// 同时清空跳跃目标、鹦鹉恢复状态和已记录 miss 的 beat。
    /// </remarks>
    private void SnapToPlatform(VerticalRunnerPlatform platform)
    {
        currentPlatform = platform;
        targetPlatform = null;
        jumping = false;
        parrotRecoveryJump = false;
        missedJumpBeat = -1;
        missedParrotBeat = -1;
        if (platform != null)
        {
            transform.position = platform.transform.position + new Vector3(0f, 0.48f, 0f);
        }
    }

    /// <summary>
    /// Detects missed bananas, missed parrot actions, and missed jump beats after their beat windows pass.
    /// </summary>
    /// <remarks>
    /// 中文：检查玩家是否漏掉该做的动作。香蕉、鹦鹉、跳跃各自过了对应拍子后，
    /// 会通知 Manager 记录 miss。漏跳是允许发生的，但会计入 miss。
    /// </remarks>
    private void CheckMissedActions()
    {
        if (currentPlatform == null)
        {
            return;
        }

        VerticalRunnerPickup pickup = spawner.GetMissedCollectibleCoin(transform.position, settings.coinCollectRadius, manager.CurrentBeatPosition, settings.actionWindowBeats);
        if (pickup != null)
        {
            manager.ReportMissedPickup(pickup);
        }

        if (currentPlatform.requiresDirectionalChoice && currentPlatform.actionBeatIndex >= 0 && currentPlatform.actionBeatIndex != missedParrotBeat && manager.HasPassedBeatWindow(currentPlatform.actionBeatIndex))
        {
            missedParrotBeat = currentPlatform.actionBeatIndex;
            manager.ReportMissedParrot(currentPlatform);
            return;
        }

        VerticalRunnerPlatform next = currentPlatform.defaultNext != null ? currentPlatform.defaultNext : spawner.GetNextPlatformAfterBeat(currentPlatform.beatIndex);
        if (next != null && next.beatIndex != missedJumpBeat && manager.HasPassedBeatWindow(next.beatIndex))
        {
            missedJumpBeat = next.beatIndex;
            manager.ReportMissedJump(next);
        }
    }

    /// <summary>
    /// Handles trigger contact with pickups and obstacles.
    /// </summary>
    /// <remarks>
    /// 中文：处理碰撞触发。碰到香蕉时显示抓取提示；碰到鹦鹉障碍时触发失败反馈。
    /// </remarks>
    private void OnTriggerEnter2D(Collider2D other)
    {
        VerticalRunnerPickup pickup = other.GetComponent<VerticalRunnerPickup>();
        if (pickup != null && !pickup.collected)
        {
            manager.ShowCoinCollectHint();
            promptedPickup = pickup;
            return;
        }

        VerticalRunnerObstacle obstacle = other.GetComponent<VerticalRunnerObstacle>();
        if (obstacle != null)
        {
            if (parrotRecoveryJump)
            {
                return;
            }

            manager.TakeDamage("Parrot", "Avoid parrot");
        }
    }
}
