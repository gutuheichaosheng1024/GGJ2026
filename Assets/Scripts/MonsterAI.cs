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

    [Header("Health")]
    public float maxHealth = 50f;
    public float currentHealth = 50f;

    [Header("Animation")]
    public Animator animator;
    public string moveBool = "IsMoving";
    public string attackTrigger = "Attack";
    public string deathTrigger = "Die";
    public float deathDestroyDelay = 1.0f;

    [Header("Events")]
    public UnityEvent onDeath;

    private Rigidbody2D rb;
    private Collider2D col;
    private float lastAttackTime;
    private bool isDead;
    private Vector2 desiredVelocity;
    private bool shouldMove;

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

    void Update()
    {
        if (isDead || target == null)
        {
            return;
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

    void FixedUpdate()
    {
        if (isDead || target == null)
        {
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
            rb.velocity = Vector2.zero; // Unity 2022: Rigidbody2D uses velocity (linearVelocity is Unity 6+).
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
            rb.velocity = Vector2.zero; // Unity 2022: Rigidbody2D uses velocity (linearVelocity is Unity 6+).
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

        PlayerStatus status = target.GetComponent<PlayerStatus>();
        if (status != null)
        {
            status.AddHealth(-attackDamage);
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
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        if (currentHealth <= 0f)
        {
            Die();
        }
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
