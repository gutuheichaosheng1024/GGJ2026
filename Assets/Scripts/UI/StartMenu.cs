using Manager;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [BuildSceneSelector]
    [SerializeField]private string NextScene = string.Empty;
    [SerializeField]private Button startGame;
    [SerializeField]private Button exitGame;
    [SerializeField] private TextMeshProUGUI nameField;
    void Start()
    {
        startGame.onClick.AddListener(StartGame);
        exitGame.onClick.AddListener(() => Application.Quit());
    }

    void StartGame()
    {
        PlayerStatus.Pname = nameField.text;
        SceneTransitionManager.Instance.LoadScene(NextScene);
    }

}
