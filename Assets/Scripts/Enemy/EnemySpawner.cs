using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject enemyPrefab;
    [Tooltip("Optional. If set (>=2), spawner will pick randomly from this list.")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("Optional weights for enemyPrefabs (same size). If empty, uniform random.")]
    public List<float> enemyWeights = new List<float>();
    public Transform player;
    public PlayerStatus playerStatus;
    [Tooltip("Spawned enemies will be parented under this transform if set.")]
    public Transform spawnParent;
    [Tooltip("Kill counter to bind to enemy death events.")]
    public BattleUI BattleUI;

    [Header("Spawn Area")]
    public float minSpawnRadius = 5f;
    public float maxSpawnRadius = 12f;
    public LayerMask blockedLayers = 0;
    public int maxPositionTries = 8;

    [Header("SAN -> Rate")]
    public float minInterval = 0.5f;
    public float maxInterval = 3.0f;
    public float spawnRateMultiplier = 1f;

    [Header("SAN -> Count")]
    public int minCount = 1;
    public int maxCount = 4;
    public float spawnCountMultiplier = 1f;

    [Header("SAN Step Table")]
    [Tooltip("Sanity thresholds (percent). 60/40/20/0 => 4 steps.")]
    public float[] sanSteps = new float[] { 60f, 40f, 20f, 0f };
    [Tooltip("Spawn interval per step. 1,1,0.5,0.5")]
    public float[] stepIntervals = new float[] { 1f, 1f, 0.5f, 0.5f };
    [Tooltip("Spawn count per step. 2,4,4,8")]
    public int[] stepCounts = new int[] { 2, 4, 4, 8 };

    [Header("Limits")]
    public int maxAlive = 12;

    private float nextSpawnTime;
    private readonly List<GameObject> alive = new List<GameObject>(128);

    void Start()
    {
        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.transform;
            }
        }

        if (playerStatus == null && player != null)
        {
            playerStatus = player.GetComponent<PlayerStatus>();
        }

        if (BattleUI == null)
        {
            BattleUI = FindFirstObjectByType<BattleUI>();
        }

        ScheduleNextSpawn();
    }

    void Update()
    {
        CleanupDead();

        if (GetRandomPrefab() == null || player == null)
        {
            return;
        }

        if (Time.time >= nextSpawnTime)
        {
            SpawnWave();
            ScheduleNextSpawn();
        }
    }

    void SpawnWave()
    {
        if (alive.Count >= maxAlive)
        {
            return;
        }

        int count = GetSpawnCount();
        int canSpawn = Mathf.Clamp(maxAlive - alive.Count, 0, count);
        for (int i = 0; i < canSpawn; i++)
        {
            Vector3 pos;
            if (!TryGetSpawnPosition(out pos))
            {
                continue;
            }

            GameObject prefab = GetRandomPrefab();
            if (prefab == null)
            {
                return;
            }
            GameObject enemy = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
            BindKillCounter(enemy);
            alive.Add(enemy);
        }
    }

    void BindKillCounter(GameObject enemy)
    {
        if (enemy == null || BattleUI == null)
        {
            return;
        }

        MonsterAI ai = enemy.GetComponent<MonsterAI>();
        if (ai != null && ai.onDeath != null)
        {
            ai.onDeath.AddListener(BattleUI.AddKill);
        }
    }

    bool TryGetSpawnPosition(out Vector3 pos)
    {
        for (int i = 0; i < maxPositionTries; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 candidate = player.position + (Vector3)(dir * dist);
            candidate.z = player.position.z;

            if (blockedLayers != 0)
            {
                if (Physics2D.OverlapCircle(candidate, 0.2f, blockedLayers) != null)
                {
                    continue;
                }
            }

            pos = candidate;
            return true;
        }

        pos = Vector3.zero;
        return false;
    }

    int GetSpawnCount()
    {
        if (stepCounts != null && stepCounts.Length > 0)
        {
            int idx = GetSanStepIndex();
            int count = stepCounts[Mathf.Clamp(idx, 0, stepCounts.Length - 1)];
            count = Mathf.RoundToInt(count * Mathf.Max(0.1f, spawnCountMultiplier));
            return Mathf.Max(1, count);
        }

        float san01 = GetSan01();
        float raw = Mathf.Lerp(maxCount, minCount, san01);
        raw *= Mathf.Max(0.1f, spawnCountMultiplier);
        int fallback = Mathf.RoundToInt(raw);
        return Mathf.Clamp(fallback, minCount, maxCount);
    }

    float GetSpawnInterval()
    {
        if (stepIntervals != null && stepIntervals.Length > 0)
        {
            int idx = GetSanStepIndex();
            float interval = stepIntervals[Mathf.Clamp(idx, 0, stepIntervals.Length - 1)];
            interval *= Mathf.Max(0.1f, spawnRateMultiplier);
            return Mathf.Max(0.05f, interval);
        }

        float san01 = GetSan01();
        float fallback = Mathf.Lerp(minInterval, maxInterval, san01);
        fallback *= Mathf.Max(0.1f, spawnRateMultiplier);
        return Mathf.Max(0.05f, fallback);
    }

    int GetSanStepIndex()
    {
        float sanPercent = 100f * GetSan01();
        if (sanSteps == null || sanSteps.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < sanSteps.Length; i++)
        {
            if (sanPercent >= sanSteps[i])
            {
                return i;
            }
        }

        return sanSteps.Length - 1;
    }

    float GetSan01()
    {
        if (playerStatus == null || playerStatus.maxSan <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(playerStatus.currentSan / playerStatus.maxSan);
    }

    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + GetSpawnInterval();
    }

    void CleanupDead()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null)
            {
                alive.RemoveAt(i);
            }
        }
    }

    GameObject GetRandomPrefab()
    {
        if (enemyPrefabs != null && enemyPrefabs.Count >= 2)
        {
            int count = enemyPrefabs.Count;
            if (enemyWeights != null && enemyWeights.Count == count)
            {
                float total = 0f;
                for (int i = 0; i < count; i++)
                {
                    total += Mathf.Max(0f, enemyWeights[i]);
                }

                if (total > 0f)
                {
                    float pick = Random.value * total;
                    float acc = 0f;
                    for (int i = 0; i < count; i++)
                    {
                        acc += Mathf.Max(0f, enemyWeights[i]);
                        if (pick <= acc)
                        {
                            return enemyPrefabs[i];
                        }
                    }
                }
            }

            int idx = Random.Range(0, count);
            return enemyPrefabs[idx];
        }

        return enemyPrefab;
    }
}
