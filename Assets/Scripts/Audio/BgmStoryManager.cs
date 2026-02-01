using System.Collections;
using UnityEngine;

public class BgmStoryManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip normalBgm;
    public AudioClip bossBgm;
    public AudioClip afterBossBgm;

    [Header("Fade")]
    public float fadeDuration = 0.5f;
    public bool useUnscaledTime = true;

    [Header("Startup")]
    public bool playNormalOnStart = true;

    Coroutine fadeRoutine;
    float baseVolume = 1f;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (audioSource != null)
        {
            baseVolume = audioSource.volume;
        }
    }

    void Start()
    {
        if (playNormalOnStart)
        {
            PlayNormal();
        }
    }

    public void PlayNormal()
    {
        SwitchTo(normalBgm);
    }

    public void PlayBoss()
    {
        SwitchTo(bossBgm);
    }

    public void PlayAfterBoss()
    {
        SwitchTo(afterBossBgm);
    }

    void SwitchTo(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        if (audioSource.clip == clip && audioSource.isPlaying)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        if (fadeDuration <= 0f)
        {
            audioSource.clip = clip;
            audioSource.Play();
            return;
        }

        fadeRoutine = StartCoroutine(FadeTo(clip));
    }

    IEnumerator FadeTo(AudioClip clip)
    {
        float t = 0f;
        float from = audioSource.volume;
        float to = 0f;

        while (t < fadeDuration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float lerp = Mathf.Clamp01(t / fadeDuration);
            audioSource.volume = Mathf.Lerp(from, to, lerp);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.clip = clip;
        audioSource.Play();

        t = 0f;
        from = 0f;
        to = baseVolume;

        while (t < fadeDuration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float lerp = Mathf.Clamp01(t / fadeDuration);
            audioSource.volume = Mathf.Lerp(from, to, lerp);
            yield return null;
        }

        audioSource.volume = baseVolume;
        fadeRoutine = null;
    }
}
