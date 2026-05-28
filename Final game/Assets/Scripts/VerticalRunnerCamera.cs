using UnityEngine;

public class VerticalRunnerCamera : MonoBehaviour
{
    private Transform target;
    private float minY;

    public void Follow(Transform target)
    {
        this.target = target;
        if (Camera.main != null)
        {
            minY = Camera.main.transform.position.y;
        }
    }

    public void Tick()
    {
        Camera camera = Camera.main;
        if (camera == null || target == null)
        {
            return;
        }

        camera.orthographic = true;
        camera.orthographicSize = 5f;
        Vector3 position = camera.transform.position;
        float desiredY = Mathf.Max(minY, target.position.y + 1.35f);
        position.y = Mathf.Lerp(position.y, desiredY, Time.deltaTime * 3.2f);
        position.x = Mathf.Lerp(position.x, 0f, Time.deltaTime * 2.5f);
        position.z = -10f;
        camera.transform.position = position;
        minY = Mathf.Max(minY, position.y);
    }
}
