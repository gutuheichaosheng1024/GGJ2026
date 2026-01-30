using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WeaponHotbarUI : MonoBehaviour
{
    [Header("Slots")]
    public Image currentIcon;
    public Image nextIcon;

    [Header("Data")]
    public List<Sprite> weaponIcons = new List<Sprite>();
    public int startIndex = 0;

    [Header("Input")]
    public KeyCode switchKey = KeyCode.Alpha3;
    public bool listenInput = false;

    [Header("Slide")]
    public float slideDuration = 0.2f;
    public float slideDistance = 80f;
    public bool useUnscaledTime = true;

    [Header("Debug")]
    public bool logIndexUpdates = false;

    [Header("Events")]
    public UnityEvent<int> onWeaponIndexChanged;

    int currentIndex;
    bool isSwitching;
    RectTransform currentRect;
    RectTransform nextRect;
    Vector2 basePos;

    void Awake()
    {
        if (currentIcon != null)
        {
            currentRect = currentIcon.rectTransform;
            basePos = currentRect.anchoredPosition;
        }

        if (nextIcon != null)
        {
            nextRect = nextIcon.rectTransform;
            nextIcon.gameObject.SetActive(false);
        }

        InitializeIcons();
    }

    void Update()
    {
        if (listenInput && Input.GetKeyDown(switchKey))
        {
            TrySwitchToNext();
        }
    }

    public void SetWeapons(List<Sprite> icons, int startAt = 0)
    {
        weaponIcons = icons != null ? new List<Sprite>(icons) : new List<Sprite>();
        startIndex = Mathf.Clamp(startAt, 0, Mathf.Max(0, weaponIcons.Count - 1));
        InitializeIcons();
    }

    void InitializeIcons()
    {
        if (weaponIcons == null || weaponIcons.Count == 0 || currentIcon == null)
        {
            return;
        }

        currentIndex = Mathf.Clamp(startIndex, 0, weaponIcons.Count - 1);
        currentIcon.sprite = weaponIcons[currentIndex];
        currentIcon.enabled = currentIcon.sprite != null;
        if (nextIcon != null)
        {
            nextIcon.enabled = true;
            nextIcon.gameObject.SetActive(false);
        }
    }

    void TrySwitchToNext()
    {
        if (isSwitching)
        {
            return;
        }

        if (weaponIcons == null || weaponIcons.Count < 2)
        {
            return;
        }

        int nextIndex = (currentIndex + 1) % weaponIcons.Count;
        StartCoroutine(SlideSwitch(nextIndex));
    }

    public void SetWeaponIndex(int index)
    {
        if (logIndexUpdates)
        {
            Debug.Log($"[WeaponHotbarUI] SetWeaponIndex({index})");
        }
        if (weaponIcons == null || weaponIcons.Count == 0 || currentIcon == null)
        {
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, weaponIcons.Count - 1);
        currentIcon.sprite = weaponIcons[currentIndex];
        currentIcon.enabled = currentIcon.sprite != null;
        if (nextIcon != null)
        {
            nextIcon.gameObject.SetActive(false);
        }
    }

    public void SetWeaponIndexAnimated(int index)
    {
        if (logIndexUpdates)
        {
            Debug.Log($"[WeaponHotbarUI] SetWeaponIndexAnimated({index})");
        }
        if (weaponIcons == null || weaponIcons.Count == 0)
        {
            return;
        }

        int clamped = Mathf.Clamp(index, 0, weaponIcons.Count - 1);
        if (clamped == currentIndex || isSwitching)
        {
            SetWeaponIndex(clamped);
            return;
        }

        StartCoroutine(SlideSwitch(clamped));
    }

    IEnumerator SlideSwitch(int nextIndex)
    {
        if (currentIcon == null || nextIcon == null)
        {
            currentIndex = nextIndex;
            if (currentIcon != null && weaponIcons.Count > 0)
            {
                currentIcon.sprite = weaponIcons[currentIndex];
            }
            onWeaponIndexChanged?.Invoke(currentIndex);
            yield break;
        }

        isSwitching = true;

        nextIcon.sprite = weaponIcons[nextIndex];
        nextIcon.gameObject.SetActive(true);

        Vector2 offset = new Vector2(slideDistance, 0f);
        currentRect.anchoredPosition = basePos;
        nextRect.anchoredPosition = basePos + offset;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = slideDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / slideDuration);
            float eased = t * t * (3f - 2f * t);

            currentRect.anchoredPosition = Vector2.Lerp(basePos, basePos - offset, eased);
            nextRect.anchoredPosition = Vector2.Lerp(basePos + offset, basePos, eased);
            yield return null;
        }

        currentIndex = nextIndex;
        currentIcon.sprite = weaponIcons[currentIndex];
        currentRect.anchoredPosition = basePos;
        nextRect.anchoredPosition = basePos + offset;
        nextIcon.gameObject.SetActive(false);

        onWeaponIndexChanged?.Invoke(currentIndex);
        isSwitching = false;
    }
}
