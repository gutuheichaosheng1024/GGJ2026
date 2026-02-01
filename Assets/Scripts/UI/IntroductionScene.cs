using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroductionScene : MonoBehaviour
{
    [SerializeField] Sprite[] images;
    private int current = 0;

    [SerializeField] private Image show;
    [SerializeField] private Button next;
    [SerializeField] private Button pre;

    [BuildSceneSelector]
    [SerializeField] string nextScene;

    private void Awake()
    {
        next.onClick.AddListener(() => Change(1));
        pre.onClick.AddListener(() => Change(-1));
    }

    void Change(int dir)
    {
        current = Math.Clamp(current+dir,0,images.Length);
        if (current == images.Length)
        {
            Manager.SceneTransitionManager.Instance.LoadScene(nextScene);
        }
        else
        {
            show.sprite = images[current];
        }
    }

}
