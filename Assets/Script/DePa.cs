using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class DePa : MonoBehaviour
{
    public RectTransform canvas;
    public float deafHeight;
    public int lineNumber;

    private List<RectTransform> lines = new();
    private List<TextMeshProUGUI> texts = new();
    private List<Func<object>> obs = new();
    private List<string> names = new();

    private float height, width;
    private int register;
    private bool active = false;

    public void Start()
    {
        Ct.act.Main.DebugPage.performed +=
            c =>
            {
                if(c.interaction is TapInteraction)
                {
                    active = !active;
                    Active();
                }
            };

        Ct.dePa = this;

        height = canvas.sizeDelta.y;
        width = canvas.localPosition.x;
        deafHeight = height / lineNumber;

        GameObject r = (GameObject)Resources.Load("Deb");
        RectTransform o = r.GetComponent<RectTransform>();
        o.sizeDelta = new(width,deafHeight);
        TextMeshProUGUI t = r.GetComponent<TextMeshProUGUI>();
        t.fontSize = deafHeight;

        for (int i = 0; i < lineNumber; i++)
        {
            GameObject or = Instantiate(r);
            or.name = i.ToString();
            or.transform.SetParent(transform, false);
            RectTransform ob = or.GetComponent<RectTransform>();
            ob.anchoredPosition = new(0, -deafHeight * i);
            lines.Add(ob);
            texts.Add(or.GetComponent<TextMeshProUGUI>());

            obs.Add(() => "");
            names.Add("");
        }
        Active();
    }
    private void FixedUpdate()
    {
        if (active && register != 0)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                texts[i].text = names[i] + ": " + obs[i]?.Invoke().ToString();
            }
        }
    }

    public void Regist(int index, Func<object> getter, string name)
    {
        obs[index] = getter;
        names[index] = name;
        register++;
    }
    public void Delete(int index)
    {
        obs[index] = () =>"";
        names[index] = "";
        register--;
    }
    private void Active()
    {
        for (int i = 0; i < lineNumber; i++)
        {
            Transform t = transform.GetChild(i);
            t.gameObject.SetActive(active);
        }
    }
}
