using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OceanSpriteAnimator : MonoBehaviour
{
    public float framesPerSecond = 6f;

    private Image image;
    private readonly List<Sprite> frames = new List<Sprite>();
    private int frameIndex;
    private float frameTimer;

    public void SetFrames(Sprite[] sprites, float fps)
    {
        image = image != null ? image : GetComponent<Image>();
        frames.Clear();
        frameIndex = 0;
        frameTimer = 0f;
        framesPerSecond = Mathf.Max(1f, fps);

        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    frames.Add(sprites[i]);
                }
            }
        }

        enabled = frames.Count > 1 && image != null;
        if (image != null && frames.Count > 0)
        {
            image.sprite = frames[0];
        }
    }

    public void StopOn(Sprite sprite)
    {
        image = image != null ? image : GetComponent<Image>();
        frames.Clear();
        enabled = false;
        if (image != null)
        {
            image.sprite = sprite;
        }
    }

    private void Update()
    {
        if (image == null || frames.Count <= 1)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Count;
            image.sprite = frames[frameIndex];
        }
    }
}
