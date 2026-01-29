using System.Collections;
using System.Collections.Generic;
using SK.Framework;
using UnityEngine;

public class RotateCarmera: MonoBehaviour
{
    public float rotateTime = 0.2f;//旋转所花费时间
    private Transform player;
    private bool isRotating = false;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = player.position;
        Rotate();
    }
    void Rotate()
    {
        if (Input.GetKeyDown(KeyCode.Q) ||Input.GetKeyDown(XBox.LB) && !isRotating)
        {
            StartCoroutine(RotateAround(-45, rotateTime));
        }
        if (Input.GetKeyDown(KeyCode.E)|| Input.GetKeyDown(XBox.RB) && !isRotating)
        {
            StartCoroutine(RotateAround(45, rotateTime));
        }
    }
    //使用协程函数来更新镜头旋转角度 
    IEnumerator RotateAround(float angel,float time)
    {
        float number = 60 * time;
        float nextAngel = angel / number;
        isRotating = true;
        for(int i = 0; i < number; i++)
        {
            transform.Rotate(new Vector3(0, 0, nextAngel));
            yield return new WaitForFixedUpdate();//暂停执行 等到下一帧时继续执行下个循环
            //默认FixedUpdate()一秒更新60帧
            //使用其他频率 修改number前帧数 例如100 这里使用waitforseconds(0.01f)

        }
        isRotating = false;
    }
}
