using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject gameOverRoot;
    public bool pauseOnShow = true;

    [Header("Restart")]
    public string battleSceneName = "scene1";

    void Awake()
    {
        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(false);
        }
    }

    public void Show()
    {
        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(true);
        }

        if (pauseOnShow)
        {
            Time.timeScale = 0f;
        }
    }

    public void Restart()
    {
        if (pauseOnShow)
        {
            Time.timeScale = 1f;
        }

        if (!string.IsNullOrEmpty(battleSceneName))
        {
            SceneManager.LoadScene(battleSceneName);
        }
    }
}
