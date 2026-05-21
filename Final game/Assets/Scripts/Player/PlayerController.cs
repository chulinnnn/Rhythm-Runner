using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

    private Animator animator;
    private Rigidbody2D rigidbody;
    private BoxCollider2D boxCollider;
    //private GameManager gameManager;

    private Vector2 boxSize;

    public float jumpForce = 5f;
    private bool IsGround = true;
    private bool canJump = true;

    private bool isJiasuMode = false;
    private Coroutine jiasuCoroutine;

    private bool isMagnetActive = false;
    private Coroutine magnetCoroutine;

    private const float MagnetDuration = 10f;
    private const float MagnetRadius = 20f;
    private const float MagnetPullSpeed = 15f;

    private float savedGravityScale;
    private bool savedColliderTrigger;

    private const float JiasuDuration = 5f;
    private const float JiasuFixedY = 2f;
    private const float JiasuSpeedMultiplier = 2f;
    private const float JiasuTargetX = -4.5f;

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        //gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        boxSize = boxCollider.size;
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
        if (Input.GetKeyDown(KeyCode.UpArrow) && canJump)
        {
            rigidbody.velocity = Vector2.up * jumpForce;
            animator.SetBool("Jump", true);
            IsGround = false;
            canJump = false;
            animator.SetBool("DoubleJump", false);
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
            GameManager.Instance.GameOver();
        }
        else
        {
            transform.position = Vector2.Lerp(transform.position, new Vector2(JiasuTargetX, transform.position.y), Time.deltaTime);
        }
        if (transform.position.y <= -6.5f)
        {
            GameManager.Instance.GameOver();
        }
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
            canJump = true;
            rigidbody.velocity = Vector2.up * jumpForce;
            animator.SetBool("Jump", true);
            animator.SetBool("DoubleJump",true);
            GameObject.Destroy(coll.transform.parent.gameObject);
        }

        if (coll.gameObject.tag == "EnemyBarrier")
        {
            GameManager.Instance.GameOver();
        }

        if (coll.gameObject.tag == "jiasu")
        {
            PickUpJiasu(coll.gameObject);
        }

        if (coll.gameObject.tag == "xt")
        {
            PickUpXt(coll.gameObject);
        }

        //if (coll.gameObject.tag == "Surfboard")
        //{
        //    animator.SetBool("Slide", true);
        //}
    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        if (coll.gameObject.tag == "Bonus1")
        {
            SoundManager.PlaySFX("jinbi");
            GameManager.Instance.UpdateBonus(1);
            Destroy(coll.gameObject);
        }
        if (coll.gameObject.tag == "Bonus2")
        {
            GameManager.Instance.UpdateBonus(5);
            //gameManager.UpdateBonus(5);
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
        GameManager.Instance.speedMultiplier = JiasuSpeedMultiplier;

        yield return new WaitForSeconds(JiasuDuration);

        isJiasuMode = false;
        rigidbody.gravityScale = savedGravityScale;
        boxCollider.isTrigger = savedColliderTrigger;
        GameManager.Instance.speedMultiplier = 1f;
        jiasuCoroutine = null;
    }
}
