using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class MouseFollow : MonoBehaviour
{
    [SerializeField] float followRange = 10f;

    [SerializeField] private Canvas canvas;           // Canvas组件


    private Vector2 originPoistion;
    private Vector2 NormalOriginPosition;
    private RectTransform rectTransform;
    private Vector2Int HalfscreenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originPoistion = canvas.GetComponent<RectTransform>().InverseTransformPoint(rectTransform.position);
        Vector2 canvasSize = GetCanvasFinalSize(canvas);
        NormalOriginPosition = new Vector2(originPoistion.x / (canvasSize.x / 2), originPoistion.y / (canvasSize.y / 2));


        HalfscreenSize = new Vector2Int(Screen.width/2, Screen.height/2);
    }


    private void Update()
    {
        // 获取鼠标位置
        Vector2 mousePosition = Input.mousePosition;
        mousePosition = new Vector2((mousePosition.x - HalfscreenSize.x) /(HalfscreenSize.x), (mousePosition.y - HalfscreenSize.y) /(HalfscreenSize.y));


        Vector2 dir = (mousePosition - NormalOriginPosition).normalized;
        rectTransform.position = canvas.GetComponent<RectTransform>().TransformPoint(originPoistion + dir * followRange);
    }

    public Vector2 GetCanvasFinalSize(Canvas canvas)
    {
        if (canvas == null) return Vector2.zero;

        // 获取Canvas的RectTransform
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null) return Vector2.zero;

        // 获取Canvas的原始尺寸
        Vector2 originalSize = canvasRect.rect.size;

        // 获取Canvas Scaler
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            // 如果没有Canvas Scaler或不是Scale With Screen Size模式，返回原始尺寸
            return originalSize;
        }

        // Scale With Screen Size模式下的计算
        Vector2 referenceResolution = scaler.referenceResolution;
        Vector2 screenResolution = new Vector2(Screen.width, Screen.height);

        float scaleFactor = 1f;

        switch (scaler.screenMatchMode)
        {
            case CanvasScaler.ScreenMatchMode.MatchWidthOrHeight:
                // 使用对数插值
                float logWidth = Mathf.Log(screenResolution.x / referenceResolution.x, 2f);
                float logHeight = Mathf.Log(screenResolution.y / referenceResolution.y, 2f);
                float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight);
                scaleFactor = Mathf.Pow(2f, logWeightedAverage);
                break;

            case CanvasScaler.ScreenMatchMode.Expand:
                // 取宽度和高度的最小缩放
                scaleFactor = Mathf.Min(
                    screenResolution.x / referenceResolution.x,
                    screenResolution.y / referenceResolution.y
                );
                break;

            case CanvasScaler.ScreenMatchMode.Shrink:
                // 取宽度和高度的最大缩放
                scaleFactor = Mathf.Max(
                    screenResolution.x / referenceResolution.x,
                    screenResolution.y / referenceResolution.y
                );
                break;
        }

        // 计算最终大小 = 原始尺寸 × 缩放因子
        Vector2 finalSize = originalSize * scaleFactor;

        return finalSize;
    }







}
