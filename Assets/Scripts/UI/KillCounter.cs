using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class KillCounter : MonoBehaviour
{
    public int kills;
    public TMP_Text killText;

    void Start()
    {
        UpdateUI();
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
    }
}
