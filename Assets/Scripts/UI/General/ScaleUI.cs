using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ScaleUI : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    [SerializeField]private float scale = 1.5f;
    [SerializeField] private float scaleTime = 0.8f;
    private Vector3 originScale;
    private RectTransform rectTransform;


    private void Awake()
    {
        originScale = transform.localScale;
        rectTransform = GetComponent<RectTransform>();

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.DOScale(originScale * scale, scaleTime);
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.DOScale(originScale, scaleTime);
    }

    private void OnDisable()
    {
        rectTransform.DOScale(originScale, .01f);
    }

}
