using UnityEngine;

public class UIClampToCanvas : MonoBehaviour
{
    private RectTransform targetUI;
    public Canvas canvas;
    public Vector2 offset = Vector2.zero;

    void Awake()
    {
        targetUI = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 设置 UI 在屏幕上的位置，并自动调整 pivot 避免超出 Canvas。
    /// </summary>
    /// <param name="screenPos">目标屏幕坐标（例如鼠标位置）</param>
    public void SetPosition(Vector2 screenPos)
    {
        if (canvas == null || targetUI == null) return;

        // 将屏幕坐标转换为本地坐标
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        targetUI.anchoredPosition = localPoint;

        ClampToCanvas();  // 自动调整 pivot 防止出界
    }

    private void ClampToCanvas()
    {
        Vector3[] corners = new Vector3[4];
        targetUI.GetWorldCorners(corners);

        for (int i = 0; i < 4; i++)
        {
            corners[i] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[i]);
        }

        bool outLeft = corners[0].x < 0;
        bool outRight = corners[2].x > Screen.width;
        bool outTop = corners[1].y > Screen.height;
        bool outBottom = corners[3].y < 0;

        Vector2 pivot = targetUI.pivot;
        Vector2 newPivot = pivot;

        if (outRight) newPivot.x = 1f;
        else if (outLeft) newPivot.x = 0f;

        if (outTop) newPivot.y = 1f;
        else if (outBottom) newPivot.y = 0f;

        if (newPivot != pivot)
        {
            Vector3 oldPos = targetUI.position;
            targetUI.pivot = newPivot;
            targetUI.position = oldPos;
        }

        targetUI.anchoredPosition += offset;
    }
}
