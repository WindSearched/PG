using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


}
