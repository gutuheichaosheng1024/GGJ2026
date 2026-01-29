using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SortByYOrder : MonoBehaviour
{
    [Header("Sorting")]
    [Tooltip("Bigger value = larger separation between layers.")]
    public float orderMultiplier = 10f;

    [Tooltip("Base sorting order offset.")]
    public int baseOrder = 1000;

    [Tooltip("Clamp sorting order to avoid extreme values.")]
    public int minOrder = 1;

    [Tooltip("Clamp sorting order to avoid extreme values.")]
    public int maxOrder = 20000;

    [Header("Traversal")]
    [Tooltip("If true, traverse children under this object (e.g. Environment root).")]
    public bool traverseChildren = true;

    [Tooltip("If true, include this object as well.")]
    public bool includeSelf = true;

    [Tooltip("If true, include inactive objects when traversing.")]
    public bool includeInactive = true;

    [Header("Filtering")]
    [Tooltip("Only include objects in these layers.")]
    public LayerMask includeLayers = ~0;

    [Tooltip("Exclude objects in these layers.")]
    public LayerMask excludeLayers = 0;

    [Tooltip("Use SpriteRenderer bounds center for height instead of transform position.")]
    public bool useRendererBoundsCenter = false;

    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>(256);

    void Awake()
    {
        CacheRenderers();
    }

    void OnEnable()
    {
        CacheRenderers();
    }

    void OnTransformChildrenChanged()
    {
        CacheRenderers();
    }

    void LateUpdate()
    {
        if (renderers.Count == 0)
        {
            CacheRenderers();
        }

        for (int i = 0; i < renderers.Count; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
            {
                continue;
            }

            float y = useRendererBoundsCenter ? sr.bounds.center.y : sr.transform.position.y;
            int order = baseOrder - Mathf.RoundToInt(y * orderMultiplier);
            if (order < minOrder) order = minOrder;
            if (order > maxOrder) order = maxOrder;
            sr.sortingOrder = order;
        }
    }

    public void CacheRenderers()
    {
        renderers.Clear();

        if (!traverseChildren)
        {
            CollectFromTransform(transform);
            return;
        }

        Transform[] all = GetComponentsInChildren<Transform>(includeInactive);
        for (int i = 0; i < all.Length; i++)
        {
            if (!includeSelf && all[i] == transform)
            {
                continue;
            }
            CollectFromTransform(all[i]);
        }
    }

    void CollectFromTransform(Transform t)
    {
        if (t == null)
        {
            return;
        }

        if (!IsInLayerMask(t.gameObject.layer, includeLayers))
        {
            return;
        }

        if (IsInLayerMask(t.gameObject.layer, excludeLayers))
        {
            return;
        }

        SpriteRenderer[] srs = t.GetComponents<SpriteRenderer>();
        if (srs == null || srs.Length == 0)
        {
            return;
        }

        for (int i = 0; i < srs.Length; i++)
        {
            renderers.Add(srs[i]);
        }
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
