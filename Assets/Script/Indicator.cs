using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Indicator : MonoBehaviour
{
    public Image im, itim;
    public RectTransform rt;
    public List<Idc> dict;
    public string preIndicate;

    private string indicate;
    public string Indicate
    {
        get => indicate;
        set
        {
            if (!indicates.ContainsKey(value) || indicate == value)
                return;
            preIndicate = indicate;
            indicate = value;
            im.sprite = indicates[value];
        }
    }
    public Vector2 position
    {
        get
        {
            return rt.position; 
        }
        set
        {
            rt.position = value;
        }
    }
    private void Start()
    {
        Ct.ct.indicator = this;
        foreach (var item in dict)
        {
            AddIndicate(item.key, item.spr);
        }
    }

    private void FixedUpdate()
    {
#if UNITY_STANDALONE_WIN
        rt.position = Ct.ct.mP;
#endif
    }

    public static Dictionary<string, Sprite> indicates = new();
    public static void AddIndicate(string key, Sprite spr)
    {
        if(indicates.ContainsKey(key))
            indicates[key] = spr;
        else
            indicates.Add(key, spr);
    }
}
[Serializable]
public class Idc
{
    public string key;
    public Sprite spr;
}
