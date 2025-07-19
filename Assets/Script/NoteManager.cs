using System.Collections.Generic;
using UnityEngine;

public static class NoteManager
{
    public static List<Noter> noters;
    public static Transform parent;
    public static float dist = 30;
    public static void UpdateNoter()
    {
        for (int i = 0; i < noters.Count; i++)
        {
            Noter nt = noters[i];
            nt.transform.SetParent(parent);
            nt.rect.anchoredPosition = new(0, dist * i);
        }
    }
    public static void Init(Transform parent)
    {
        NoteManager.parent = parent;
        noters = new();
    }
    public static void Load(object note)
    {
        GameObject o = Ct.fadeUIManager.ShowTip("notific", parent);
        o.GetComponent<Noter>().note = note.ToString();
    }
}