using UnityEngine;

public class SimpleMetronomeAudio : MonoBehaviour
{
    public float volume = 0.42f;

    private AudioSource audioSource;
    private AudioClip accentClip;
    private AudioClip weakClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        accentClip = CreateToneClip("OceanAccentTick", 920f, 0.065f, 0.85f);
        weakClip = CreateToneClip("OceanWeakTick", 540f, 0.045f, 0.55f);
    }

    public void PlayBeat(bool accented)
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(accented ? accentClip : weakClip, volume);
    }

    private AudioClip CreateToneClip(string clipName, float frequency, float duration, float gain)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - ((float)i / sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * gain;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
