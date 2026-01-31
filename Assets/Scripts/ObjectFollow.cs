using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectFollow : MonoBehaviour
{
    [SerializeField] private GameObject follow;
    [SerializeField] private float followRange = 5.0f;
    [SerializeField] private bool limitedFollow = true;
    private Vector2 RandomDir;
    private Vector3 originPosition;

    private void Awake()
    {
        originPosition = transform.position;
        RandomDir = new Vector2(1, 1).normalized;
    }

    private void Update()
    {
        if (follow != null)
        {
            Vector3 targetPosition = follow.transform.position;
            if (limitedFollow)
            {
                Vector2 dir = (targetPosition - originPosition).normalized;
                transform.position = originPosition + (Vector3)dir * followRange;
            }
            else
            {
                transform.position = targetPosition + (Vector3)RandomDir * followRange;
            }
        }
    }
}
