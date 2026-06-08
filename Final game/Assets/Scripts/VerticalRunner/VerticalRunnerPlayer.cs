using UnityEngine;

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

    public VerticalRunnerPlatform CurrentPlatform { get { return currentPlatform; } }

    public void Build(VerticalRunnerManager manager, VerticalRunnerSettings settings, VerticalBeatSpawner spawner, Sprite circleSprite)
    {
        Build(manager, settings, spawner, circleSprite, null);
    }

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

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }

    public void Tick()
    {
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
