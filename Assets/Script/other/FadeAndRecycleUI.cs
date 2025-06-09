using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeAndRecycleUI : MonoBehaviour
{
    public float duration = 8;
    public float fadeDelay = 0f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private FadeUIManager manager;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(FadeUIManager mgr)
    {
        manager = mgr;
        canvasGroup.alpha = 1f;
        StopAllCoroutines();
        StartCoroutine(FadeOutAndRecycle());
    }

    public void SetScreenPosition(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            manager.canvas.transform as RectTransform,
            screenPos,
            manager.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : manager.canvas.worldCamera,
            out Vector2 localPoint
        );
        rectTransform.anchoredPosition = localPoint;
    }

    IEnumerator FadeOutAndRecycle()
    {
        if (fadeDelay > 0)
            yield return new WaitForSeconds(fadeDelay);

        float t = 0f;
        float startAlpha = canvasGroup.alpha;

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / duration);
            yield return null;
        }

        manager.Recycle(this);
    }
}

