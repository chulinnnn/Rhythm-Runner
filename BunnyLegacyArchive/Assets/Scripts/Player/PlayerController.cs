using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour {

    public event System.Action<string, RhythmTimingResult> RhythmInputReported;
    public event System.Action JellyBounceUsed;
    public event System.Action<string, bool> RhythmCollectibleCollected;

    private Animator animator;
    private Rigidbody2D rigidbody;
    private BoxCollider2D boxCollider;
    //private GameManager gameManager;

    private Vector2 boxSize;

    public float jumpForce = 5f;
    public float goodJumpMultiplier = 1.05f;
    public float perfectJumpMultiplier = 1.12f;
    private bool IsGround = true;
    private bool canJump = true;

    private bool isJiasuMode = false;
    private Coroutine jiasuCoroutine;

    private bool isMagnetActive = false;
    private Coroutine magnetCoroutine;
    private RhythmTimingResult lastRhythmResult = RhythmTimingResult.None;
    private float lastRhythmInputTime = -10f;

    private const float MagnetDuration = 10f;
    private const float MagnetRadius = 20f;
    private const float MagnetPullSpeed = 15f;

    private float savedGravityScale;
    private bool savedColliderTrigger;

    private const float JiasuDuration = 5f;
    private const float JiasuFixedY = 2f;
    private const float JiasuSpeedMultiplier = 2f;
    private const float JiasuTargetX = -4.5f;
    private const float DefaultJumpForce = 5f;

    private bool IsTutorialScene
    {
        get { return SceneManager.GetActiveScene().name == "Tutorial"; }
    }

	// Use this for initialization
	void Start () {
        EnsureComponents();
	}

    private void EnsureComponents()
    {
        if (animator != null && rigidbody != null && boxCollider != null && boxSize != Vector2.zero)
        {
            return;
        }

        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        //gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        if (jumpForce <= 0f)
        {
            jumpForce = DefaultJumpForce;
        }

        if (boxCollider != null)
        {
            boxSize = boxCollider.size;
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (isMagnetActive)
        {
            AttractNearbyCoins();
        }

        if (isJiasuMode)
        {
            UpdateJiasuMode();
            return;
        }

        //float v = Input.GetAxis("Vertical");

        //控制跳跃
        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) && canJump)
        {
            Jump();
        }


        if (Input.GetKeyDown(KeyCode.DownArrow) && IsGround)
        {
            ReportRhythmInput("Slide");
        }

        if (Input.GetKey(KeyCode.DownArrow) && IsGround)
        {
            animator.SetBool("Slide", true);
            boxCollider.size = new Vector2(1, 0.5f);

        }
        else
        {
            animator.SetBool("Slide", false);
            boxCollider.size = boxSize;
        }

        if (transform.position.x <= -6.5f)
        {
            TriggerGameOver();
        }
        else
        {
            transform.position = Vector2.Lerp(transform.position, new Vector2(JiasuTargetX, transform.position.y), Time.deltaTime);
        }
        if (transform.position.y <= -6.5f)
        {
            TriggerGameOver();
        }
    }

    private void Jump()
    {
        RhythmTimingResult timing = ReportRhythmInput("Jump");
        float rhythmJumpForce = GetRhythmJumpForce(timing);

        rigidbody.velocity = new Vector2(rigidbody.velocity.x, rhythmJumpForce);
        animator.SetBool("Jump", true);
        animator.SetBool("DoubleJump", false);
        animator.SetBool("Slide", false);
        boxCollider.size = boxSize;

        IsGround = false;
        canJump = false;
    }

    private RhythmTimingResult ReportRhythmInput(string actionName)
    {
        if (RhythmManager.Instance == null)
        {
            return RhythmTimingResult.None;
        }

        RhythmTimingResult result = RhythmManager.Instance.ReportInput(actionName);
        lastRhythmResult = result;
        lastRhythmInputTime = Time.time;
        if (RhythmInputReported != null)
        {
            RhythmInputReported(actionName, result);
        }
        return result;
    }

    private float GetRhythmJumpForce(RhythmTimingResult timing)
    {
        if (timing == RhythmTimingResult.Perfect)
        {
            return jumpForce * perfectJumpMultiplier;
        }

        if (timing == RhythmTimingResult.Good)
        {
            return jumpForce * goodJumpMultiplier;
        }

        return jumpForce;
    }

    private void UpdateJiasuMode()
    {
        rigidbody.velocity = Vector2.zero;
        float x = Mathf.Lerp(transform.position.x, JiasuTargetX, Time.deltaTime);
        transform.position = new Vector3(x, JiasuFixedY, transform.position.z);

        animator.SetBool("Jump", false);
        animator.SetBool("DoubleJump", false);
        animator.SetBool("Slide", false);
        boxCollider.size = boxSize;
    }

    public void OnCollisionEnter2D(Collision2D coll)
    {
        if (isJiasuMode)
        {
            if (coll.gameObject.tag == "jiasu")
            {
                PickUpJiasu(coll.gameObject);
            }
            if (coll.gameObject.tag == "xt")
            {
                PickUpXt(coll.gameObject);
            }
            return;
        }

        if (coll.gameObject.tag == "Floor")
        {
            canJump = true;
            IsGround = true;
            animator.SetBool("Jump", false);
            animator.SetBool("DoubleJump", false);
        }

        if (coll.gameObject.tag == "UpCollider")
        {
            if (IsTutorialScene)
            {
                return;
            }

            canJump = true;
            rigidbody.velocity = Vector2.up * jumpForce;
            animator.SetBool("Jump", true);
            animator.SetBool("DoubleJump",true);
            if (JellyBounceUsed != null)
            {
                JellyBounceUsed();
            }
            GameObject.Destroy(coll.transform.parent.gameObject);
        }

        if (coll.gameObject.tag == "EnemyBarrier")
        {
            TriggerGameOver();
        }

        if (coll.gameObject.tag == "jiasu")
        {
            if (IsTutorialScene)
            {
                return;
            }

            PickUpJiasu(coll.gameObject);
        }

        if (coll.gameObject.tag == "xt")
        {
            if (IsTutorialScene)
            {
                return;
            }

            PickUpXt(coll.gameObject);
        }

        //if (coll.gameObject.tag == "Surfboard")
        //{
        //    animator.SetBool("Slide", true);
        //}
    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        if (IsTutorialScene && IsLegacyTutorialItemTag(coll.gameObject.tag))
        {
            return;
        }

        if (coll.gameObject.tag == "Bonus1")
        {
            SoundManager.PlaySFX("jinbi");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateBonus(1);
            }
            ReportRhythmCollectible(coll.gameObject.tag);
            Destroy(coll.gameObject);
        }
        if (coll.gameObject.tag == "Bonus2")
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateBonus(5);
            }
            //gameManager.UpdateBonus(5);
            ReportRhythmCollectible(coll.gameObject.tag);
            Destroy(coll.gameObject);
        }
        if (coll.gameObject.tag == "jiasu")
        {
            PickUpJiasu(coll.gameObject);
        }
        if (coll.gameObject.tag == "xt")
        {
            PickUpXt(coll.gameObject);
        }
    }

    private void ReportRhythmCollectible(string collectibleTag)
    {
        bool wasOnBeat = IsRecentRhythmSuccess();
        if (RhythmCollectibleCollected != null)
        {
            RhythmCollectibleCollected(collectibleTag, wasOnBeat);
        }
    }

    private void TriggerGameOver()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    private bool IsLegacyTutorialItemTag(string tagName)
    {
        return tagName == "Bonus1"
            || tagName == "Bonus2"
            || tagName == "jiasu"
            || tagName == "xt"
            || tagName == "UpCollider";
    }

    private bool IsRecentRhythmSuccess()
    {
        bool success = lastRhythmResult == RhythmTimingResult.Perfect || lastRhythmResult == RhythmTimingResult.Good;
        return success && Time.time - lastRhythmInputTime <= 0.4f;
    }

    public void ResetForTutorial(Vector3 startPosition)
    {
        EnsureComponents();

        if (jiasuCoroutine != null)
        {
            StopCoroutine(jiasuCoroutine);
            jiasuCoroutine = null;
        }

        if (magnetCoroutine != null)
        {
            StopCoroutine(magnetCoroutine);
            magnetCoroutine = null;
        }

        isJiasuMode = false;
        isMagnetActive = false;
        canJump = true;
        IsGround = true;
        lastRhythmResult = RhythmTimingResult.None;
        lastRhythmInputTime = -10f;

        if (rigidbody != null)
        {
            rigidbody.gravityScale = savedGravityScale > 0f ? savedGravityScale : rigidbody.gravityScale;
            rigidbody.velocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
        }

        if (boxCollider != null)
        {
            boxCollider.isTrigger = savedColliderTrigger;
            boxCollider.size = boxSize;
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("Jump", false);
            animator.SetBool("DoubleJump", false);
            animator.SetBool("Slide", false);
        }

        transform.position = startPosition;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.speedMultiplier = 1f;
        }
    }

    private void AttractNearbyCoins()
    {
        GameObject[] coins = GameObject.FindGameObjectsWithTag("Bonus1");
        Vector3 playerPos = transform.position;

        foreach (GameObject coin in coins)
        {
            if (coin == null)
            {
                continue;
            }

            Vector3 coinPos = coin.transform.position;
            if (Vector3.Distance(coinPos, playerPos) > MagnetRadius)
            {
                continue;
            }

            coin.transform.position = Vector3.MoveTowards(
                coinPos, playerPos, MagnetPullSpeed * Time.deltaTime);
        }
    }

    private void PickUpXt(GameObject item)
    {
        Destroy(item);
        if (magnetCoroutine != null)
        {
            StopCoroutine(magnetCoroutine);
        }
        magnetCoroutine = StartCoroutine(MagnetPowerUpRoutine());
    }

    private IEnumerator MagnetPowerUpRoutine()
    {
        isMagnetActive = true;
        yield return new WaitForSeconds(MagnetDuration);
        isMagnetActive = false;
        magnetCoroutine = null;
    }

    private void PickUpJiasu(GameObject item)
    {
        Destroy(item);
        if (jiasuCoroutine != null)
        {
            StopCoroutine(jiasuCoroutine);
        }
        jiasuCoroutine = StartCoroutine(JiasuPowerUpRoutine());
    }

    private IEnumerator JiasuPowerUpRoutine()
    {
        if (!isJiasuMode)
        {
            savedGravityScale = rigidbody.gravityScale;
            savedColliderTrigger = boxCollider.isTrigger;
        }

        isJiasuMode = true;
        rigidbody.gravityScale = 0f;
        rigidbody.velocity = Vector2.zero;
        boxCollider.isTrigger = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.speedMultiplier = JiasuSpeedMultiplier;
        }

        yield return new WaitForSeconds(JiasuDuration);

        isJiasuMode = false;
        rigidbody.gravityScale = savedGravityScale;
        boxCollider.isTrigger = savedColliderTrigger;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.speedMultiplier = 1f;
        }
        jiasuCoroutine = null;
    }
}
