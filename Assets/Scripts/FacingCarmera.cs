using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacingCarmera : MonoBehaviour
{
    Transform[] childs;
    // Start is called before the first frame update
    void Start()
    {
        CacheChildren();
    }

    void OnTransformChildrenChanged()
    {
        CacheChildren();
    }

    // Update is called once per frame
    void Update()
    {
        if (childs == null || childs.Length == 0)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        for (int i = 0; i < childs.Length; i++)
        {
            if (childs[i] == null)
            {
                continue;
            }
            childs[i].rotation = cam.transform.rotation;//让节点上的子物体与相机旋转角一致
        }
    }

    void CacheChildren()
    {
        int count = transform.childCount;
        childs = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            childs[i] = transform.GetChild(i);
        }
    }
}
