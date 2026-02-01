using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class PlayerStatus : MonoBehaviour
{
    public static string Pname = "Player";

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Sanity")]
    public float maxSan = 100f;
    public float currentSan = 100f;

    public float moveSpeed = 1.0f;
    public float dashTime = 2.0f;
    public float dashSpeed = 8.0f;

    [Header("Mask / Buff")]
    public bool isMaskOn = false;
    [Tooltip("Sanity regen per second when NOT wearing mask (positive).")]
    public float sanRegenPerSecond = 0.5f;
    [Tooltip("Sanity drain per second when mask is ON (positive).")]
    public float sanDrainMaskPerSecond = 3f;
    public MaskBuff maskBuff = new MaskBuff();
    public AudioSource maskAudioSource;
    public AudioClip maskToggleClip;

    [Header("Potion")]
    public KeyCode potionKey = KeyCode.Alpha1;
    public bool usePotionOnKey = true;
    public int potionCount = 2;
    public float potionHeal = 5f;
    public TMP_Text potionCountText;
    public AudioSource potionAudioSource;
    public AudioClip potionClip;

    [Header("Events")]
    public UnityAction<float> onHealthChange;
    public UnityAction<float> onSanChange;
    public UnityEvent onDeath;

    private bool isDead;

    private static PlayerStatus instance;

    public static PlayerStatus Instance
    {
        get
        {

            if (instance == null)
            {
                instance = FindAnyObjectByType<PlayerStatus>();

                if (instance == null)
                {
                    throw new System.Exception("不存在playerStatus");
                }
            }
            return instance;

        }
    }

    void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentSan = Mathf.Clamp(currentSan, 0f, maxSan);
        if (potionAudioSource == null)
        {
            potionAudioSource = GetComponent<AudioSource>();
        }
        if (maskAudioSource == null)
        {
            maskAudioSource = potionAudioSource;
        }
        UpdatePotionUI();
    }

    void Update()
    {
        if (usePotionOnKey && Input.GetKeyDown(potionKey))
        {
            TryUsePotion();
        }

        float dt = Time.deltaTime;
        if (isMaskOn)
        {
            if (sanDrainMaskPerSecond > 0f && currentSan > 0f)
            {
                AddSan(-sanDrainMaskPerSecond * dt);
            }
        }
        else
        {
            if (sanRegenPerSecond > 0f && currentSan < maxSan)
            {
                AddSan(sanRegenPerSecond * dt);
            }
        }

        CheckDeath();
    }

    public void ToggleMask()
    {
        isMaskOn = !isMaskOn;
        PlayMaskSfx();
        SyncPostFx();
    }

    public void SetMask(bool on)
    {
        isMaskOn = on;
        PlayMaskSfx();
        SyncPostFx();
    }

    void SyncPostFx()
    {
        PostFXController fx = FindFirstObjectByType<PostFXController>();
        if (fx != null)
        {
            fx.SetMask(isMaskOn);
        }
    }

    public float GetMoveSpeed()
    {
        float baseSpeed = moveSpeed;
        if (!isMaskOn || maskBuff == null)
        {
            return baseSpeed;
        }

        return (baseSpeed + maskBuff.moveSpeedAdd) * maskBuff.moveSpeedMul;
    }

    public void AddHealth(float amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        onHealthChange?.Invoke(currentHealth);
        CheckDeath();
    }

    public bool TryUsePotion()
    {
        if (isDead)
        {
            return false;
        }

        if (potionCount <= 0)
        {
            return false;
        }

        if (currentHealth >= maxHealth)
        {
            return false;
        }

        potionCount -= 1;
        AddHealth(potionHeal);
        if (potionAudioSource != null && potionClip != null)
        {
            potionAudioSource.PlayOneShot(potionClip);
        }
        UpdatePotionUI();
        return true;
    }

    public void AddPotions(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        potionCount += amount;
        UpdatePotionUI();
    }

    public void AddSan(float amount)
    {
        currentSan = Mathf.Clamp(currentSan + amount, 0f, maxSan);
        onSanChange?.Invoke(currentSan);
    }

    void CheckDeath()
    {
        if (!isDead && currentHealth <= 0f)
        {
            isDead = true;
            onDeath?.Invoke();
        }
    }

    void UpdatePotionUI()
    {
        if (potionCountText != null)
        {
            potionCountText.text = potionCount.ToString();
        }
    }

    void PlayMaskSfx()
    {
        if (maskAudioSource != null && maskToggleClip != null)
        {
            maskAudioSource.PlayOneShot(maskToggleClip);
        }
    }


}

[System.Serializable]
public class MaskBuff
{
    [Header("Cone (melee)")]
    public float coneDamageAdd = 0f;
    public float coneDamageMul = 1.1f;
    public float coneRadiusAdd = 0f;
    public float coneRadiusMul = 1f;
    public float coneCooldownAdd = 0f;
    public float coneCooldownMul = 0.9f;

    [Header("Circle (melee)")]
    public float circleDamageAdd = 0f;
    public float circleDamageMul = 1.0f;
    public float circleRadiusAdd = 0f;
    public float circleRadiusMul = 1f;
    public float circleCooldownAdd = 0f;
    public float circleCooldownMul = 1.0f;

    [Header("Movement")]
    public float moveSpeedAdd = 0f;
    public float moveSpeedMul = 1.0f;

    public float GetDamageMul(PlayerAttack.AttackMode mode)
    {
        switch (mode)
        {
            case PlayerAttack.AttackMode.Cone: return coneDamageMul;
            case PlayerAttack.AttackMode.Circle: return circleDamageMul;
            default: return 1f;
        }
    }

    public float GetCooldownMul(PlayerAttack.AttackMode mode)
    {
        switch (mode)
        {
            case PlayerAttack.AttackMode.Cone: return coneCooldownMul;
            case PlayerAttack.AttackMode.Circle: return circleCooldownMul;
            default: return 1f;
        }
    }

    public float GetDamageAdd(PlayerAttack.AttackMode mode)
    {
        switch (mode)
        {
            case PlayerAttack.AttackMode.Cone: return coneDamageAdd;
            case PlayerAttack.AttackMode.Circle: return circleDamageAdd;
            default: return 0f;
        }
    }

    public float GetRadiusAdd(PlayerAttack.AttackMode mode)
    {
        switch (mode)
        {
            case PlayerAttack.AttackMode.Cone: return coneRadiusAdd;
            case PlayerAttack.AttackMode.Circle: return circleRadiusAdd;
            default: return 0f;
        }
    }

    public float GetRadiusMul(PlayerAttack.AttackMode mode)
    {
        switch (mode)
        {
            case PlayerAttack.AttackMode.Cone: return coneRadiusMul;
            case PlayerAttack.AttackMode.Circle: return circleRadiusMul;
            default: return 1f;
        }
    }

    public float GetRangeAdd(PlayerAttack.AttackMode mode)
    {
        return 0f;
    }

    public float GetRangeMul(PlayerAttack.AttackMode mode)
    {
        return 1f;
    }

    public float GetCooldownAdd(PlayerAttack.AttackMode mode)
    {
        switch (mode)
        {
            case PlayerAttack.AttackMode.Cone: return coneCooldownAdd;
            case PlayerAttack.AttackMode.Circle: return circleCooldownAdd;
            default: return 0f;
        }
    }
}
