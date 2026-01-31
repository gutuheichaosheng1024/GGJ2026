using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BossAI2 : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Chase,
        Attack,
        Skill,
        Dead
    }

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

    [Header("Health")]
    public float maxHealth = 40f;
    public float currentHealth = 40f;

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

    [Header("Events")]
    public UnityEvent onDeath;

    [Header("Boss Stats")]
    public float normalAttackDamage = 2f;
    [Tooltip("Attacks per second. 0.65 ~= 1.54s cooldown.")]
    public float attackSpeed = 0.65f;
    public float attackRangeOverride = 2.2f;
    public LayerMask playerLayers = ~0;
    public bool rotateBossToTarget = false;

    [Header("Phase / Invulnerable")]
    public bool startPhased = false;

    [Header("Tentacle Whip")]
    public float tentacleWindup = 0.5f;
    public float tentacleAngle = 90f;
    public float tentacleRadius = 2.5f;
    public bool tentaclePhases = true;

    [Header("Charge (HP < 50%)")]
    public float chargeWindup = 1.5f;
    public float chargeDistance = 6f;
    public float chargeSpeed = 10f;
    public Vector2 chargeBoxSize = new Vector2(1.6f, 5.5f);
    public float chargeTriggerRange = 6f;
    public float chargeCooldown = 6f;
    public bool chargeOnlyBelowHalf = true;
    public bool chargeDistanceMatchesTelegraph = true;
    public bool chargeDamageOnContact = true;
    public bool chargeUseTriggerDuringDash = true;

    [Header("Stomp (HP 75% / 25%)")]
    public float stompWindup = 1.5f;
    public float stompAngle = 120f;
    public float stompRadius = 4f;
    public int stompWaves = 3;
    public float stompWaveInterval = 0.25f;
    public float stompWaveDamage = 3f;

    [Header("Summon (HP 50% once)")]
    public Transform arenaCenter;
    public Transform[] summonPoints;
    public Vector3[] summonOffsets = new Vector3[]
    {
        new Vector3(-2f, 0f, 0f),
        new Vector3(2f, 0f, 0f),
        new Vector3(0f, 2f, 0f)
    };
    public GameObject cultistPrefab;
    public int cultistCount = 3;
    public float cultistMaxHealth = 6f;

    [Header("Telegraph")]
    public Material telegraphMaterial;
    public Color telegraphColor = new Color(1f, 0.25f, 0.25f, 0.35f);
    public string telegraphSortingLayer = "Default";
    public int telegraphSortingOrder = 5;

    [Header("Phase Visual")]
    [Range(0f, 1f)]
    public float phasedAlpha = 0.35f;
    public float maxPhasedDuration = 5f;

    BossState state = BossState.Idle;
    Rigidbody2D rb;
    Collider2D col;
    bool isDead;
    bool isPhased;
    bool isSkillRunning;
    float nextAttackTime;
    float nextChargeTime;
    float phasedUntil;

    bool triggered75;
    bool triggered50;
    bool triggered25;
    readonly List<GameObject> summonedMinions = new List<GameObject>();

    Vector2 desiredVelocity;
    bool shouldMove;
    bool isCharging;
    Vector2 chargeDir;
    float chargeRemaining;

    SpriteRenderer[] spriteRenderers;
    MaterialPropertyBlock block;
    int flashPropId;
    Coroutine flashRoutine;
    float lastHitSfxTime;

    float knockbackTime;
    float knockbackElapsed;
    Vector2 knockbackDir;
    float knockbackTotal;
    float knockbackPrevDist;

    GameObject coneTelegraph;
    MeshFilter coneFilter;
    MeshRenderer coneRenderer;
    GameObject rectTelegraph;
    MeshFilter rectFilter;
    MeshRenderer rectRenderer;

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

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        SetupTelegraphs();
        SetPhased(startPhased);
    }

    void Start()
    {
        if (onDeath != null)
        {
            onDeath.AddListener(OnBossDeath);
        }

        if (target == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null)
            {
                target = go.transform;
            }
        }
    }

    void Update()
    {
        if (isPhased && maxPhasedDuration > 0f && Time.time >= phasedUntil)
        {
            SetPhased(false);
        }

        if (isDead || target == null)
        {
            return;
        }

        if (!isSkillRunning)
        {
            TryTriggerThresholdSkills();
        }

        if (isSkillRunning)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        bool canCharge = CanChargeNow();
        bool inChargeRange = canCharge && distance <= chargeTriggerRange;
        bool inAttackRange = distance <= attackRangeOverride;
        if (inChargeRange || inAttackRange)
        {
            state = BossState.Attack;
            TryAttack();
            shouldMove = false;
            SetMoving(false);
        }
        else
        {
            if (distance > stopDistance)
            {
                state = BossState.Chase;
                UpdateDesiredVelocity();
                shouldMove = true;
                SetMoving(true);
            }
            else
            {
                shouldMove = false;
                SetMoving(false);
            }
        }
    }

    void FixedUpdate()
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

        if (isCharging)
        {
            float step = chargeSpeed * Time.fixedDeltaTime;
            chargeRemaining -= step;
            Vector2 delta = chargeDir * step;
            if (rb != null)
            {
                rb.MovePosition(rb.position + delta);
            }
            else
            {
                transform.position += (Vector3)delta;
            }

            if (!chargeDamageOnContact)
            {
                DealBoxDamage(chargeDir, chargeBoxSize, normalAttackDamage);
            }

            if (chargeRemaining <= 0f)
            {
                isCharging = false;
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
        if (target == null)
        {
            return;
        }

        Vector2 dir = (target.position - transform.position).normalized;
        if (rotateBossToTarget && dir != Vector2.zero)
        {
            transform.up = dir;
        }

        desiredVelocity = dir * moveSpeed;
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
        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (CanChargeNow())
        {
            StartCoroutine(DoCharge());
            nextChargeTime = Time.time + chargeCooldown;
        }
        else
        {
            StartCoroutine(DoTentacleWhip());
        }

        float cooldown = attackSpeed > 0f ? 1f / attackSpeed : 1.5f;
        nextAttackTime = Time.time + cooldown;
    }

    bool CanChargeNow()
    {
        if (chargeOnlyBelowHalf)
        {
            float hp01 = currentHealth / Mathf.Max(1f, maxHealth);
            if (hp01 > 0.5f)
            {
                return false;
            }
        }

        return Time.time >= nextChargeTime;
    }

    void TryTriggerThresholdSkills()
    {
        float hp01 = currentHealth / Mathf.Max(1f, maxHealth);

        if (!triggered75 && hp01 <= 0.75f)
        {
            triggered75 = true;
            StartCoroutine(DoStomp());
            return;
        }

        if (!triggered50 && hp01 <= 0.50f)
        {
            triggered50 = true;
            StartCoroutine(DoSummon());
            return;
        }

        if (!triggered25 && hp01 <= 0.25f)
        {
            triggered25 = true;
            StartCoroutine(DoStomp());
            return;
        }
    }

    IEnumerator DoTentacleWhip()
    {
        isSkillRunning = true;
        state = BossState.Skill;
        StopMovement();

        if (tentaclePhases)
        {
            SetPhased(true);
        }

        Vector2 dir = GetFacingDirection();
        ShowConeTelegraph(dir, tentacleRadius, tentacleAngle);
        yield return new WaitForSeconds(tentacleWindup);
        HideTelegraphs();

        DealConeDamage(dir, tentacleRadius, tentacleAngle, normalAttackDamage);

        if (tentaclePhases)
        {
            SetPhased(false);
        }

        isSkillRunning = false;
        state = BossState.Chase;
    }

    IEnumerator DoCharge()
    {
        isSkillRunning = true;
        state = BossState.Skill;
        StopMovement();
        SetPhased(false);

        float windupElapsed = 0f;
        while (windupElapsed < chargeWindup)
        {
            Vector2 dir = GetFacingDirection();
            ShowRectTelegraph(dir, chargeBoxSize);
            windupElapsed += Time.deltaTime;
            yield return null;
        }
        HideTelegraphs();

        chargeDir = GetFacingDirection();
        float finalDistance = chargeDistanceMatchesTelegraph ? chargeBoxSize.y : chargeDistance;
        chargeRemaining = finalDistance;
        bool prevTrigger = false;
        if (col != null && chargeUseTriggerDuringDash)
        {
            prevTrigger = col.isTrigger;
            col.isTrigger = true;
        }

        isCharging = true;
        while (isCharging)
        {
            yield return null;
        }

        if (col != null && chargeUseTriggerDuringDash)
        {
            col.isTrigger = prevTrigger;
        }

        isSkillRunning = false;
        state = BossState.Chase;
    }

    IEnumerator DoStomp()
    {
        isSkillRunning = true;
        state = BossState.Skill;
        StopMovement();
        SetPhased(false);

        Vector2 dir = GetFacingDirection();
        ShowConeTelegraph(dir, stompRadius, stompAngle);
        yield return new WaitForSeconds(stompWindup);
        HideTelegraphs();

        for (int i = 0; i < stompWaves; i++)
        {
            DealConeDamage(dir, stompRadius, stompAngle, stompWaveDamage);
            yield return new WaitForSeconds(stompWaveInterval);
        }

        SetPhased(true);
        isSkillRunning = false;
        state = BossState.Chase;
    }

    IEnumerator DoSummon()
    {
        isSkillRunning = true;
        state = BossState.Skill;
        StopMovement();
        SetPhased(true);

        if (arenaCenter != null)
        {
            transform.position = arenaCenter.position;
        }

        for (int i = 0; i < cultistCount; i++)
        {
            Vector3 pos = GetSummonPoint(i);
            if (cultistPrefab == null)
            {
                break;
            }

            GameObject minion = Instantiate(cultistPrefab, pos, Quaternion.identity);
            MonsterAI ai = minion.GetComponent<MonsterAI>();
            if (ai != null)
            {
                ai.maxHealth = cultistMaxHealth;
                ai.currentHealth = cultistMaxHealth;
            }
            summonedMinions.Add(minion);
        }

        isSkillRunning = false;
        state = BossState.Chase;
        yield break;
    }

    void OnBossDeath()
    {
        for (int i = summonedMinions.Count - 1; i >= 0; i--)
        {
            GameObject minion = summonedMinions[i];
            if (minion != null)
            {
                Destroy(minion);
            }
        }
        summonedMinions.Clear();
    }

    Vector2 GetFacingDirection()
    {
        Vector2 dir = (target.position - transform.position).normalized;
        if (dir == Vector2.zero)
        {
            dir = transform.up;
        }
        if (rotateBossToTarget && dir != Vector2.zero)
        {
            transform.up = dir;
        }
        return dir;
    }

    Vector3 GetSummonPoint(int index)
    {
        if (summonPoints != null && summonPoints.Length > 0)
        {
            int i = Mathf.Clamp(index, 0, summonPoints.Length - 1);
            if (summonPoints[i] != null)
            {
                return summonPoints[i].position;
            }
        }

        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        if (summonOffsets != null && summonOffsets.Length > 0)
        {
            int i = index % summonOffsets.Length;
            return center + summonOffsets[i];
        }

        return center;
    }

    void SetPhased(bool on)
    {
        isPhased = on;
        if (col != null)
        {
            col.enabled = !on;
        }
        if (isPhased && maxPhasedDuration > 0f)
        {
            phasedUntil = Time.time + maxPhasedDuration;
        }
        ApplyPhaseVisual();
    }

    void ApplyPhaseVisual()
    {
        if (spriteRenderers == null)
        {
            return;
        }

        float a = isPhased ? phasedAlpha : 1f;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null)
            {
                continue;
            }
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    bool DealConeDamage(Vector2 dir, float radius, float angle, float damage)
    {
        bool hit = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, playerLayers);
        float half = angle * 0.5f;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            PlayerStatus ps = hits[i].GetComponentInParent<PlayerStatus>();
            if (ps == null)
            {
                continue;
            }

            Vector2 toTarget = (hits[i].transform.position - transform.position);
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            float a = Vector2.Angle(dir, toTarget.normalized);
            if (a > half)
            {
                continue;
            }

            ps.AddHealth(-damage);
            hit = true;
        }
        return hit;
    }

    bool DealBoxDamage(Vector2 dir, Vector2 size, float damage)
    {
        Vector2 center = (Vector2)transform.position + dir * (size.y * 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, playerLayers);
        bool hit = false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            PlayerStatus ps = hits[i].GetComponentInParent<PlayerStatus>();
            if (ps == null)
            {
                continue;
            }

            ps.AddHealth(-damage);
            hit = true;
        }
        return hit;
    }

    void SetupTelegraphs()
    {
        coneTelegraph = CreateTelegraphObject("ConeTelegraph", out coneFilter, out coneRenderer);
        rectTelegraph = CreateTelegraphObject("RectTelegraph", out rectFilter, out rectRenderer);
        HideTelegraphs();
    }

    GameObject CreateTelegraphObject(string name, out MeshFilter filter, out MeshRenderer renderer)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        filter = go.AddComponent<MeshFilter>();
        renderer = go.AddComponent<MeshRenderer>();

        Material mat = telegraphMaterial;
        if (mat == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                mat = new Material(shader);
            }
        }

        if (mat != null)
        {
            mat = new Material(mat);
            mat.color = telegraphColor;
            renderer.sharedMaterial = mat;
        }

        renderer.sortingLayerName = telegraphSortingLayer;
        renderer.sortingOrder = telegraphSortingOrder;
        return go;
    }

    void ShowConeTelegraph(Vector2 dir, float radius, float angle)
    {
        if (coneFilter == null)
        {
            return;
        }

        Mesh mesh = coneFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            coneFilter.sharedMesh = mesh;
        }

        mesh.Clear();

        int segments = 24;
        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] tris = new int[segments * 3];

        vertices[0] = Vector3.zero;
        float half = angle * 0.5f;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float ang = baseAngle - half + t * angle;
            float rad = ang * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;
            vertices[i + 1] = new Vector3(x, y, 0f);
        }

        int tri = 0;
        for (int i = 0; i < segments; i++)
        {
            tris[tri++] = 0;
            tris[tri++] = i + 1;
            tris[tri++] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        coneTelegraph.SetActive(true);
    }

    void ShowRectTelegraph(Vector2 dir, Vector2 size)
    {
        if (rectFilter == null)
        {
            return;
        }

        Mesh mesh = rectFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            rectFilter.sharedMesh = mesh;
        }

        mesh.Clear();
        Vector3[] vertices = new Vector3[4];
        int[] tris = new int[6];

        float halfX = size.x * 0.5f;
        vertices[0] = new Vector3(-halfX, 0f, 0f);
        vertices[1] = new Vector3(halfX, 0f, 0f);
        vertices[2] = new Vector3(halfX, size.y, 0f);
        vertices[3] = new Vector3(-halfX, size.y, 0f);

        tris[0] = 0; tris[1] = 2; tris[2] = 1;
        tris[3] = 0; tris[4] = 3; tris[5] = 2;

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        rectTelegraph.transform.localPosition = Vector3.zero;
        float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        rectTelegraph.transform.rotation = Quaternion.Euler(0f, 0f, z);
        rectTelegraph.SetActive(true);
    }

    void HideTelegraphs()
    {
        if (coneTelegraph != null)
        {
            coneTelegraph.SetActive(false);
        }
        if (rectTelegraph != null)
        {
            rectTelegraph.SetActive(false);
        }
    }

    public void TakeDamage(float amount)
    {
        ApplyHit(amount, Vector2.zero, false);
    }

    public void ApplyHit(float amount, Vector2 hitDir, bool isMelee)
    {
        if (isDead || isPhased)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        TriggerFlash();
        SpawnHitVfx(hitDir);
        PlayHitSfx();
        StartKnockback(hitDir);
        isCharging = false;
        shouldMove = false;

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

    void SetMoving(bool isMoving)
    {
        if (animator != null && !string.IsNullOrEmpty(moveBool))
        {
            animator.SetBool(moveBool, isMoving);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!chargeDamageOnContact || !isCharging)
        {
            return;
        }

        PlayerStatus ps = collision.collider.GetComponentInParent<PlayerStatus>();
        if (ps == null)
        {
            return;
        }

        ps.AddHealth(-normalAttackDamage);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!chargeDamageOnContact || !isCharging)
        {
            return;
        }

        PlayerStatus ps = other.GetComponentInParent<PlayerStatus>();
        if (ps == null)
        {
            return;
        }

        ps.AddHealth(-normalAttackDamage);
    }
}
