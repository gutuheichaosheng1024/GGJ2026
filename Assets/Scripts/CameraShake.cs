using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraShake : MonoBehaviour
{
    public float defaultDuration = 0.06f;
    public float defaultStrength = 0.15f;

    private Coroutine routine;
    private float timeRemaining;
    private float shakeStrength;
    private Vector3 lastOffset;

    public void Shake(float duration, float strength)
    {
        timeRemaining = Mathf.Max(0f, duration);
        shakeStrength = Mathf.Max(0f, strength);

        if (routine != null)
        {
            StopCoroutine(routine);
        }
        routine = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        routine = null;
    }

    void LateUpdate()
    {
        if (timeRemaining > 0f)
        {
            if (lastOffset != Vector3.zero)
            {
                transform.localPosition -= lastOffset;
            }

            Vector2 offset = Random.insideUnitCircle * shakeStrength;
            lastOffset = new Vector3(offset.x, offset.y, 0f);
            transform.localPosition += lastOffset;
        }
        else if (lastOffset != Vector3.zero)
        {
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;
        }
    }
}
