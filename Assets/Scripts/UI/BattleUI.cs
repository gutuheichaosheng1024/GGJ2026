using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [Header("playerStatus")]
    public Slider healthSlider;
    public Slider sanSlider;

    [Header("End")]
    public GameObject gameOverRoot;
    public bool pauseOnShow = true;
    public Button restart;
    [BuildSceneSelector]
    public string backSceneName;
    public Button back;

    [Header("Kill Count")]
    public int kills;
    public TMP_Text killText;

    [SerializeField] private Talkable talkable_main;
    [SerializeField] private Talkable talkable_other;
    [SerializeField] private List<int> Killingtrigger;
    [SerializeField] private GameObject bossRef;
    private int killingIndex;
    [SerializeField] private Sprite[] playerHeader;

    void Start()
    {
        Manager.SoundManager.PlayBackGroundSound("Boss");
        UpdateUI();
        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(false);
        }
        restart.onClick.AddListener(Restart);
        back.onClick.AddListener(Back);
        PlayerStatus.Instance.onDeath.AddListener(GameOver);

        healthSlider.maxValue = PlayerStatus.Instance.maxHealth;
        healthSlider.value = healthSlider.maxValue;
        PlayerStatus.Instance.onHealthChange += OnHealthChange;

        sanSlider.maxValue = PlayerStatus.Instance.maxSan;
        sanSlider.value = sanSlider.maxValue;
        PlayerStatus.Instance.onSanChange += OnSanChange;
        DialogueUI.Instance.StartTalk(talkable_main, playerHeader[0]);

    }
    public void GameOver()
    {
        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(true);
            Manager.SoundManager.PlayBackGroundSound("Fail");
        }

        if (pauseOnShow)
        {
            Time.timeScale = 0f;
        }
    }

    public void AddKill()
    {
        kills++;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (killText != null)
        {
            killText.text = "击杀: " + kills;
        }
        if (killingIndex < Killingtrigger.Count && Killingtrigger[killingIndex] <= kills)
        {
            switch (killingIndex)
            {
                case 0:
                    DialogueUI.Instance.StartTalk(talkable_main, talkable_other);
                    break;
                case 1:
                    DialogueUI.Instance.StartTalk(talkable_main, playerHeader[killingIndex]);
                    break;
            }
            killingIndex++;
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Manager.SceneTransitionManager.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Back()
    {
        Debug.LogWarning("call");
        Time.timeScale = 1f;
        Manager.SceneTransitionManager.Instance.LoadScene(backSceneName);
    }

    public void OnHealthChange(float value)
    {

        healthSlider.value = value;
    }
    public void OnSanChange(float value)=>sanSlider.value = value;

    
}
