using UnityEngine;

[DisallowMultipleComponent]
public class Bullet : MonoBehaviour
{
    public enum HitMode
    {
        HitFirst,
        Pierce
    }

    [Header("Motion")]
    public float speed = 8f;
    public float maxDistance = 6f;

    [Header("Damage")]
    public float damage = 10f;
    public HitMode hitMode = HitMode.HitFirst;
    public LayerMask targetLayers = ~0;

    private Vector2 direction = Vector2.up;
    private Vector3 startPosition;

    public void Initialize(Vector2 dir, float newSpeed, float newMaxDistance, float newDamage, HitMode newMode, LayerMask layers)
    {
        direction = dir.normalized;
        speed = newSpeed;
        maxDistance = newMaxDistance;
        damage = newDamage;
        hitMode = newMode;
        targetLayers = layers;
        startPosition = transform.position;
    }

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject.layer, targetLayers))
        {
            return;
        }

        MonsterAI monster = other.GetComponentInParent<MonsterAI>();
        if (monster != null)
        {
            monster.TakeDamage(damage);
            if (hitMode == HitMode.HitFirst)
            {
                Destroy(gameObject);
            }
        }
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
