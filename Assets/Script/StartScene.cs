using IPGModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Windows.Forms;
using UnityEngine.InputSystem;
using System.Text;
using SFB;
using UnityEngine.SceneManagement;
public class StartScene : MonoBehaviour
{
    public static Dictionary<Type, BuildObj> changeMode = new();
    public delegate GameObject BuildObj(StartObj st);
    public static List<RectTransform> rectScaleChanged = new();
    public static List<Vector2> realPosition = new();

    public static string curMod;
    public static Setting set;
    public static float scale;
    public Transform parent;
    public RectTransform prt;
    public RectTransform deafrt;
    public GameObject modPage;
    public GameObject changesPage;
    public TextMeshProUGUI changes;
    public Transform modPageParent;
    public InputAction ia = new(name: "Tap", type: InputActionType.Button, binding: "<Keyboard>/Escape");
    public GameObject actived;
    public FadeUIManager fadeUIManager;
    private void Start()
    {
        parent = GameObject.Find("Canvas/add").transform;
        fadeUIManager = GetComponent<FadeUIManager>();
        NoteManager.Init(transform.Find("notes"));
        prt = parent.GetComponent<RectTransform>();

        GetScale();

        Setting.Load(out set);
        Camera.main.backgroundColor = set.startSceneBackGround;

        Init();

        ia.Enable();
        ia.performed += c =>
        {
            actived.SetActive(!actived.activeInHierarchy);
        };
    }
    public void Init()
    {
        string path = Mod.modPath;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        StartMods();

        foreach (string p in Directory.GetDirectories(path))
        {
            string pp = p + "/start.json";
            if(!File.Exists(pp))
                continue;

            curMod = Path.GetFileName(p);
            StartData sd = Data.ReadJson<StartData>(pp);
            foreach (StartObj o in sd.objs)
            {
                Build(o);
            }
        }
    }
    private void OnDisable()
    {
        ia.Disable();
    }
    public void StartMods()
    {
        string path = Mod.modPath;

        foreach (string p in Directory.GetDirectories(path))
        {
            string pt = p + "/" + Path.GetFileName(p) + ".dll";
            if (File.Exists(pt))
            {
                Assembly assembly = Assembly.Load(File.ReadAllBytes(pt));
                Type[] types = assembly.GetTypes();//get all class in the ddl

                foreach (Type type in types)
                {
                    if (typeof(IPGM).IsAssignableFrom(type))
                    {
                        IPGM mod = (IPGM)Activator.CreateInstance(type);
                        mod.OnStart();
                        Debug.Log($"[ModLoader]Start Mod dll: {type.Name}");
                    }
                }
            }
            else
                Debug.Log("[ModLoader]Do not found the dll: " + pt);
        }
    }
    public void Build(StartObj o)
    {
        Type t = Type.GetType(o.type);
        GameObject ob;

        if (t == null)
        {
            ob = PathParse.Load(o.obj, curMod) as GameObject;
            ob = Instantiate(ob,parent);
        }
        else
        {
            if (!changeMode.ContainsKey(t))
            {
                Debug.Log("cannot build because has not the build mode");
                return;
            }
            ob = changeMode[t].Invoke(o);
        }

        RectTransform rt = ob.GetComponent<RectTransform>();
        rt.anchoredPosition = o.pos;
        rt.rotation = Quaternion.Euler(o.rot);
        realPosition.Add(rt.anchoredPosition);

        rt.localScale = new(scale,scale);
        rt.localPosition *= scale;
        rectScaleChanged.Add(rt);
    }
    public static void GetScale()
    {
        Vector2 size = GameObject.Find("Canvas").GetComponent<RectTransform>().sizeDelta;
        Vector2 deaf = new(600,400);
        Vector2 rela = size / deaf;
        if(rela.x < rela.y)
            scale = rela.x;
        else
            scale = rela.y;
    }

    void OnRectTransformDimensionsChange()
    {
        GetScale();
        Debug.Log("scale: " + scale);
        for(int i = 0; i < rectScaleChanged.Count; i++)
        {
            RectTransform r = rectScaleChanged[i];
            r.localScale = new(scale, scale);
            r.localPosition = realPosition[i] * scale;
        }
    }
    public void ModButton()
    {
        float by = -30;
        actived = modPage;
        modPage.SetActive(!modPage.activeInHierarchy);

        int i = 0;
        while(i < modPageParent.childCount)
        {
            Destroy(modPageParent.GetChild(i).gameObject);
            i++;
        }
        GameObject bar = Resources.Load("modBar") as GameObject;

        if (!Data.DIrectioryExists(Mod.modPath))
            Data.Create(Mod.modPath);

        DirectoryInfo[] dis = Data.GetDirectories(Mod.modPath);
        i = 0;
        while (i < dis.Length)
        {
            GameObject o = Instantiate(bar, modPageParent);
            RectTransform rt = o.GetComponent<RectTransform>();
            rt.localPosition = new(50, i * 100 + by);

            o.transform.Find("icon").GetComponent<Image>().sprite = Mod.LoadSprite(dis[i].FullName + "/icon.png");
            o.transform.Find("name").GetComponent<TextMeshProUGUI>().text = dis[i].Name;
            o.transform.Find("description").GetComponent<TextMeshProUGUI>().text = Data.LoadFile(dis[i].FullName + "/description.txt");

            i++;
        }
    }
    public void AddMod()
    {
        var path = StandaloneFileBrowser.OpenFolderPanel("Choose a mod folder ...","",false);
        if (path.Length > 0)
        {
            DirectoryInfo di = new(path[0]);
            string p = Mod.modPath + "/" + di.Name;
            //try
            //{
            if (Data.DIrectioryExists(p))
                Data.Delete(p);
            Data.CopyAll(path[0], p);
            NoteManager.Load($"load mod {di.Name}");
        }
    }
    public void ChangesButton()
    {
        actived = changesPage;
        changesPage.SetActive(!changesPage.activeInHierarchy);

        StringBuilder sb = new();
        sb.Append(Data.LoadFile(UnityEngine.Application.streamingAssetsPath + "/changes.txt"));
        sb.AppendLine();

        foreach (DirectoryInfo di in Data.GetDirectories(Mod.modPath))
        {
            sb.Append(Data.LoadFile(di.FullName + "/changes.txt"));
            sb.AppendLine();
        }

        changes.text = sb.ToString();

    }

    public void WorldListEnter(GameObject o)
    {
        string n = o.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
        set.preWorld = n;
        SceneManager.LoadScene("main");
    }
    public void UpdateWorldList()
    {
        if(worldListP.gameObject.activeInHierarchy == false)
            return;
        if(worldListBar == null)
            worldListBar = Resources.Load("worldListBar") as GameObject;

        for(int i = 0;i< worldListPa.childCount;i++)
        {
            Destroy(worldListPa.GetChild(i).gameObject);
        }

        int j = 0;
        foreach(var v in Data.GetDirectories(Data.worldPath))
        {
            GameObject g = Instantiate(worldListBar, worldListPa);
            g.GetComponent<RectTransform>().anchoredPosition = new(0, j++ * -80);

            g.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = v.Name;
            g.transform.GetChild(2).GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => WorldListEnter(g));
            g.transform.GetChild(1).GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { DestroyWorld(g);UpdateWorldList(); });
        }
    }
    public Transform worldListP;
    public Transform worldListPa;
    public GameObject worldListBar;
    public void DestroyWorld(GameObject o)
    {
        string path = o.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
        path = Data.worldPath + "/" + path;
        Data.Delete(path);
    }
    public void SwitchActive(GameObject o)
    {
        if(o.activeInHierarchy)
        {
            o.SetActive(false);
        }
        else
        {
            o.SetActive(true);
            actived = o;
        }
    }

    public void CreateWorld()
    {
        string name = inpName.text;
        if (name == "")
            return;
        string seed = inpSeed.text;
        if(seed == "")
            return;

        WorldData wd = new()
        {
            name = name,
            seed = seed
        };
        string p = Data.worldPath + name;
        if (Data.FileExists(p))
            return;
        else 
            Data.Create(p);

        wd.Save();
        UpdateWorldList();
        inpName.text = "";
        inpSeed.text = "";

        Debug.Log(inpName.text);
    }
    public TMP_InputField inpName;
    public TMP_InputField inpSeed;
}

[Serializable]
public class StartData
{
    public StartObj[] objs;
}
[Serializable]
public class StartObj
{
    public string obj = "";
    /// <summary>
    /// use when obj is a frame
    /// </summary>
    public string spritePath = "";
    /// <summary>
    /// use when obj is a frame, change the text of obj
    /// </summary>
    public string text = "";
    /// <summary>
    /// path, if obj has interaction, ex: button
    /// </summary>
    public string method = "";
    public Vector2 pos;
    public Vector3 rot;
    /// <summary>
    /// is the completed name
    /// 需type完整命名
    /// </summary>
    public string type = "Unity.UI.Image";
}

/// <summary>
/// AssetBundles loaded in the mod
/// </summary>
public static class AB
{
    public static Dictionary<string, AssetBundle> abo = new();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="abName">is the  mod name, not abpack name</param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static object Load(string abName, string path)
    {
        string abon = abName + "/" + path;
        if (!abo.ContainsKey(abon))
        {
            string abPath = Mod.modPath + abName + "/abp/" + path;
            abo.Add(abon, AssetBundle.LoadFromFile(abPath));
        }

        object o = abo[abon].LoadAsset<UnityEngine.Object>(path);
        return o;
    }
}
public static class PathParse
{
    public static PathGet Parse(string path, out string rest)
    {
        rest = path;
        if (path[0] == ':')
        {
            string[] p = path.Split(':');
            rest = p[2];
            return (PathGet)Enum.Parse(typeof(PathGet), p[1]);
        }
        else
            return PathGet.nor;
    }
    /// <summary>
    /// note: just support sprite nor load
    /// 注意：只支持sprite的普通加载
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static object Load(string path, string mod)
    {
        PathGet pp = Parse(path, out string r);
        switch (pp)
        {
            case PathGet.nor:
                return Mod.LoadSprite(Mod.modPath + "/" + mod + "/" + path);
            case PathGet.AB:
                return AB.Load(mod, r);
            default:
                return null;
        }
    }

    public enum PathGet
    {
        nor,
        AB
    }
}
public static class DLLpg
{
    public static Dictionary<string, Assembly> dlls = new();

    public static dPGM Load(string method, string class_, string mod)
    {
        if (!dlls.ContainsKey(mod))
        {
            string p = Mod.modPath + mod + "/" + mod + ".dll";
            dlls.Add(mod, Assembly.Load(File.ReadAllBytes(p)));
        }

        Type t = dlls[mod].GetType(mod + "." + class_);
        if (t != null)
        {
            MethodInfo m = t.GetMethod(method);

            return (dPGM)Delegate.CreateDelegate(typeof(dPGM), m);
        }
        return null;
    }
}