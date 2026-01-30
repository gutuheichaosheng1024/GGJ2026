using Manager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [BuildSceneSelector]
    [SerializeField]private string NextScene = string.Empty;
    [SerializeField]private Button StartGame;
    [SerializeField] private Button ExitGame;
    void Start()
    {
        StartGame.onClick.AddListener(()=>SceneTransitionManager.Instance.LoadScene(NextScene));
        ExitGame.onClick.AddListener(() => Application.Quit());
    }

}
