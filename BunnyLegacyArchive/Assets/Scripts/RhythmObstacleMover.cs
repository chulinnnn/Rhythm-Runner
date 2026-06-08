using UnityEngine;

public class RhythmObstacleMover : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float destroyX = -20f;

    void Update()
    {
        float speed = moveSpeed;
        if (GameManager.Instance != null)
        {
            speed *= GameManager.Instance.speedMultiplier;
        }

        transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);

        if (transform.position.x <= destroyX)
        {
            Destroy(gameObject);
        }
    }
}
