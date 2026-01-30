using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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

    public float speed = 1.0f;
    public float dashTime = 2.0f;
    public float dashSpeed = 8.0f;

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
    }

    void Update()
    {
        if (sanDrainPerSecond > 0f && currentSan > 0f)
        {
            AddSan(-sanDrainPerSecond * Time.deltaTime);
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
        onHealthChange.Invoke(currentHealth);
        CheckDeath();
    }

    public void AddSan(float amount)
    {
        currentSan = Mathf.Clamp(currentSan + amount, 0f, maxSan);
        onSanChange.Invoke(currentSan);
    }

    void CheckDeath()
    {
        if (!isDead && currentHealth <= 0f)
        {
            isDead = true;
            onDeath?.Invoke();
        }
    }



}
