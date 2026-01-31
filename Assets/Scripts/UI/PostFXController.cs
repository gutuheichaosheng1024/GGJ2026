using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PostFXController : MonoBehaviour
{
    [Header("Volumes")]
    public Volume globalVolume;
    public Volume maskVolume;

    [Header("Transition")]
    public float transitionTime = 0.35f;
    public bool useUnscaledTime = true;

    Coroutine transitionRoutine;

    void Awake()
    {
        if (globalVolume != null)
        {
            globalVolume.weight = 1f;
        }
    }

    public void SetMask(bool on)
    {
        float target = on ? 1f : 0f;
        StartTransition(target);
    }

    void StartTransition(float target)
    {
        if (maskVolume == null)
        {
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }
        transitionRoutine = StartCoroutine(TransitionTo(target));
    }

    IEnumerator TransitionTo(float target)
    {
        float start = maskVolume.weight;
        float duration = Mathf.Max(0.01f, transitionTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            maskVolume.weight = Mathf.Lerp(start, target, eased);
            yield return null;
        }

        maskVolume.weight = target;
        transitionRoutine = null;
    }
}
