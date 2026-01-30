using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Serializable]
    public class DialogueLine
    {
        [Header("对话内容")]
        [TextArea(3, 10)]
        public string content;               // 对话文本
        public bool isPlayer = false;
    }

    [Header("对话设置")]
    public DialogueLine[] dialogueLines;     // 对话行数组

    [Header("全局设置")]
    public float defaultTypingSpeed = 1.0f; // 默认打字速度
    public AudioClip defaultTypeSound;      // 默认打字音效
    public bool autoAdvance = false;         // 是否自动进入下一句
    public float autoAdvanceDelay = 1.0f;     // 自动进入下一句的延迟
}