using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainCanvs : MonoBehaviour
{
    public static Vector2 size;
    public GameObject escPage;
    public void Awake()
    {
        size = GetComponent<RectTransform>().sizeDelta;
    }
    public void Start()
    {
        Page.Add("esc", () => { escPage.SetActive(true); }, () => { });

        Ct.mcanvas = this;
    }
    private void OnRectTransformDimensionsChange()
    {
        Ct.GetScale(size);
        for (int i = 0; i < Ct.scalableui.Count; i++)
        {
            Ct.ToScale(Ct.scalableui[i]);
        }
        for (int i = 0; i < Ct.realPositions.Count; i++)
        {
            Ct.ToScale(Ct.realPositions[i], Ct.scalableui[i]);
        }
    }

    public GraphicRaycaster raycaster;  // 挂你当前 Canvas 上的 GraphicRaycaster
    public EventSystem eventSystem;     // 场景中的 EventSystem

    /// <summary>
    /// 检测指定屏幕坐标下的 UI 对象列表
    /// </summary>
    public List<RaycastResult> GetUIObjectsAt(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        return results;
    }

    /// <summary>
    /// 获取第一个 UI 元素（常用）
    /// </summary>
    public GameObject GetTopUIObjectAt(Vector2 screenPosition)
    {
        var results = GetUIObjectsAt(screenPosition);
        if (results.Count > 0)
        {
            return results[0].gameObject; // 屏幕坐标上最上层的 UI
        }
        return null;
    }
}
