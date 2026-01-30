using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Talkable))]
[RequireComponent(typeof(Player))]
public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject talkPrefab;

    [SerializeField] private GameObject currentTalkCanvas;

    private GameObject playerSign;
    private GameObject otherSign;
    private TextMeshProUGUI textArea;

    private Talkable _currentTalk = null;

    private Coroutine talkCoroutine;
    private bool isTalking = false;
    private bool NextTalk = false;
    private int _currentLine;
    private string fullText;
    private void Awake()
    {
        currentTalkCanvas = Instantiate(talkPrefab);
        Canvas parent = GameObject.FindAnyObjectByType<Canvas>();
        currentTalkCanvas.transform.SetParent(parent.transform,false);
        currentTalkCanvas.transform.SetAsLastSibling();
        playerSign = currentTalkCanvas.transform.Find("PlayerSign").gameObject;
        otherSign = currentTalkCanvas.transform.Find("OtherSign").gameObject;
        textArea = currentTalkCanvas.transform.Find("TextArea").GetComponent<TextMeshProUGUI>();
        otherSign.SetActive(false);
        playerSign.SetActive(false);
        currentTalkCanvas.SetActive(false);
    }

    private void StartTalk()
    {
        if (!_currentTalk || _currentTalk._index > _currentTalk._data.Length && isTalking) return;
        isTalking = true;
        //初始化聊天准备
        currentTalkCanvas.SetActive(true);
        otherSign.GetComponent<Image>().sprite = _currentTalk.speakerPortrait;
        TextMeshProUGUI oTextMeshProUGUI = otherSign.transform.GetComponentInChildren<TextMeshProUGUI>();
        oTextMeshProUGUI.text = _currentTalk.speakerName;
        Debug.Log(oTextMeshProUGUI.text + "  " + _currentTalk.speakerName);
        oTextMeshProUGUI.color = _currentTalk.nameColor;

        playerSign.GetComponent<Image>().sprite = GetComponent<Talkable>().speakerPortrait;
        TextMeshProUGUI pTextMeshProUGUI = playerSign.transform.GetComponentInChildren<TextMeshProUGUI>();
        pTextMeshProUGUI.text = GetComponent<Talkable>().speakerName;
        pTextMeshProUGUI.color = GetComponent<Talkable>().nameColor;
        Debug.Log(pTextMeshProUGUI.text + "  " + GetComponent<Talkable>().speakerName);
        GetComponent<Player>().StopMove();

        _currentLine = 0;
        fullText = _currentTalk._data[_currentTalk._index].dialogueLines[_currentLine].content;

        talkCoroutine = StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        if (_currentTalk._data[_currentTalk._index].dialogueLines[_currentLine].isPlayer)
        {
            playerSign.SetActive(true);
            otherSign.SetActive(false);
        }
        else
        {
            playerSign.SetActive(false);
            otherSign.SetActive(true);
        }
        textArea.text = "";
        int NewLine = 15;
        int currentLine = 0;
        foreach (char c in fullText)
        {
            textArea.text += c;
            yield return new WaitForSeconds(_currentTalk._data[_currentTalk._index].defaultTypingSpeed);
            currentLine++;
            if (currentLine == NewLine)
            {
                currentLine = 0;
                textArea.text += "\n";
            }
        }

        talkCoroutine = null;
        // 协程执行完毕后，将引用置为null
        Judge();
    }

    // 跳过当前打字效果
    public void SkipTyping()
    {
        if (talkCoroutine != null)
        {
            StopCoroutine(talkCoroutine);
            textArea.text = fullText; // 直接显示完整文本
            talkCoroutine = null;
        }
        Judge();
    }

    public void Judge()
    {

        _currentLine++;
        if(_currentLine < _currentTalk._data[_currentTalk._index].dialogueLines.Length)//本次对话还有内容
        {
            fullText = _currentTalk._data[_currentTalk._index].dialogueLines[_currentLine].content;
            if (_currentTalk._data[_currentTalk._index].autoAdvance)
            {
                talkCoroutine = StartCoroutine(TypeText());
            }
            else
            {
                NextTalk = true;
            }

        }
        else//此次对话结束
        {
            _currentTalk._index++;
            GetComponent<Player>().StartMove();
            currentTalkCanvas.SetActive(false);
            isTalking = false; 
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        _currentTalk = collision.transform.GetComponent<Talkable>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartTalk();
        }
        if(Input.GetKeyDown(KeyCode.Mouse0) && NextTalk)
        {
            NextTalk = false;
            talkCoroutine = StartCoroutine(TypeText());
        }
    }


}
