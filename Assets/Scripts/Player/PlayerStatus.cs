using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerStatus : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Sanity")]
    public float maxSan = 100f;
    public float currentSan = 100f;
    [Tooltip("Sanity drain per second.")]
    public float sanDrainPerSecond = 1f;

    [Header("UI")]
    public Slider healthSlider;
    public Slider sanSlider;

    [Header("Events")]
    public UnityEvent onDeath;

    private bool isDead;

    void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentSan = Mathf.Clamp(currentSan, 0f, maxSan);
        SyncSliders();
    }

    void Update()
    {
        if (sanDrainPerSecond > 0f && currentSan > 0f)
        {
            currentSan = Mathf.Max(0f, currentSan - sanDrainPerSecond * Time.deltaTime);
            SyncSan();
        }

        CheckDeath();
    }

    public void AddHealth(float amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        SyncHealth();
        CheckDeath();
    }

    public void AddSan(float amount)
    {
        currentSan = Mathf.Clamp(currentSan + amount, 0f, maxSan);
        SyncSan();
    }

    void CheckDeath()
    {
        if (!isDead && currentHealth <= 0f)
        {
            isDead = true;
            onDeath?.Invoke();
        }
    }

    void SyncSliders()
    {
        SyncHealth();
        SyncSan();
    }

    void SyncHealth()
    {
        if (healthSlider == null)
        {
            return;
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    void SyncSan()
    {
        if (sanSlider == null)
        {
            return;
        }

        sanSlider.maxValue = maxSan;
        sanSlider.value = currentSan;
    }
}
