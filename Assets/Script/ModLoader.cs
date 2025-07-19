using IPGModAPI;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
/// <summary>
/// Mod insert extern recources in the intern 
/// </summary>
public static class Mod
{
    public static string curLoadModName;//the mod name
    public static bool modLoaded = false;   

    [Serializable]
    public class StartScene
    {
        public string worldPath, setPath, ModPath, developPath;
        public string titlePath;
        public string[] element;
    }
    [Serializable]
    public class MItem
    {
        public InsertMode insertMode;
        public ItemData data;
        public TextManager.Text[] nameOfLangue;
        public TextManager.Text[] description;

        public string spritePath;
        /// <summary>
        /// used when the data is null
        /// </summary>
        public string name;
        /// <summary>
        /// pixels per unit
        /// </summary>
        public int ppu = 32;

        public PartialChanger[] changers;
        public string mothernal;

        public void AddTo(string modName)
        {
            string name = data == null ? this.name : data.name;

            switch (insertMode)
            {
                case InsertMode.change:
                    Item.Change(name, data);
                    break;
                case InsertMode.add:
                    Item.Add(data);
                    break;
                case InsertMode.patial:
                    Item.PartialChange(name, changers);
                    break;
                case InsertMode.mothernal:
                    Item.MothernalAdd(mothernal, changers);
                    break;
            }
            Sprite sprite = LoadSprite(modName, this);

            try
            {
                Item.SetSprite(name, sprite);
            }
            catch
            {
                Debug.LogError("[ModLoader]do not exist the item : " + name);
            }
        }
        public void AddTexts()
        {
            if (nameOfLangue != null)
            {
                string key = "itname_" + data.name;
                foreach (var v in nameOfLangue)
                {
                    v.key = key;
                    v.AddTo();
                }
            }
            if (description != null)
            {
                string key = "itdescrp_" + data.name;
                foreach (var v in description)
                {
                    v.key = key;
                    v.AddTo();
                }
            }
        }

    }
    [Serializable]
    public class MObject
    {
        public InsertMode insertMode;
        public ObjData data;
        public TextManager.Text[] description;
        public TextManager.Text[] name;
        /// <summary>
        /// use when 
        /// </summary>
        public PartialChanger[] changers;

        //public string typeName;
        public string mothernal;

        /// <summary>
        /// add to Obj class
        /// </summary>
        public void AddTo(string modName)
        {
            switch (insertMode)
            {
                case InsertMode.change:
                    Obj.Change(data.name, data);
                    break;
                case InsertMode.add:
                    Obj.Add(data);
                    break;
                case InsertMode.patial:
                    Obj.PartialChange(data.name, changers);
                    break;
                case InsertMode.mothernal:
                    Obj.MothernalAdd(mothernal, changers);
                    break;

            }

            SpriteManager sm = SpriteManager.Load(data.spriteobj);
            data.collider.size = SMath.Spr.GetValidPixels(sm.Get());

            try
            {
                Obj.SetSprite(data.name, sm);
            }
            catch
            {
                Debug.LogError("[ModLoader]do not extst the obj : " + data.name);
            }
        }
        public void AddText()
        {
            if (name != null)
            {
                string key = "obname_" + data.name;
                foreach (var v in name)
                {
                    v.key = key;
                    v.AddTo();
                }
            }
            if (description != null)
            {
                string key = "obdescrp_" + data.name;
                foreach (var v in description)
                {
                    v.key = key;
                    v.AddTo();
                }
            }
        }
    }
    [Serializable]
    public class MActor
    {
        public object spritePath;
        public ActorData data;

        public void AddTo()
        {
            Actor.aTy.Add(data.name);
            Actor.data.Add(data.name, data);

            var sm = SpriteManager.Load(spritePath);
            Actor.sprites.Add(sm);
        }
    }
    /// <summary>
    /// use to chenge partial or some data
    /// </summary>
    public class PartialChanger
    {
        public string changedName;
        public object changer;
    }
    /// <summary>
    /// Mode of the item/object insert in the stream
    /// </summary>
    public enum InsertMode
    {
        add,
        change,
        patial,
        mothernal
    }

    public static Sprite Load(string name)
    {
        string path = modPath + name;
        return LoadSprite(path);
    }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    public static string modPath = Application.streamingAssetsPath + "/mod/";
#else 
    public static string modPath = Application.persistentDataPath + "/mod/";
#endif
    /// <summary>
    /// Load the immage to sprite on the path
    /// </summary>
    public static Sprite LoadSprite(string path, Vector2 pivot, int ppu)
    {
        if (!File.Exists(path))
        {
            Debug.LogAssertion($"文件不存在: {path}");
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);

        Texture2D tex = new(2, 2);
        if (!tex.LoadImage(bytes))
        {
            Debug.LogError("加载图片失败");
            return null;
        }

        tex.filterMode = FilterMode.Point;

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            pivot,
            ppu // pixelsPerUnit
        );

        Debug.Log($"[ModLoader]Sprite 加载成功: {path}, 尺寸: {tex.width}x{tex.height}");

        return sprite;
    }
    public static Sprite LoadSprite(string path, Vector2 pivot)
    {
        return LoadSprite(path, pivot, 32);
    }
    public static Sprite LoadSprite(string path, int ppu)
    {
        return LoadSprite(path, new(0.5f, 0.5f), ppu);
    }
    public static Sprite LoadSprite(string path)
    {
        return LoadSprite(path, new Vector2(0.5f, 0.5f));
    }
    public static Sprite LoadSprite(string modName, MItem data)
    {
        string s = data.spritePath == null ? data.data.name + ".png" : data.spritePath;
        string p = modPath + modName + "/sprites/" + s;
        try
        {
            return LoadSprite(p, data.ppu);
        }
        catch
        {
            Debug.LogError("[ModLoader]do not exist the sprite: " + p);
            return null;
        }
    }
    /// <summary>
    /// use for load added modding
    /// </summary>
    public static void LoadMods()
    {
        string path = modPath;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        foreach (string p in Directory.GetDirectories(path))
        {
            curLoadModName = Path.GetFileName(p);

            string pp = p + "/objects/";
            if (Data.DIrectioryExists(pp))
                foreach (string ppp in Directory.GetFiles(pp))
                {
                    if (ppp.Contains(".m"))
                        continue;
                    try
                    {
                        var o = Data.ReadJson<MObject>(ppp);
                        if (o == null)
                        {
                            Debug.Log("null");
                        }
                        o.AddTo(curLoadModName);
                    }
                    catch
                    {

                    }
                }

            pp = p + "/items/";
            if (Data.DIrectioryExists(pp))
                foreach (string ppp in Directory.GetFiles(pp))
                {
                    if (ppp.Contains(".m"))
                        continue;
                    MItem d = Data.ReadJson<MItem>(ppp);
                    d.AddTo(curLoadModName);
                    d.AddTexts();
                }

            pp = p + "/actors/";
            if (Data.DIrectioryExists(pp))
                foreach (string ppp in Directory.GetFiles(pp))
                {
                    if (ppp.Contains(".m"))
                        continue;
                    MActor a = Data.ReadJson<MActor>(ppp);
                    a.AddTo();
                }

            TextManager.AddTextFromFile(p + "/texts.txt");

            LoadDLL(p + "/" + curLoadModName + ".dll");

            pp = p + "/recipes.json";
            if (Data.FileExists(pp))
                Crafting.Load(Data.ReadJson<Recipe[]>(pp));

        }
        modLoaded = true;
    }
    public static void LoadModsInWorld()
    {
        string path = modPath;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        foreach (string p in Directory.GetDirectories(path))
        {

            WorldGenerator.LoadingFromPath(p + "/biomes.json");
        }
    }
    private static void LoadDLL(string path)
    {
        if (File.Exists(path))
        {
            Assembly assembly = Assembly.LoadFrom(path);
            Type[] types = assembly.GetTypes();//get all class in the ddl

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool isLoaded = loadedAssemblies.Any(a => a.FullName == assembly.FullName);
            Debug.Log("DLL loaded? " + isLoaded);

            foreach (Type type in types)
            {
                if (typeof(IPGM).IsAssignableFrom(type))
                {
                    IPGM mod = (IPGM)Activator.CreateInstance(type);
                    try
                    {
                        mod.OnLoad();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                        NoteManager.Load(ex);
                    }
                    Debug.Log($"[ModLoader]Loaded Mod dll: {type.Name}");
                }
                else
                    Debug.Log("[ModLoader]not assignable");
            }
        }
        else
            Debug.Log("[ModLoader]Do not found the dll: " + path);
    }
}