using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private Coroutine routine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Trigger(float duration, float timeScale)
    {
        if (duration <= 0f)
        {
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }
        routine = StartCoroutine(Run(duration, timeScale));
    }

    IEnumerator Run(float duration, float timeScale)
    {
        float prev = Time.timeScale;
        Time.timeScale = Mathf.Clamp01(timeScale);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = prev;
        routine = null;
    }
}
