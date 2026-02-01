using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class MonsterAI : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    public Transform target;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float stopDistance = 1.2f;
    [Tooltip("Force kinematic movement to avoid spinning from collisions.")]
    public bool forceKinematic = true;
    [Tooltip("Prevent rotation from physics.")]
    public bool freezeRotation = true;

    [Header("Attack")]
    public float attackRange = 1.2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.0f;
    public GameObject attackVfxPrefab;
    public Vector3 attackVfxOffset;
    public float attackVfxLifetime = 1.0f;

    [Header("Health")]
    public float maxHealth = 50f;
    public float currentHealth = 50f;

    [Header("Hit Flash (Material)")]
    [Tooltip("Float property name on the material/shader (e.g. FlashAmount).")]
    public string flashProperty = "FlashAmount";
    public float flashDuration = 0.08f;

    [Header("Hit Knockback")]
    public float knockbackDistance = 0.4f;
    public float knockbackDuration = 0.08f;
    [Tooltip("0-1 time to 0-1 distance. Ease-out by default.")]
    public AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Hit VFX")]
    public GameObject hitVfxPrefab;
    public Vector3 hitVfxOffset;
    public float hitVfxLifetime = 1.0f;

    [Header("Hit SFX")]
    public AudioSource hitAudioSource;
    public AudioClip[] hitClips;
    public float hitSfxCooldown = 0.1f;
    public Vector2 hitPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Animation")]
    public Animator animator;
    public string moveBool = "IsMoving";
    public string attackTrigger = "Attack";
    public string deathTrigger = "Die";
    public float deathDestroyDelay = 1.0f;

    [Header("Facing")]
    public bool flipByTarget = true;

    [Header("Events")]
    public UnityEvent onDeath;

    private Rigidbody2D rb;
    private Collider2D col;
    private float lastAttackTime;
    private bool isDead;
    private Vector2 desiredVelocity;
    private bool shouldMove;
    private Vector3 baseScale;

    private SpriteRenderer[] spriteRenderers;
    private MaterialPropertyBlock block;
    private int flashPropId;
    private Coroutine flashRoutine;

    private float knockbackTime;
    private float knockbackElapsed;
    private Vector2 knockbackDir;
    private float knockbackTotal;
    private float knockbackPrevDist;
    private float lastHitSfxTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (rb != null)
        {
            if (forceKinematic)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            if (freezeRotation)
            {
                rb.freezeRotation = true;
                rb.angularVelocity = 0f;
            }
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        block = new MaterialPropertyBlock();
        flashPropId = Shader.PropertyToID(flashProperty);
        SetFlashAmount(0f);

        if (hitAudioSource == null)
        {
            hitAudioSource = GetComponent<AudioSource>();
        }

        baseScale = transform.localScale;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    protected virtual void Update()
    {
        if (isDead || target == null)
        {
            return;
        }

        if (flipByTarget)
        {
            UpdateFacing();
        }

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance <= attackRange)
        {
            TryAttack();
            SetMoving(false);
            shouldMove = false;
            return;
        }

        if (distance > stopDistance)
        {
            UpdateDesiredVelocity();
            SetMoving(true);
            shouldMove = true;
        }
        else
        {
            SetMoving(false);
            shouldMove = false;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || target == null)
        {
            return;
        }

        if (knockbackTime > 0f)
        {
            float dt = Time.fixedDeltaTime;
            knockbackTime -= dt;
            knockbackElapsed += dt;

            float t = knockbackDuration > 0f ? Mathf.Clamp01(knockbackElapsed / knockbackDuration) : 1f;
            float dist = knockbackCurve != null ? knockbackCurve.Evaluate(t) * knockbackTotal : t * knockbackTotal;
            float delta = dist - knockbackPrevDist;
            knockbackPrevDist = dist;

            Vector2 step = knockbackDir * delta;
            if (rb != null)
            {
                rb.MovePosition(rb.position + step);
            }
            else
            {
                transform.position += (Vector3)step;
            }
            return;
        }

        if (rb == null)
        {
            if (shouldMove)
            {
                transform.position += (Vector3)(desiredVelocity * Time.fixedDeltaTime);
            }
            return;
        }

        if (shouldMove)
        {
            Vector2 nextPosition = rb.position + desiredVelocity * Time.fixedDeltaTime;
            rb.MovePosition(nextPosition);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    void UpdateDesiredVelocity()
    {
        Vector2 direction = (target.position - transform.position).normalized;
        desiredVelocity = direction * moveSpeed;
    }

    void StopMovement()
    {
        shouldMove = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
        {
            return;
        }

        lastAttackTime = Time.time;
        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }

        SpawnAttackVfx();

        PlayerStatus status = target != null ? target.GetComponentInParent<PlayerStatus>() : null;
        if (status == null && target != null)
        {
            status = target.GetComponentInChildren<PlayerStatus>();
        }
        if (status == null)
        {
            return;
        }
        status.AddHealth(-attackDamage);
    }

    void SpawnAttackVfx()
    {
        if (attackVfxPrefab == null)
        {
            return;
        }

        Vector3 offset = attackVfxOffset;
        if (target != null)
        {
            Vector3 dir = (target.position - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
                offset = new Vector3(dir.x * Mathf.Abs(attackVfxOffset.x), dir.y * Mathf.Abs(attackVfxOffset.y), attackVfxOffset.z);
            }
        }

        Vector3 pos = transform.position + offset;
        GameObject vfx = Instantiate(attackVfxPrefab, pos, Quaternion.identity);
        if (attackVfxLifetime > 0f)
        {
            Destroy(vfx, attackVfxLifetime);
        }
    }

    void SetMoving(bool isMoving)
    {
        if (animator != null && !string.IsNullOrEmpty(moveBool))
        {
            animator.SetBool(moveBool, isMoving);
        }
    }

    public void TakeDamage(float amount)
    {
        ApplyHit(amount, Vector2.zero, false);
    }

    public void ApplyHit(float amount, Vector2 hitDir, bool isMelee)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        TriggerFlash();
        SpawnHitVfx(hitDir);
        PlayHitSfx();
        StartKnockback(hitDir);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void TriggerFlash()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashCoroutine());
    }

    void SpawnHitVfx(Vector2 hitDir)
    {
        if (hitVfxPrefab == null)
        {
            return;
        }

        Vector3 pos = transform.position + hitVfxOffset;
        Quaternion rot = Quaternion.identity;
        if (hitDir != Vector2.zero)
        {
            float z = Mathf.Atan2(hitDir.y, hitDir.x) * Mathf.Rad2Deg;
            rot = Quaternion.Euler(0f, 0f, z);
        }

        GameObject vfx = Instantiate(hitVfxPrefab, pos, rot);
        if (hitVfxLifetime > 0f)
        {
            Destroy(vfx, hitVfxLifetime);
        }
    }

    void PlayHitSfx()
    {
        if (hitAudioSource == null || hitClips == null || hitClips.Length == 0)
        {
            return;
        }

        if (Time.time - lastHitSfxTime < hitSfxCooldown)
        {
            return;
        }

        AudioClip clip = hitClips.Length == 1 ? hitClips[0] : hitClips[Random.Range(0, hitClips.Length)];
        if (clip == null)
        {
            return;
        }

        lastHitSfxTime = Time.time;
        float pitch = Random.Range(hitPitchRange.x, hitPitchRange.y);
        hitAudioSource.pitch = pitch;
        hitAudioSource.PlayOneShot(clip);
    }

    IEnumerator FlashCoroutine()
    {
        SetFlashAmount(1f);
        yield return new WaitForSecondsRealtime(flashDuration);
        SetFlashAmount(0f);
    }

    void SetFlashAmount(float value)
    {
        if (spriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null)
            {
                continue;
            }

            sr.GetPropertyBlock(block);
            block.SetFloat(flashPropId, value);
            sr.SetPropertyBlock(block);
        }
    }

    void StartKnockback(Vector2 hitDir)
    {
        if (knockbackDistance <= 0f || knockbackDuration <= 0f)
        {
            return;
        }

        if (hitDir == Vector2.zero)
        {
            if (target != null)
            {
                hitDir = (transform.position - target.position);
            }
        }

        if (hitDir == Vector2.zero)
        {
            return;
        }

        knockbackDir = hitDir.normalized;
        knockbackTotal = knockbackDistance;
        knockbackTime = knockbackDuration;
        knockbackElapsed = 0f;
        knockbackPrevDist = 0f;
    }

    void Die()
    {
        isDead = true;
        StopMovement();

        if (col != null)
        {
            col.enabled = false;
        }

        if (animator != null && !string.IsNullOrEmpty(deathTrigger))
        {
            animator.SetTrigger(deathTrigger);
        }

        onDeath?.Invoke();

        Destroy(gameObject, deathDestroyDelay);
    }

    void UpdateFacing()
    {
        float dx = target.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.0001f)
        {
            return;
        }

        Vector3 scale = baseScale;
        float sign = dx < 0f ? -1f : 1f;
        scale.x = Mathf.Abs(baseScale.x) * sign;
        transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
