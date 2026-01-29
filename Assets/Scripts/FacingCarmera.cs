using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacingCarmera : MonoBehaviour
{
    Transform[] childs;
    // Start is called before the first frame update
    void Start()
    {
        childs = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            childs[i] = transform.GetChild(i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < childs.Length; i++)
        {
            childs[i].rotation = Camera.main.transform.rotation;//让节点上的子物体与相机旋转角一致
        }
    }
}
