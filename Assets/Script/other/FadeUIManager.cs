using System.Collections.Generic;
using UnityEngine;

public class FadeUIManager : MonoBehaviour
{
    public static FadeUIManager Instance { get; private set; }

    [System.Serializable]
    public class PrefabEntry
    {
        public string key; // 类型名称，例如 "success", "error"
        public FadeAndRecycleUI prefab;
    }

    [Header("多个类型的 Prefab")]
    public List<PrefabEntry> prefabList;
    public Canvas canvas;

    private Dictionary<string, Queue<FadeAndRecycleUI>> poolDict = new();
    private Dictionary<string, FadeAndRecycleUI> prefabDict = new();

    void Awake()
    {
        Instance = this;
        foreach (var entry in prefabList)
        {
            prefabDict[entry.key] = entry.prefab;
            poolDict[entry.key] = new Queue<FadeAndRecycleUI>();
        }

        Ct.fadeUIManager = this;
    }

    public GameObject ShowTip(string type, Transform parent)
    {
        if (!prefabDict.ContainsKey(type))
        {
            Debug.LogWarning($"没有类型为 {type} 的 prefab！");
            return null;
        }

        var ui = Get(type);
        ui.gameObject.SetActive(true);
        ui.Init(this);

        return ui.gameObject;
    }

    private FadeAndRecycleUI Get(string type, Transform parent = null)
    {
        if (poolDict[type].Count > 0)
            return poolDict[type].Dequeue();
        Transform t = parent == null ? canvas.transform : parent;

        return Instantiate(prefabDict[type], t);
    }

    public void Recycle(FadeAndRecycleUI ui)
    {
        foreach (var kvp in prefabDict)
        {
            if (ui.name.StartsWith(kvp.Value.name))
            {
                ui.gameObject.SetActive(false);
                poolDict[kvp.Key].Enqueue(ui);
                return;
            }
        }

        Destroy(ui.gameObject); // fallback
    }
}

