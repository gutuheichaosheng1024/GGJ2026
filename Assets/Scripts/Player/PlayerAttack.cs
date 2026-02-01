using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerAttack : MonoBehaviour
{
    public enum AttackMode
    {
        Cone,
        Circle,
        Ranged
    }

    [System.Serializable]
    public class AttackConfig
    {
        [Header("Attack")]
        public float radius = 2f;
        [Range(5f, 180f)]
        public float angle = 90f;
        public float damage = 15f;
        public float cooldown = 0.5f;
        public int meshSegments = 24;

        [Header("Preview")]
        public float previewDuration = 0.15f;
        public Color previewColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
        public Material previewMaterial;

        [Header("Animation / FX")]
        public string attackTrigger = "Attack";
        public GameObject attackEffectPrefab;
        public float effectLifetime = 1.0f;
    }

    [System.Serializable]
    public class RangedConfig
    {
        [Header("Ranged")]
        public GameObject projectilePrefab;
        public float projectileSpeed = 8f;
        public float projectileRange = 6f;
        public float projectileDamage = 10f;
        public Bullet.HitMode hitMode = Bullet.HitMode.HitFirst;

        [Header("Animation / FX")]
        public string attackTrigger = "Attack";
        public GameObject attackEffectPrefab;
        public float effectLifetime = 1.0f;

        [Header("Cooldown")]
        public float cooldown = 0.6f;
    }

    [Header("Mode")]
    public AttackMode attackMode = AttackMode.Cone;
    public KeyCode cycleKey = KeyCode.Alpha3;
    public KeyCode maskToggleKey = KeyCode.Alpha2;

    [Header("Configs")]
    public AttackConfig coneConfig = new AttackConfig();
    public AttackConfig circleConfig = new AttackConfig();
    public RangedConfig rangedConfig = new RangedConfig();

    [Header("Common")]
    public LayerMask targetLayers = ~0;
    public Animator animator;
    public UnityEvent onAttack;
    public bool logModeEvents = false;

    [Header("UI (Optional Direct Link)")]
    public WeaponHotbarUI weaponHotbarUI;
    public PostFXController postFxController;

    [Header("Hit Stop (Melee)")]
    public bool useHitStop = true;
    [Range(0.01f, 0.2f)]
    public float hitStopDuration = 0.06f;
    [Range(0f, 0.2f)]
    public float hitStopTimeScale = 0.0f;
    public HitStop hitStop;

    [Header("Camera Shake (Melee)")]
    public bool useCameraShake = true;
    public float shakeStrength = 0.15f;
    public float shakeDuration = 0.06f;
    public CameraShake cameraShake;

    [Header("Preview")]
    [Tooltip("Optional preview holder. If empty, a child will be created.")]
    public Transform previewRoot;
    public string previewObjectName = "AttackPreview";

    [Header("Input")]
    public int mouseButton = 0;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private float previewUntilTime;
    private Vector2 lastAttackDir;
    private float lastConeAttackTime;
    private float lastCircleAttackTime;
    private float lastRangedAttackTime;
    private PlayerStatus status;

    void Awake()
    {
        EnsurePreviewObject();
        status = PlayerStatus.Instance;
        if (postFxController == null)
        {
            postFxController = FindFirstObjectByType<PostFXController>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (hitStop == null)
        {
            hitStop = FindFirstObjectByType<HitStop>();
        }

        if (cameraShake == null)
        {
            cameraShake = FindFirstObjectByType<CameraShake>();
        }

        SetupPreviewRenderer();
        HidePreview();
    }

    void Start()
    {
        if (postFxController != null && status != null)
        {
            postFxController.SetMask(status.isMaskOn);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(cycleKey))
        {
            CycleAttackMode();
        }
        if (Input.GetKeyDown(maskToggleKey) && status != null)
        {
            status.ToggleMask();
            if (postFxController != null)
            {
                postFxController.SetMask(status.isMaskOn);
            }
        }

        if (Input.GetMouseButton(mouseButton))
        {
            TryAttack();
        }

        if (Time.time > previewUntilTime)
        {
            HidePreview();
        }
    }

    public void SetAttackMode(AttackMode mode)
    {
        attackMode = mode;
        SetupPreviewRenderer();
        if (attackMode == AttackMode.Ranged)
        {
            HidePreview();
        }
        if (logModeEvents)
        {
            Debug.Log($"[PlayerAttack] Mode -> {attackMode} ({(int)attackMode})");
        }
        if (weaponHotbarUI != null)
        {
            weaponHotbarUI.SetWeaponIndexAnimated((int)attackMode);
        }
    }

    public void CycleAttackMode()
    {
        int next = ((int)attackMode + 1) % 3;
        SetAttackMode((AttackMode)next);
    }

    void TryAttack()
    {
        if (attackMode == AttackMode.Ranged)
        {
            TryRangedAttack();
        }
        else
        {
            TryMeleeAttack();
        }
    }

    void TryMeleeAttack()
    {
        AttackConfig cfg = GetMeleeConfig();
        if (cfg == null)
        {
            return;
        }

        float cooldown = GetMeleeCooldown(cfg);
        if (!CanAttack(cfg, cooldown))
        {
            return;
        }

        RecordAttack(cfg);

        Vector2 attackDir = GetMouseDirection();
        if (attackDir == Vector2.zero)
        {
            attackDir = Vector2.up;
        }

        lastAttackDir = attackDir;
        ShowPreview(attackDir, cfg);

        if (animator != null && !string.IsNullOrEmpty(cfg.attackTrigger))
        {
            animator.SetTrigger(cfg.attackTrigger);
        }

        if (cfg.attackEffectPrefab != null)
        {
            GameObject fx = Instantiate(cfg.attackEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, Mathf.Max(0.01f, cfg.effectLifetime));
        }

        onAttack?.Invoke();
        float damage = GetMeleeDamage(cfg);
        float radius = GetMeleeRadius(cfg);
        bool hitAny = ApplyDamage(attackDir, cfg, damage, radius);

        if (hitAny)
        {
            if (useHitStop && hitStop != null)
            {
                hitStop.Trigger(hitStopDuration, hitStopTimeScale);
            }

            if (useCameraShake && cameraShake != null)
            {
                cameraShake.Shake(shakeDuration, shakeStrength);
            }
        }
    }

    void TryRangedAttack()
    {
        if (rangedConfig == null)
        {
            return;
        }

        float cooldown = GetRangedCooldown(rangedConfig);
        if (Time.time - lastRangedAttackTime < cooldown)
        {
            return;
        }

        if (rangedConfig.projectilePrefab == null)
        {
            return;
        }

        lastRangedAttackTime = Time.time;

        Vector2 attackDir = GetMouseDirection();
        if (attackDir == Vector2.zero)
        {
            attackDir = Vector2.up;
        }

        if (animator != null && !string.IsNullOrEmpty(rangedConfig.attackTrigger))
        {
            animator.SetTrigger(rangedConfig.attackTrigger);
        }

        if (rangedConfig.attackEffectPrefab != null)
        {
            GameObject fx = Instantiate(rangedConfig.attackEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, Mathf.Max(0.01f, rangedConfig.effectLifetime));
        }

        onAttack?.Invoke();

        float damage = GetRangedDamage(rangedConfig);
        float range = GetRangedRange(rangedConfig);
        SpawnProjectile(attackDir, damage, range);
    }

    void SpawnProjectile(Vector2 direction, float damage, float range)
    {
        if (rangedConfig.projectilePrefab == null)
        {
            return;
        }

        Quaternion rot = Quaternion.LookRotation(Vector3.forward, direction);
        GameObject proj = Instantiate(rangedConfig.projectilePrefab, transform.position, rot);
        Bullet bullet = proj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Initialize(direction, rangedConfig.projectileSpeed, range, damage, rangedConfig.hitMode, targetLayers);
        }
    }

    bool ApplyDamage(Vector2 attackDir, AttackConfig cfg, float damage, float radius)
    {
        bool hitAny = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayers);
        float halfAngle = cfg.angle * 0.5f;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            Vector2 toTarget = (hits[i].transform.position - transform.position);
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            if (attackMode == AttackMode.Cone)
            {
                float a = Vector2.Angle(attackDir, toTarget.normalized);
                if (a > halfAngle)
                {
                    continue;
                }
            }

            MonsterAI monster = hits[i].GetComponentInParent<MonsterAI>();
            if (monster != null)
            {
                monster.ApplyHit(damage, toTarget.normalized, true);
                hitAny = true;
                continue;
            }

            BossAI2 boss = hits[i].GetComponentInParent<BossAI2>();
            if (boss != null)
            {
                boss.ApplyHit(damage, toTarget.normalized, true);
                hitAny = true;
            }
        }

        return hitAny;
    }

    void ShowPreview(Vector2 direction, AttackConfig cfg)
    {
        previewUntilTime = Time.time + cfg.previewDuration;
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }

        UpdatePreviewMesh(direction, cfg);
    }

    void HidePreview()
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    void SetupPreviewRenderer()
    {
        if (meshRenderer == null)
        {
            return;
        }

        AttackConfig cfg = GetMeleeConfig();
        if (cfg == null)
        {
            return;
        }

        Material mat = cfg.previewMaterial;
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
            mat.color = cfg.previewColor;
            meshRenderer.sharedMaterial = mat;
        }
    }

    void UpdatePreviewMesh(Vector2 direction, AttackConfig cfg)
    {
        if (meshFilter == null)
        {
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            meshFilter.sharedMesh = mesh;
        }

        mesh.Clear();

        if (attackMode == AttackMode.Circle)
        {
            BuildCircleMesh(mesh, cfg);
        }
        else
        {
            BuildConeMesh(mesh, direction, cfg);
        }
    }

    Vector2 GetMouseDirection()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return Vector2.zero;
        }

        Plane plane = new Plane(Vector3.forward, transform.position);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            Vector2 dir = (hit - transform.position);
            return dir.normalized;
        }

        return Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        AttackConfig cfg = GetMeleeConfig();
        if (cfg == null)
        {
            return;
        }

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, cfg.radius);
    }

    void BuildCircleMesh(Mesh mesh, AttackConfig cfg)
    {
        int segments = Mathf.Max(6, cfg.meshSegments);
        int vertexCount = segments + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            float ang = t * Mathf.PI * 2f;
            float x = Mathf.Cos(ang) * cfg.radius;
            float y = Mathf.Sin(ang) * cfg.radius;
            vertices[i + 1] = new Vector3(x, y, 0f);
        }

        int tri = 0;
        for (int i = 0; i < segments; i++)
        {
            int a = i + 1;
            int b = i == segments - 1 ? 1 : i + 2;
            triangles[tri++] = 0;
            triangles[tri++] = a;
            triangles[tri++] = b;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    void BuildConeMesh(Mesh mesh, Vector2 direction, AttackConfig cfg)
    {
        int segments = Mathf.Max(3, cfg.meshSegments);
        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        float halfAngle = cfg.angle * 0.5f;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float ang = baseAngle - halfAngle + t * cfg.angle;
            float rad = ang * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * cfg.radius;
            float y = Mathf.Sin(rad) * cfg.radius;
            vertices[i + 1] = new Vector3(x, y, 0f);
        }

        int tri = 0;
        for (int i = 0; i < segments; i++)
        {
            triangles[tri++] = 0;
            triangles[tri++] = i + 1;
            triangles[tri++] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    void EnsurePreviewObject()
    {
        if (previewRoot == null)
        {
            Transform existing = transform.Find(previewObjectName);
            if (existing != null)
            {
                previewRoot = existing;
            }
            else
            {
                GameObject go = new GameObject(previewObjectName);
                go.transform.SetParent(transform, false);
                previewRoot = go.transform;
            }
        }

        meshFilter = previewRoot.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = previewRoot.gameObject.AddComponent<MeshFilter>();
        }

        meshRenderer = previewRoot.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = previewRoot.gameObject.AddComponent<MeshRenderer>();
        }
    }

    AttackConfig GetMeleeConfig()
    {
        return attackMode == AttackMode.Cone ? coneConfig : circleConfig;
    }

    bool CanAttack(AttackConfig cfg, float cooldown)
    {
        float lastTime = attackMode == AttackMode.Cone ? lastConeAttackTime : lastCircleAttackTime;
        return Time.time - lastTime >= cooldown;
    }

    void RecordAttack(AttackConfig cfg)
    {
        if (attackMode == AttackMode.Cone)
        {
            lastConeAttackTime = Time.time;
        }
        else
        {
            lastCircleAttackTime = Time.time;
        }
    }

    float GetMeleeDamage(AttackConfig cfg)
    {
        float baseDamage = cfg.damage;
        if (status == null || status.maskBuff == null || !status.isMaskOn) return baseDamage;
        float add = status.maskBuff.GetDamageAdd(attackMode);
        float mul = status.maskBuff.GetDamageMul(attackMode);
        return (baseDamage + add) * mul;
    }

    float GetMeleeRadius(AttackConfig cfg)
    {
        float baseRadius = cfg.radius;
        if (status == null || status.maskBuff == null || !status.isMaskOn) return baseRadius;
        float add = status.maskBuff.GetRadiusAdd(attackMode);
        float mul = status.maskBuff.GetRadiusMul(attackMode);
        return Mathf.Max(0.01f, (baseRadius + add) * mul);
    }

    float GetMeleeCooldown(AttackConfig cfg)
    {
        float baseCd = cfg.cooldown;
        if (status == null || status.maskBuff == null || !status.isMaskOn) return baseCd;
        float add = status.maskBuff.GetCooldownAdd(attackMode);
        float mul = status.maskBuff.GetCooldownMul(attackMode);
        return Mathf.Max(0.05f, (baseCd - add) * mul);
    }

    float GetRangedDamage(RangedConfig cfg)
    {
        float baseDamage = cfg.projectileDamage;
        if (status == null || status.maskBuff == null || !status.isMaskOn) return baseDamage;
        float add = status.maskBuff.GetDamageAdd(AttackMode.Ranged);
        float mul = status.maskBuff.GetDamageMul(AttackMode.Ranged);
        return (baseDamage + add) * mul;
    }

    float GetRangedRange(RangedConfig cfg)
    {
        float baseRange = cfg.projectileRange;
        if (status == null || status.maskBuff == null || !status.isMaskOn) return baseRange;
        float add = status.maskBuff.GetRangeAdd(AttackMode.Ranged);
        float mul = status.maskBuff.GetRangeMul(AttackMode.Ranged);
        return Mathf.Max(0.01f, (baseRange + add) * mul);
    }

    float GetRangedCooldown(RangedConfig cfg)
    {
        float baseCd = cfg.cooldown;
        if (status == null || status.maskBuff == null || !status.isMaskOn) return baseCd;
        float add = status.maskBuff.GetCooldownAdd(AttackMode.Ranged);
        float mul = status.maskBuff.GetCooldownMul(AttackMode.Ranged);
        return Mathf.Max(0.05f, (baseCd - add) * mul);
    }
}
