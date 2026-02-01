using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Talkable))]
[RequireComponent(typeof(Player))]
public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject talkPrefab;

    [SerializeField] private static GameObject currentTalkCanvas;

    private static GameObject playerSign;
    private static GameObject otherSign;
    private static TextMeshProUGUI NameArea;
    private static TextMeshProUGUI textArea;

    private static Talkable _currentTalk = null;
    private static Talkable _otherTalk = null;

    private static Coroutine talkCoroutine;
    private enum DialogState
    {
        Free = 0,
        Talking = 1,
        EndTalking = 2,
        WaitForEnd = 3,
    }
    private static DialogState _state;
    private static int _currentLine;
    private static string fullText;


    public static DialogueUI _instance;
    public static DialogueUI Instance => _instance;

    public Action End;

    private void Awake()
    {
        _instance = this;
        currentTalkCanvas = Instantiate(talkPrefab);
        Canvas parent = GameObject.FindAnyObjectByType<Canvas>();
        currentTalkCanvas.transform.SetParent(parent.transform,false);
        currentTalkCanvas.transform.SetAsLastSibling();
        playerSign = currentTalkCanvas.transform.Find("PlayerSign").gameObject;
        otherSign = currentTalkCanvas.transform.Find("OtherSign").gameObject;
        textArea = currentTalkCanvas.transform.Find("TextArea").GetComponent<TextMeshProUGUI>();
        textArea.color = Color.black;
        NameArea = textArea.transform.Find("Name").GetComponent<TextMeshProUGUI>();

        otherSign.SetActive(false);
        playerSign.SetActive(false);
        currentTalkCanvas.SetActive(false);
    }

    public void StartTalk(Talkable target,Talkable _other = null)
    {
        Time.timeScale = 0.0f;
        _currentTalk = target;
        _otherTalk = _other;
        if (_currentTalk._index >= _currentTalk._data.Length && _state != DialogState.Free) return;
        _state = DialogState.Talking;
        //初始化聊天准备
        currentTalkCanvas.SetActive(true);
        otherSign.GetComponent<Image>().sprite = _currentTalk.speakerPortrait;
        if (_otherTalk)
        {
            playerSign.GetComponent<Image>().sprite = _otherTalk.speakerPortrait;
        }
        else
        {
            playerSign.GetComponent<Image>().sprite = GetComponent<Talkable>().speakerPortrait;
        }
        GetComponent<Player>().StopMove();

        _currentLine = 0;
        fullText = _currentTalk._data[_currentTalk._index].dialogueLines[_currentLine].content;

        talkCoroutine = StartCoroutine(TypeText());
    }


    public void StartTalk(Talkable target, Sprite playerSprite)
    {
        Time.timeScale = 0.0f;
        _currentTalk = target;
        _otherTalk = null;
        if (_currentTalk._index >= _currentTalk._data.Length && _state != DialogState.Free) return;
        _state = DialogState.Talking;
        //初始化聊天准备
        currentTalkCanvas.SetActive(true);
        otherSign.GetComponent<Image>().sprite = _currentTalk.speakerPortrait;

        playerSign.GetComponent<Image>().sprite = playerSprite;
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
            if (_otherTalk)
            {
                NameArea.color = _otherTalk.nameColor;
                NameArea.text = _otherTalk.speakerName;
            }
            else
            {
                NameArea.color = GetComponent<Talkable>().nameColor;
                NameArea.text = PlayerStatus.Pname;
            }

        }
        else
        {
            playerSign.SetActive(false);
            otherSign.SetActive(true);
            NameArea.text = _currentTalk.speakerName;
            NameArea.color = _currentTalk.nameColor;
        }
        textArea.text = "";
        int NewLine = 30;
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
        _state = DialogState.EndTalking;
        Judge();
    }

    // 跳过当前打字效果
    public void SkipTyping()
    {
        if (talkCoroutine != null)
        {
            StopCoroutine(talkCoroutine);
            int NewLine = 30;
            int currentLine = 0;
            foreach (char c in fullText)
            {
                textArea.text += c;
                currentLine++;
                if (currentLine == NewLine)
                {
                    currentLine = 0;
                    textArea.text += "\n";
                }
            }
            talkCoroutine = null;
            _state = DialogState.EndTalking;
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
                _state = DialogState.Talking;
            }
        }
        else//此次对话结束
        {
            if (_currentTalk._data[_currentTalk._index].autoAdvance)
            {
                EndTalk();
            }
            else
            {
                _state = DialogState.WaitForEnd;
            }
        }
    }

    private void EndTalk()
    {
        Time.timeScale = 1.0f;
        End?.Invoke();
        _currentTalk._index++;
        _currentTalk = _otherTalk = null;
        GetComponent<Player>().StartMove();
        currentTalkCanvas.SetActive(false);
        _state = DialogState.Free;
    }

    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    _currentTalk = collision.transform.GetComponent<Talkable>();
    //}

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    StartTalk();
        //}
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (_state == DialogState.EndTalking)
            {
                _state = DialogState.Talking;
                talkCoroutine = StartCoroutine(TypeText());
            }
            else if(_state == DialogState.Talking)
            {
                SkipTyping();
            }
            else if(_state == DialogState.WaitForEnd)
            {
                EndTalk();
            }

        }
    }


}
