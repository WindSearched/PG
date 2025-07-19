using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HighlightLabelUI : MonoBehaviour
{
    [Header("组件引用")]
    public TextMeshProUGUI text;
    public Image background;

    [Header("配置")]
    public float showTime = 1.5f;
    public float fadeDuration = 0.5f;
    public Color backgroundColor = new Color(1f, 1f, 0f, 0.6f); // 黄色半透明
    public Color textColor = Color.black;
    public float fontSize = 36f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    public void Show(string message)
    {
        StopAllCoroutines();
        text.text = message;
        text.fontSize = fontSize;
        text.color = textColor;
        background.color = backgroundColor;
        LayoutRebuild();
        StartCoroutine(ShowAndFade());
    }

    void LayoutRebuild()
    {
        // 强制刷新布局以适应新文字
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    IEnumerator ShowAndFade()
    {
        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(showTime);

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        gameObject.SetActive(false); // 可回收
    }
}
