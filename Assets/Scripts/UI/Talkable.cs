using UnityEngine;

public class Talkable : MonoBehaviour
{
    public DialogueData[] _data;
    public int _index = 0;

    [Header("说话者信息")]
    public string speakerName;           // 说话者名字
    public Sprite speakerPortrait;       // 说话者头像
    public Color nameColor = Color.white; // 名字颜色
    public AudioClip voiceClip;          // 说话音效

}
