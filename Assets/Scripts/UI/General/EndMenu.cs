using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndMenu : MonoBehaviour
{
    [BuildSceneSelector]
    [SerializeField] private string startMenuName;

    [SerializeField] private Button backButton;

    private void Awake()
    {
        backButton.onClick.AddListener(()=>Manager.SceneTransitionManager.Instance.LoadScene(startMenuName));
    }
}
