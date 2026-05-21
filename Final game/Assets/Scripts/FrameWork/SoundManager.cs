using UnityEngine;

public class SoundManager : MonoBehaviour {

    private static SoundManager _instance;
    public static SoundManager Instance
    {
        get { return _instance; }
    }

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        _instance = this;
    }

    public void PlayAudioByNmae(string name)
    {
        PlaySFX(name);
    }

    public static void PlaySFX(string clipName)
    {
        AudioClip clip = LoadClip(clipName);
        if (clip == null)
        {
            return;
        }

        Vector3 position = Vector3.zero;
        if (Camera.main != null)
        {
            position = Camera.main.transform.position;
        }

        AudioSource.PlayClipAtPoint(clip, position);
    }

    private static AudioClip LoadClip(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>(clipName);
        if (clip == null)
        {
            clip = Resources.Load<AudioClip>("Sounds/" + clipName);
        }

        if (clip == null)
        {
            Debug.LogWarning("SoundManager: AudioClip not found in Resources: " + clipName);
        }

        return clip;
    }

    public void PlayMusicByName(string name)
    {
        AudioClip clip = LoadClip(name);
        if (clip == null)
        {
            return;
        }

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        audioSource.clip = clip;
        audioSource.Play();
    }
}
