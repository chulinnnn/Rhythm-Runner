using System.Collections.Generic;
using UnityEngine;

public enum TutorialActionType
{
    None,
    Jump,
    Slide,
    RhythmCoin,
    BeatBoost,
    PulseMagnet
}

public class TutorialSpawnedObject : MonoBehaviour
{
    public TutorialActionType actionType;
    public int beatIndex;
}

public class TutorialBeatSpawner : MonoBehaviour
{
    public GameObject barrierPrefab;
    public GameObject coinPrefab;
    public GameObject beatBoostPrefab;
    public GameObject pulseMagnetPrefab;
    public float bpm = 126f;
    public float playerMeetX = -4.5f;
    public float moveSpeed = 8f;
    public float groundY = -2.35f;
    public float pickupY = -1.35f;
    public float pickupTriggerRadius = 0.72f;
    public float spawnZ = 0f;
    public bool logSpawns = true;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private Sprite fallbackSprite;

    public void Configure(float tutorialBpm, float tutorialMoveSpeed, float meetX)
    {
        bpm = Mathf.Max(1f, tutorialBpm);
        moveSpeed = Mathf.Max(0.1f, tutorialMoveSpeed);
        playerMeetX = meetX;
        ResolveGroundY();
        ResolvePickupY();
        ResolveDefaultPrefabs();
    }

    public void ClearAll()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();
    }

    public Transform Spawn(TutorialActionType actionType, int beatIndex, string stepName)
    {
        GameObject obj = null;
        Vector3 position = new Vector3(GetWorldXForBeat(beatIndex), groundY, spawnZ);

        if (actionType == TutorialActionType.Jump)
        {
            obj = SpawnPrefabOrFallback(barrierPrefab, position + new Vector3(0f, 0.45f, 0f), Color.red, "EnemyBarrier", new Vector2(0.75f, 0.75f), false);
        }
        else if (actionType == TutorialActionType.Slide)
        {
            obj = CreateSlideGate(position + new Vector3(0f, 1.25f, 0f));
        }
        else if (actionType == TutorialActionType.RhythmCoin)
        {
            obj = SpawnPickup(coinPrefab, new Vector3(position.x, pickupY, spawnZ), new Color(1f, 0.78f, 0.12f), "COIN", actionType, beatIndex, 1);
        }
        else if (actionType == TutorialActionType.BeatBoost)
        {
            obj = SpawnPickup(beatBoostPrefab, new Vector3(position.x, pickupY, spawnZ), new Color(0.25f, 0.92f, 1f), "BOOST", actionType, beatIndex, 2);
        }
        else if (actionType == TutorialActionType.PulseMagnet)
        {
            obj = SpawnPickup(pulseMagnetPrefab, new Vector3(position.x, pickupY, spawnZ), new Color(0.72f, 0.38f, 1f), "MAGNET", actionType, beatIndex, 2);
        }

        if (obj == null)
        {
            Debug.LogWarning("TutorialBeatSpawner: Failed to spawn " + actionType + " at beat " + beatIndex + ".");
            return null;
        }

        obj.name = "Tutorial_" + stepName + "_Beat_" + beatIndex;
        TutorialSpawnedObject marker = obj.GetComponent<TutorialSpawnedObject>();
        if (marker == null)
        {
            marker = obj.AddComponent<TutorialSpawnedObject>();
        }

        marker.actionType = actionType;
        marker.beatIndex = beatIndex;

        RhythmObstacleMover mover = obj.GetComponent<RhythmObstacleMover>();
        if (mover == null)
        {
            mover = obj.AddComponent<RhythmObstacleMover>();
        }

        mover.moveSpeed = moveSpeed;
        spawnedObjects.Add(obj);

        if (logSpawns)
        {
            Debug.Log("TutorialBeatSpawner: Spawned " + obj.name + " at " + obj.transform.position + ".");
        }

        return obj.transform;
    }

    public void RemoveStaticTutorialObjects()
    {
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject obj = transforms[i].gameObject;
            if (obj == null || obj.GetComponent<TutorialSpawnedObject>() != null)
            {
                continue;
            }

            if (IsStaticTutorialGameplayObject(obj))
            {
                obj.SetActive(false);
            }
        }
    }

    private GameObject SpawnPrefabOrFallback(GameObject prefab, Vector3 position, Color color, string tagName, Vector2 size, bool trigger)
    {
        GameObject obj = null;
        if (prefab != null)
        {
            obj = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            obj = CreateFallbackBox("TutorialFallback", position, color, tagName, size, trigger);
        }

        obj.SetActive(true);
        return obj;
    }

    private GameObject CreateSlideGate(Vector3 position)
    {
        GameObject obj = CreateFallbackBox("TutorialSlideGate", position, new Color(0.95f, 0.22f, 0.25f), "EnemyBarrier", new Vector2(1.1f, 0.9f), false);
        return obj;
    }

    private GameObject CreateFallbackBox(string objectName, Vector3 position, Color color, string tagName, Vector2 size, bool trigger)
    {
        GameObject obj = new GameObject(objectName);
        if (!string.IsNullOrEmpty(tagName))
        {
            obj.tag = tagName;
        }

        obj.transform.position = position;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFallbackSprite();
        renderer.color = color;
        renderer.sortingOrder = 4;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = trigger;
        return obj;
    }

    private GameObject SpawnPickup(GameObject prefab, Vector3 position, Color color, string label, TutorialActionType pickupType, int beatIndex, int scoreValue)
    {
        GameObject obj = null;
        if (prefab != null)
        {
            obj = Instantiate(prefab, position, Quaternion.identity);
            ForceTutorialPickupPhysics(obj);
        }
        else
        {
            obj = CreateFallbackBox("Tutorial" + label, position, color, "Untagged", new Vector2(0.72f, 0.72f), true);
            GameObject labelObject = new GameObject(label + "Label");
            labelObject.transform.SetParent(obj.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.02f);

            TextMesh text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.characterSize = 0.18f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.black;
            MeshRenderer textRenderer = text.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingOrder = 6;
            }
            obj.tag = "Untagged";
        }

        ConfigurePickupComponents(obj, pickupType, beatIndex, label, scoreValue);
        obj.SetActive(true);
        return obj;
    }

    private void ForceTutorialPickupPhysics(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        Transform[] transforms = obj.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].gameObject.tag = "Untagged";
        }

        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }

        if (colliders.Length == 0)
        {
            CircleCollider2D collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;
            collider.isTrigger = true;
        }
    }

    private void ConfigurePickupComponents(GameObject root, TutorialActionType pickupType, int beatIndex, string displayName, int scoreValue)
    {
        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
        CircleCollider2D rootTrigger = root.GetComponent<CircleCollider2D>();
        if (rootTrigger == null)
        {
            rootTrigger = root.AddComponent<CircleCollider2D>();
        }
        rootTrigger.radius = pickupTriggerRadius;
        rootTrigger.isTrigger = true;

        Rigidbody2D body = root.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = root.AddComponent<Rigidbody2D>();
        }
        body.isKinematic = true;
        body.gravityScale = 0f;

        colliders = root.GetComponentsInChildren<Collider2D>(true);
        if (colliders.Length == 0)
        {
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = pickupTriggerRadius;
            collider.isTrigger = true;
            colliders = new Collider2D[] { collider };
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            TutorialRhythmPickup pickup = colliders[i].GetComponent<TutorialRhythmPickup>();
            if (pickup == null)
            {
                pickup = colliders[i].gameObject.AddComponent<TutorialRhythmPickup>();
            }

            pickup.pickupType = pickupType;
            pickup.beatIndex = beatIndex;
            pickup.displayName = displayName;
            pickup.scoreValue = scoreValue;
            pickup.ownerRoot = root;
        }
    }

    private float GetWorldXForBeat(int beatIndex)
    {
        float songTime = RhythmManager.Instance != null ? RhythmManager.Instance.GetAdjustedSongTime() : 0f;
        float targetSongTime = beatIndex * (60f / Mathf.Max(1f, bpm));
        return playerMeetX + moveSpeed * (targetSongTime - songTime);
    }

    private void ResolveGroundY()
    {
        GameObject floor = GameObject.FindGameObjectWithTag("Floor");
        if (floor == null)
        {
            Debug.LogWarning("TutorialBeatSpawner: No object tagged Floor found. Using fallback groundY " + groundY + ".");
            return;
        }

        Collider2D collider = floor.GetComponent<Collider2D>();
        if (collider != null)
        {
            groundY = collider.bounds.max.y;
        }
    }

    private void ResolvePickupY()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            pickupY = groundY + 0.85f;
            return;
        }

        Collider2D collider = player.GetComponent<Collider2D>();
        if (collider != null)
        {
            pickupY = collider.bounds.center.y;
        }
        else
        {
            pickupY = player.transform.position.y;
        }
    }

    private void ResolveDefaultPrefabs()
    {
        if (barrierPrefab == null)
        {
            Barrier barrier = FindObjectOfType<Barrier>();
            if (barrier != null && barrier.barrierPrefabs != null && barrier.barrierPrefabs.Length > 0)
            {
                barrierPrefab = barrier.barrierPrefabs[0];
            }
        }
    }

    private bool IsStaticTutorialGameplayObject(GameObject obj)
    {
        if (obj.CompareTag("Bonus1") || obj.CompareTag("Bonus2") || obj.CompareTag("jiasu") || obj.CompareTag("xt") || obj.CompareTag("UpCollider"))
        {
            return true;
        }

        string objectName = obj.name;
        return objectName.StartsWith("Bonus")
            || objectName.StartsWith("Gold")
            || objectName.StartsWith("jetpack")
            || objectName.StartsWith("carrot_gold");
    }

    private Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        fallbackSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return fallbackSprite;
    }
}

public class TutorialRhythmPickup : MonoBehaviour
{
    public TutorialActionType pickupType;
    public int beatIndex;
    public string displayName;
    public int scoreValue = 1;
    public GameObject ownerRoot;

    private bool consumed;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || other == null || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        consumed = true;
        TutorialFlowManager flow = FindObjectOfType<TutorialFlowManager>();
        if (flow != null)
        {
            flow.ReportRhythmPickup(pickupType, beatIndex, displayName, scoreValue, ownerRoot != null ? ownerRoot : gameObject);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        OnTriggerEnter2D(other);
    }
}
