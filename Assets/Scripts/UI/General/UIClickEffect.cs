using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIClickEffect : MonoBehaviour, IPointerClickHandler
{
    private static GameObject soundObject;

    private void Awake()
    {
        if (soundObject ==  null)
        {
            soundObject = new GameObject("UIsound");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Manager.SoundManager.PlayEffect("buttonClicked", soundObject);
    }
}
