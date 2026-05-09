using UnityEngine;

/// <summary>
/// Triggers an instant game over when the target object touches this obstacle.
/// Supports both trigger collisions and regular 2D collisions.
/// </summary>
public class ObstacleGameOverOnTouch : MonoBehaviour
{
    [Tooltip("Only objects with this tag can trigger game over.")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("If true, destroy the touching target after triggering game over.")]
    [SerializeField] private bool destroyTargetOnHit = true;

    [Tooltip("If true, this obstacle also destroys itself after hit.")]
    [SerializeField] private bool destroySelfOnHit = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerGameOver(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTriggerGameOver(collision.gameObject);
    }

    private void TryTriggerGameOver(GameObject other)
    {
        if (!other.CompareTag(targetTag))
            return;

        if (GameManager.instance != null && !GameManager.instance.gameIsOver)
        {
            GameManager.instance.GameOver();
        }

        if (destroyTargetOnHit)
        {
            Destroy(other);
        }

        if (destroySelfOnHit)
        {
            Destroy(gameObject);
        }
    }
}
