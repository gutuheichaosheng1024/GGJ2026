using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


class Task
{
    public float TimeLeft;
    public Action callBack;

    public Task(float timeLeft, Action callBack)
    {
        TimeLeft = timeLeft;
        this.callBack = callBack;
    }
}

public class TimeHandler : MonoBehaviour
{
    static List<Task> tasks = new List<Task>();

    private static TimeHandler instance;

    private void Awake()
    {

        if (instance != this)
        {
            instance = this;
            SceneManager.sceneUnloaded += OnSceneUnLoad;
        }
    }
    public static void Register(float TimeFromNow,Action callBack)
    {
        tasks.Add(new Task(TimeFromNow, callBack));
        tasks.Sort((x, y) => x.TimeLeft.CompareTo(y.TimeLeft));
    }

    private void FixedUpdate()
    {
        int deleteIndex = -1;
        for (int i = 0; i < tasks.Count; i++) 
        {
            tasks[i].TimeLeft -= Time.fixedDeltaTime;
            if(tasks[i].TimeLeft < 0 )deleteIndex = i;
        }
        for(int i = 0; i <= deleteIndex; i++)
        {
            tasks[i].callBack();
        }
        tasks.RemoveRange(0, deleteIndex);
    }

    void OnSceneUnLoad(Scene scene)
    {
        tasks.Clear();
    }
}
