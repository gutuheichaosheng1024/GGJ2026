using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossAI : MonsterAI
{
    public enum BossState
    {
        Idle,
        Chase,
        Attack,
        Skill,
        Dead
    }

    [Header("Boss Stats")]
    public float normalAttackDamage = 2f;
    [Tooltip("Attacks per second. 0.65 ~= 1.54s cooldown.")]
    public float attackSpeed = 0.65f;
    public float attackRangeOverride = 2.2f;
    public LayerMask playerLayers = ~0;
    public bool logDebug = false;
    public bool logSpeedDebug = false;
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
    Rigidbody2D bossRb;
    Collider2D bossCol;
    bool isPhased;
    bool isSkillRunning;
    float nextAttackTime;
    float nextChargeTime;
    float phasedUntil;
    Vector3 lastPos;
    float lastSpeedLogTime;
    Vector2 desiredVelocity;
    bool shouldMove;
    bool isCharging;
    Vector2 chargeDir;
    float chargeRemaining;

    bool triggered75;
    bool triggered50;
    bool triggered25;
    int activeSummonCount;
    readonly List<GameObject> summonedMinions = new List<GameObject>();

    GameObject coneTelegraph;
    MeshFilter coneFilter;
    MeshRenderer coneRenderer;

    GameObject rectTelegraph;
    MeshFilter rectFilter;
    MeshRenderer rectRenderer;

    SpriteRenderer[] spriteRenderers;

    void Start()
    {
        bossRb = GetComponent<Rigidbody2D>();
        bossCol = GetComponent<Collider2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
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

        SetupTelegraphs();
        SetPhased(startPhased);
        lastPos = transform.position;
        lastSpeedLogTime = Time.time;
    }

    protected override void Update()
    {
        if (isPhased && maxPhasedDuration > 0f && Time.time >= phasedUntil)
        {
            SetPhased(false);
        }

        if (state == BossState.Dead)
        {
            return;
        }

        if (target == null)
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
        if (logDebug)
        {
            Debug.Log($"[BossAI] state={state} skill={isSkillRunning} dist={distance:F2} range={attackRangeOverride:F2} move={moveSpeed:F2}");
        }
        if (logSpeedDebug && Time.time - lastSpeedLogTime >= 1f)
        {
            float dt = Mathf.Max(0.0001f, Time.time - lastSpeedLogTime);
            float speed = Vector3.Distance(transform.position, lastPos) / dt;
            Debug.Log($"[BossAI] realSpeed={speed:F2} timeScale={Time.timeScale:F2}");
            lastPos = transform.position;
            lastSpeedLogTime = Time.time;
        }
        if (distance <= attackRangeOverride)
        {
            state = BossState.Attack;
            TryAttack();
            shouldMove = false;
        }
        else
        {
            if (distance > stopDistance)
            {
                state = BossState.Chase;
                UpdateDesiredVelocity();
                shouldMove = true;
            }
            else
            {
                shouldMove = false;
            }
        }
    }

    protected override void FixedUpdate()
    {
        if (isCharging)
        {
            float step = chargeSpeed * Time.fixedDeltaTime;
            chargeRemaining -= step;
            Vector2 delta = chargeDir * step;
            if (bossRb != null)
            {
                bossRb.MovePosition(bossRb.position + delta);
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

        if (bossRb == null)
        {
            if (shouldMove)
            {
                transform.position += (Vector3)(desiredVelocity * Time.fixedDeltaTime);
            }
            return;
        }

        if (shouldMove)
        {
            Vector2 next = bossRb.position + desiredVelocity * Time.fixedDeltaTime;
            bossRb.MovePosition(next);
        }
        else
        {
            bossRb.velocity = Vector2.zero;
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
        if (bossRb != null)
        {
            bossRb.velocity = Vector2.zero;
        }
    }


    void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        float hp01 = currentHealth / Mathf.Max(1f, maxHealth);
        bool canCharge = !chargeOnlyBelowHalf || hp01 <= 0.5f;

        if (canCharge && Time.time >= nextChargeTime)
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
        if (bossCol != null && chargeUseTriggerDuringDash)
        {
            prevTrigger = bossCol.isTrigger;
            bossCol.isTrigger = true;
        }
        isCharging = true;
        while (isCharging)
        {
            yield return null;
        }

        if (bossCol != null && chargeUseTriggerDuringDash)
        {
            bossCol.isTrigger = prevTrigger;
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
            activeSummonCount++;
            summonedMinions.Add(minion);
        }

        isSkillRunning = false;
        state = BossState.Chase;
        yield break;
    }


    void OnSummonMinionDeath()
    {
        activeSummonCount = Mathf.Max(0, activeSummonCount - 1);
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
        if (bossCol != null)
        {
            bossCol.enabled = !on;
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!chargeDamageOnContact)
        {
            return;
        }

        if (!isSkillRunning)
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
        if (!chargeDamageOnContact)
        {
            return;
        }

        if (!isSkillRunning)
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
        float halfY = size.y * 0.5f;
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
}
