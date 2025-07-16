using IPGModAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

public class Obj : MonoBehaviour
{
    public static Quaternion facing = new();//toward where object facing
    /// <summary>
    /// object types
    /// </summary>
    public static List<string> oTy = new();
    /// <summary>
    /// every object's data
    /// </summary>
    public static List<ObjData> data = new();
    /// <summary>
    /// all sprites of every object
    /// </summary>
    //public static List<ObjSprite> sprites = new();
    public static List<SpriteManager> sprites = new();

    public static Dictionary<string, Interaction> interactions = new();


    public SpriteRenderer spriteR;
    public Transform spt;//sprite transform
    public Sprite Sprite
    {
        get
        {
            if (spriteR == null)
                spriteR = GetComponent<SpriteRenderer>();
            return spriteR.sprite;
        }
        set
        {
            if (spriteR == null)
                spriteR = GetComponent<SpriteRenderer>();
            spriteR.sprite = value;
        }
    }
    public int index = -1;
    public int stateIndex;
    public ObjLoadData ld = new();
    public Vector2Int cp;
    public ObjData dt;
    public Chunk.ObjState state;
    public dPGM onDestroy;
    public virtual void Start()
    {
        c++;

        if (!CompareTag("Player"))
        {
            if (CompareTag("Object"))
            {
                transform.parent = Ct.ct.objects.transform;
            }
            else
            {
                transform.parent = null;
            }

            if (ld.name == "n")
            {
                Destroy(gameObject);
                return;
            }

            if (index == -1)
                index = oTy.IndexOf(ld.name);

            Sprite = sprites[index].Get();
            dt = data[index];

            transform.SetPositionAndRotation(ld.relapos.ToVec(cp), Quaternion.Euler(ld.rotation.ToVec()));
            if (!dt.collision)
            {
                gameObject.layer = LayerMask.NameToLayer("No-collision Obj");
            }

            BoxCollider co = GetComponent<BoxCollider>();

            ObjData.Collider c = dt.collider;
            if (ld.calcutated)//setting the collider
            {
                int p = SMath.Spr.pxPerUnit;
                if (!c.calcutated)
                {
                    Vector2 point = new(c.size.x, c.size.y);
                    Vector2 size = new Vector2(c.size.width, c.size.height) * 0.5f;
                    c.center = (point + size);

                    c.calcutated = true;
                }
                ld.size = new(c.size.width / p, c.size.height / p, c.depth);
                Vector2 cen = Sprite.pivot;
                ld.center.FromVec((c.center - new Vector3(cen.x, cen.y)) / p);

                ld.calcutated = false;
            }
            co.size = ld.size.ToVec();
            co.center = ld.center.ToVec();
        }
        else
        {
            ld.name = "player";
            index = oTy.IndexOf(ld.name);
            Sprite = sprites[index].Get();
        }
        gameObject.name = ld.name;
        if (sprites[index].towardable)
            Debug.Log(ld.angleinit);

        Ct.evn.WhenVisionRotating += ChangeVision;
        Ct.evn.WhenVisionElevate += ChangeVision;
        spt.rotation = facing;
    }
    public static int c = 0;

    public static void LoadDefualtInteractions()
    {
        interactions.Add("container", Container);
        interactions.Add("burn", Burn);

        Page.Add("container", () =>
        {
            Ct.DestroyAll(Ct.ct.chestView);
            Ct.ct.chestView.gameObject.SetActive(true);
            Ct.ct.invp.gameObject.SetActive(true);
        }, () =>
        {
            Ct.ct.chestView.gameObject.SetActive(false);
            Ct.ct.invp.gameObject.SetActive(false);
            Ct.selectContainerInv.WhenInvChange -= InventoryPage.OnUpdate;
            Ct.selectContainerInv = null;
        });
    }
    public static void Container(ObjData data, Obj obj)
    {
        Page.ChangePage("container");
        if (Ct.ct.chestView.childCount != 0)
            Debug.Log("nonononon");
        Chunk.ObjState state = obj.state;
        if (!state.states.ContainsKey("container"))
        {
            state.Regist("container", new Inventory(data.container.cell));
        }
        Inventory inv = state.GetState("container") as Inventory;
        Ct.selectContainerInv = inv;
        inv.WhenInvChange += InventoryPage.OnUpdate;

        GameObject grid = Resources.Load("chestgrid") as GameObject;
        int x, y, len = 49;

        for (int i = 0; i < inv.invt.Count; i++)
        {
            y = i / 4;
            x = i % 4;
            GameObject g = Instantiate(grid, Ct.ct.chestView);
            g.name = i.ToString();
            g.GetComponent<RectTransform>().localPosition = new(x * len - len * 2, -y * len + len * 2);
            SButton sb = g.GetComponent<SButton>();
            sb.onClick.AddListener(() => { inv.Switch(int.Parse(g.name), Ct.mouseSelected.select, out Ct.mouseSelected.select); Ct.mouseSelected.WhenSwitch(); });
        }
        inv.Invchange();
    }
    public static void Burn(ObjData data, Obj obj)
    {
        if (Ct.mouseSelected.select.amt <= 0)
            return;

        ItemData id = Item.GetData(Ct.mouseSelected.select.item);
        string burner = "burner", burnable = "burnable";

        Chunk.ObjState state = obj.state;


        if (!state.states.ContainsKey(burner))
            state.Regist(burner, new List<string>(16));
        List<string> burners = state.GetState(burner) as List<string>;
        if (!state.states.ContainsKey(burnable))
            state.Regist(burnable, new List<string>(16));
        List<string> burnables = state.GetState(burnable) as List<string>;

        if (id.burnPw != 0 && burners.Count < 16)
        {
            burners.Add(Ct.mouseSelected.select.item);
            Ct.mouseSelected.select.Add(-1, out _);
        }
        else if (id.trasformables.ContainsKey("burn") && burnables.Count < 16)
        {
            burnables.Add(Ct.mouseSelected.select.item);
            Ct.mouseSelected.select.Add(-1, out _);
        }
        state.states[burner] = burners;
        state.states[burnable] = burnables;


        Ct.ct.CT(Burning());
    }
    public static System.Collections.IEnumerator Burning()
    {
        Obj obj = Ct.ct.casted.GetComponent<Obj>();
        Chunk.ObjState state = obj.state;
        float power = 0;
        List<string> bb = state.GetState("burnable") as List<string>;
        List<string> br = state.GetState("burner") as List<string>;
        obj.Animated = sprites[obj.index].Get("burn", (SpriteManager.Toward)0);
        while (true)
        {
            if (bb.Count == 0) break;
            ItemData.Trasformable idt = Item.GetData(bb[0]).trasformables["burn"];
            while (idt.degree > power)
            {
                if (br.Count == 0) break;
                power += Item.GetData(br[0]).burnPw;
                br.RemoveAt(0);
            }
            yield return new WaitForSeconds(idt.time);
            power -= idt.degree;
            bb.RemoveAt(0);
            Drops.Load(idt.trasformed, 1, obj.transform.position);
        }
        obj.Animated = sprites[obj.index].Get(bb.Count == 0 && br.Count == 0 ? "void" : "contain", (SpriteManager.Toward)0);
    }

    public void ChangeVision()
    {
        spt.rotation = facing;
        SpriteManager sm = sprites[index];
        sm.ToAnimate(ld.curAction, ld.angleinit, spriteR, ref animCor, ref animDt);

    }
    public SpriteManager.Compare animDt;

    public static void Change(string changedTypeName, ObjData changer)
    {
        int index = oTy.IndexOf(changedTypeName);
        data[index] = changer;
        oTy[index] = changer.name;
    }
    public static void Add(ObjData adder)
    {
        data.Add(adder);
        oTy.Add(adder.name);
    }
    public static void PartialChange(string changedObjType, Mod.PartialChanger[] changer)
    {
        int index = oTy.IndexOf(changedObjType);
        Type t = typeof(ObjData);

        for (int i = 1; i < changer.Length; i++)
        {
            FieldInfo f = t.GetField(changer[i].changedName, BindingFlags.Public | BindingFlags.Instance);
            f.SetValue(data[index], changer[index].changer);
        }
    }
    public static void MothernalAdd(string mothernalType, Mod.PartialChanger[] changer)
    {
        ObjData od = Data.ReadJson<ObjData>(Mod.modPath + Mod.curLoadModName + "/objects/" + mothernalType + ".json");
        Type t = typeof(ObjData);

        foreach (Mod.PartialChanger pc in changer)
        {
            FieldInfo f = t.GetField(pc.changedName, BindingFlags.Public | BindingFlags.Instance);
            f.SetValue(od, pc.changer);
        }
        Add(od);
    }
    public static void SetSprite(string typeName, SpriteManager spM)
    {
        sprites.Insert(oTy.IndexOf(typeName), spM);
    }
    /// <summary>
    /// load a object in the scene
    /// </summary>
    public static GameObject Load(int index, Vector3 position, bool registIn = true)
    {
        return Load(oTy[index], position, registIn);
    }
    /// <summary>
    /// This Method cannot regist in the chunk data
    /// </summary>
    /// <param name="ostate"></param>
    /// <param name="cp"></param>
    /// <returns></returns>
    public static GameObject Load(Chunk.ObjState ostate, Vector2Int cp)
    {
        GameObject o = Instantiate((GameObject)Resources.Load("object"));
        Obj ob = o.GetComponent<Obj>();
        ob.cp = cp;
        ob.ld = ostate.ld;
        ob.state = ostate;

        return o;
    }
    public static GameObject Load(ObjLoadData ld, Vector2Int cp, bool registIn = true)
    {
        Ct.ct.CT(Loadd(ld, cp, registIn));
        return loaded;
    }
    private static GameObject loaded;
    private static System.Collections.IEnumerator Loadd(ObjLoadData ld, Vector2Int cp, bool registIn = true)
    {
        GameObject o = Instantiate((GameObject)Resources.Load("object"));
        Obj ob = o.GetComponent<Obj>();
        ob.cp = cp;
        ob.ld = ld;

        if (registIn)
        {
            if (!Ct.world.loadedChunk.ContainsKey(cp))
            {
                Ct.ct.CT(Ct.world.ChunkManager(cp));
                Ct.world.loadedChunk.Add(cp, Ct.world.managingChunk);
            }
            Ct.world.loadedChunk[cp].LoadinObj(ld, out int ind);
            ob.state = Ct.world.loadedChunk[cp].objs[ind];
            ob.stateIndex = ind;
            if (ob.state != Ct.world.loadedChunk[cp].objs[ind])
                Debug.Log("not same");
        }

        loaded = o;
        yield return null;
    }

    public static GameObject Load(string type, Vector3 position, bool registIn = true)
    {
        ObjLoadData ld = new()
        {
            name = type,
            relapos = new(V3.ToRelaPos(position)),
            angleinit = Ct.curWd.camAngle
        };
        Vector2Int cp = WorldGenerator.ToChunkOfPos(position);

        return Load(ld, cp, registIn);
    }
    public static void Destroy(GameObject obj, string type)
    {
        Vector2Int cp = WorldGenerator.ToChunkOfPos(obj.transform.position);

        Ct.world.loadedChunk[cp].LoadoutObj(obj.GetComponent<Obj>().state);
        Destroy(obj);
    }
    public static ObjData GetData(int index) => data[index];
    public static ObjData GetData(string type)
    {
        if (type == "n")
            return null;
        else
        {
            try
            {
                return GetData(oTy.IndexOf(type));
            }
            catch
            {
                Debug.LogError($"Type  '{type}' is not exist");
                return default;
            }
        }
    }

    private System.Collections.IEnumerator Animating()
    {
        int i = 0;
        float pretime = 0;
        while (true)
        {
            AnimatedSprite.Frame f = asp.frames[i++];
            float t = f.time - pretime;

            spriteR.sprite = f.sprite;
            yield return new WaitForSeconds(t);

            if (i == asp.frames.Count)
            {
                i = 0;
                pretime = 0;
            }
        }
    }
    private AnimatedSprite asp;

    public AnimatedSprite Animated
    {
        get => asp;
        set
        {
            asp = value;
            if (value == null)
            {
                if (animCor != null)
                {
                    Ct.ct.Cta(animCor);
                    animCor = null;
                }
            }
            else
            {
                if (animCor != null)
                {
                    Ct.ct.Cta(animCor);
                    animCor = null;
                }
                if (value.animated)
                    animCor = Ct.ct.CT(Animating());
                else
                    spriteR.sprite = value.Get().sprite;
            }
        }
    }
    private Coroutine animCor;

    public delegate void Interaction(ObjData data, Obj obj);

    private void OnDestroy()
    {
        onDestroy?.Invoke();
        Ct.evn.WhenVisionRotating -= ChangeVision;
        Ct.evn.WhenVisionElevate -= ChangeVision;
    }
}

[Serializable]
public class ObjData
{
    public string name;
    /// <summary>
    /// if is a just path the obj is not towardable, 
    /// if is a 4 path is towardable
    /// </summary>
    public object spritePath;
    public Vector2 spritePivot = new(-1, -1);
    public float thickness = 0.3f;
    public bool collision = true;
    public Collider collider = new();
    public Break breaking = new();
    public Drop[] drops;
    public Growable growable;
    public Container container;
    public Interacter[] interacters;
    public string itc;

    public string[] initstates;

    public object spriteobj
    {
        get => spritePath == null ? name + ".png" : spritePath;
    }
    [Serializable]
    public class Collider
    {
        public Vector2 validPx;//the not trasparent part
        public Vector3 center;
        public Vector2 added = new();//The added part of validPx
        public float depth = 0.2f;//the axis z of collider

        /// <summary>
        /// value to calculate
        /// </summary>
        public Rect size;
        /// <summary>
        /// checj if the calcolation is compield
        /// </summary>
        public bool calcutated = false;
    }
    [Serializable]
    public class Break
    {
        public float hardness;
        public object fittool = "everything";
    }
    [Serializable]
    public class Drop
    {
        public string item;
        public float rate;
        /// <summary>
        /// has must < than 1
        /// </summary>
        public float shrink;

        /// <summary>
        /// to get the drop of every item
        /// </summary>
        /// <returns></returns>
        public int GetQuantity()
        {
            int a = 0;
            float dr = rate;
            while (true)
            {
                float r = UnityEngine.Random.Range(0, 100);
                if (r <= dr)
                {
                    a++;
                    dr *= 1 - shrink;//rate reduced
                }
                else
                    return a;
            }
        }
    }
    [Serializable]
    public class Growable
    {
        public string nextPhase;
        /// <summary>
        /// possibility to change to next phase
        /// </summary>
        public float possibility;
    }
    public class Container
    {
        public int cell = 16;
    }
    /// <summary>
    /// can use when the itc change sprite of obj
    /// </summary>
    public class Interacter
    {
        public string interact;
        public string action;
    }
    public void GetDrops(Vector3 position)
    {
        if (drops == null)
        {
            Debug.Log("is null");
            return;
        }
        foreach (Drop d in drops)
        {
            Drops.Load(d.item, d.GetQuantity(), position);
        }
    }
    public object GetPath()
    {
        if (spritePath == null)
            return name + ".png";
        else
            return spritePath;
    }
}
/// <summary>
/// is data of obj to load in the world
/// </summary>
[Serializable]
public class ObjLoadData
{
    public string name = "n";
    public V3 relapos = new();
    /// <summary>
    /// can calculated
    /// </summary>
    public V3 size = new();
    /// <summary>
    /// can calculated
    /// </summary>
    public V3 center = new();

    public V3 rotation = new();
    public float angleinit;
    public string curAction = SpriteManager.single;

    public bool calcutated = true;
}
[Serializable]
public class V3
{
    public float x, y, z;

    public Vector3 ToVec() => new(x, y, z);
    public Vector3 ToVec(Vector2Int cp) => new(x + cp.x * WorldGenerator.units_of_chunk, y, z + cp.y * WorldGenerator.units_of_chunk);

    public void FromVec(Vector3 vec)
    {
        x = vec.x;
        y = vec.y;
        z = vec.z;
    }
    public static V3 Get(Vector3 vec)
    {
        return new(vec);
    }
    public V3()
    { }
    public V3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public V3(Vector3 vec)
    {
        x = vec.x;
        y = vec.y;
        z = vec.z;
    }
    /// <summary>
    /// To relative posiotion at chunk the y is not influenzzable
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static Vector3 ToRelaPos(Vector3 pos)
    {
        Vector2Int cp = WorldGenerator.ToChunkOfPos(pos);
        Vector3 rela = WorldGenerator.To3DPos(cp * WorldGenerator.units_of_chunk);
        return pos - rela;
    }
}

public class ObjSprite
{
    public bool towardable = false;
    public List<Sprite> sprites = new();

    /// <summary>
    /// Load the sprite, if the paths is not 4, get just the first
    /// </summary>
    /// <param name="paths"></param>
    /// <param name="from">the path biging to mod name, modName+path</param>
    public void Load(object paths, string from, Vector2 pivot, int ppu)
    {
        string p = Mod.modPath + from + "/";
        if (paths is string)
        {
            sprites.Add(Mod.LoadSprite(p += paths, pivot, ppu));
        }
        else
        {
            JArray array = (JArray)paths;
            string[] ps = array.Select(t => (string)t).ToArray();
            if (ps.Length == 4)
            {
                towardable = true;
                foreach (string s in ps)
                {
                    sprites.Add(Mod.LoadSprite(p + s, pivot, ppu));
                }
            }
            else
            {
                sprites.Add(Mod.LoadSprite(p += ps[0], pivot, ppu));
            }
        }

    }
    public void Load(ObjData data, string modName)
    {
        Load(data.GetPath(), modName + "/sprites", data.spritePivot, SMath.Spr.pxPerUnit);
    }
    public Sprite Get(int index)
    {
        if (index >= sprites.Count)
            return null;
        return sprites[index];
    }
    public Sprite Get() => sprites[0];
    /// <summary>
    /// if it errors return the first sprite
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="wardAngle"></param>
    /// <returns></returns>
    public Sprite Get(float wardAngle)
    {
        float relaa = SMath.AngleStandardization(wardAngle - Ct.curWd.camAngle + 45);
        return sprites[(int)relaa / 90];
    }
    public void Set(int index, Sprite sprite)
    {
        if (index >= sprites.Count)
            return;

        sprites[index] = sprite;
    }


    public ObjSprite(object paths, string from) => Load(paths, from, new(), SMath.Spr.pxPerUnit);
    public ObjSprite(ObjData data, string modName) => Load(data, modName);
}

public class SpriteManager
{
    public Dictionary<string, List<AnimatedSprite>> managed = new();
    public bool towardable = false;
    public bool singleAnimation = true;
    public string initAnimation = single;
    public static string single = "single";

    public AnimatedSprite Get(string action, Toward toward)
    {
        if (!managed.ContainsKey(action))
            return null;
        return managed[action][(int)toward];
    }
    public AnimatedSprite Get(string action, float initAngle, out Compare compare)
    {
        compare = new()
        {
            actionName = action,
            toward = GetToward(initAngle)
        };
        return Get(action, compare.toward);
    }
    public void ToAnimate(string action, float initangle, SpriteRenderer spr, ref Coroutine cor, ref Compare curAnimDt)
    {
        AnimatedSprite asp;
        Compare compare;
        if (towardable)
            asp = Get(action, initangle, out compare);
        else
        {
            asp = Get(action, 0);
            compare = new(action, 0);
        }
        ToAnimate(asp, spr, ref cor, compare, ref curAnimDt);
    }
    public void ToAnimate(string action, Toward toward, SpriteRenderer spr, ref Coroutine cor, ref Compare curAnimDt)
    {
        var asp = Get(action, toward);
        Compare com = new(action, toward);

        ToAnimate(asp, spr, ref cor, com, ref curAnimDt);
    }
    public void ToAnimate(AnimatedSprite asp, SpriteRenderer spr, ref Coroutine cor, Compare compare, ref Compare curAnimDt)
    {
        if (compare != curAnimDt)
        {
            if (cor != null)
            {
                Ct.ct.Cta(cor);
                cor = null;
            }
            if (asp != null)
            {
                if (asp.animated)
                    cor = Ct.ct.CT(Animating(spr, asp));
                else
                    spr.sprite = asp.Get().sprite;
            }

            curAnimDt = compare;
        }
    }
    private static System.Collections.IEnumerator Animating(SpriteRenderer spr, AnimatedSprite asp)
    {
        int i = 0;
        float pretime = 0;
        while (true)
        {
            AnimatedSprite.Frame f = asp.frames[i];
            float t = f.time - pretime;

            spr.sprite = f.sprite;

            if (++i == asp.frames.Count)
            {
                i = 0;
                pretime = 0;
            }
            else
                pretime = t;
            yield return new WaitForSeconds(t);
        }
    }

    public static Toward GetToward(float init)
    {
        float relaa = SMath.AngleStandardization(init - Ct.curWd.camAngle + 45);
        return (Toward)((int)relaa / 90);
    }
    /// <summary>
    /// Get sprite of first action of fiest time
    /// </summary>
    /// <returns></returns>
    public Sprite Get() => managed[initAnimation][0].Get(0);
    public class SpriteData
    {
        public bool standard = true;
        public bool towardable = false;
        public object content;

        public string folder = "sprites/";
        public string form = ".png";
        public string initAnimation = single;

        public Vector2 pivot = defPivot;
        public PivotSet pivotSet = PivotSet.fixedPivot;
        public static Vector2 defPivot = new(114514, 1919180);

        public SpriteManager Load()
        {
            SpriteManager sm = new()
            {
                initAnimation = initAnimation,
                towardable = towardable
            };
            string path = Mod.modPath + Mod.curLoadModName + "/" + folder;

            if (standard)
            {
                if (content is not string)//return if the 'content' is not a string
                    return sm;

                foreach (string full in Directory
                    .GetFiles(path)
                    .Where(file =>
                    {
                        string fileName = Path.GetFileName(file);
                        return fileName.StartsWith(content as string) && !fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList())
                {
                    Sprite sp = LoadSprite(full);
                    string[] x = Path.GetFileName(full).TrimEnd(form).Split(',');// format: "spriteName,actionName,time,toward.png"


                    string actName = x[1] == "" ? single : x[1];
                    float time = x[2] == "" ? 0 : float.Parse(x[2]);
                    int toward = x[3] == "" ? 0 : int.Parse(x[3]);

                    if (!sm.managed.ContainsKey(actName))
                        sm.managed.Add(actName, Enumerable.Range(0, 4)
                               .Select(_ => new AnimatedSprite())
                               .ToList());
                    sm.managed[actName][toward].Load(sp, time);
                }
            }
            else
            {
                if (content is string)
                {
                    Sprite sp = LoadSprite(path + content);
                    sm.managed.Add(single, new()
                    {
                        new()
                        {
                            frames = new()
                            {
                                new(sp,0)
                            }
                        }
                    });
                }
            }

            return sm;
        }
        public Sprite LoadSprite(string path)
        {
            Vector2 piv;
            switch (pivotSet)
            {
                case PivotSet.normal:
                    piv = new(0.5f, 0.5f);
                    break;
                case PivotSet.adaptForAnySprite:
                    Sprite sp = Mod.LoadSprite(path);
                    sp = Sprite.Create(sp.texture, sp.rect, GetPivot(sp), SMath.Spr.pxPerUnit);
                    return sp;
                case PivotSet.fixedPivot:
                    if (pivot == defPivot)//if pivot not set, calculate the pivot
                    {
                        Sprite spr = Mod.LoadSprite(path);
                        pivot = GetPivot(spr);
                        return Sprite.Create(spr.texture, spr.rect, pivot, SMath.Spr.pxPerUnit);
                    }
                    piv = pivot;
                    break;
                default:
                    return null;
            }
            return Mod.LoadSprite(path, piv);
        }


        public enum PivotSet
        {
            normal,
            adaptForAnySprite,
            fixedPivot//use the pivot variable
        }
    }
    /// <summary>
    /// Basic Load, let the pivot in center of sprite
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static SpriteManager Load(object data)
    {
        SpriteManager sm = new();

        if (data is string)
        {
            string d = data as string;
            SpriteData sd = new();
            sd.content = d;

            if (d.Contains('.'))
                sd.standard = false;
            sm = sd.Load();
        }
        else
        {
            JObject jo = data as JObject;
            SpriteData sd = JsonConvert.DeserializeObject<SpriteData>(jo.ToString());
            sm = sd.Load();
        }

        return sm;
    }

    public static Sprite RevisePivot(Sprite sp)
    {
        return Sprite.Create(sp.texture, sp.rect, GetPivot(sp), SMath.Spr.pxPerUnit);
    }
    /// <summary>
    /// do not get the true piivot, but cna be calculate 
    /// </summary>
    /// <param name="sp"></param>
    /// <returns></returns>
    public static Vector2 GetPivot(Sprite sp)
    {
        Rect valid = SMath.Spr.GetValidPixels(sp);
        Rect full = sp.rect;

        // 计算归一化 pivot 坐标：相对整个 sprite 的尺寸 (0-1)
        float pivotX = (valid.x + valid.width * 0.5f) / full.width;
        float pivotY = (valid.y) / full.height;

        return new Vector2(pivotX, pivotY);
    }
    public enum Toward
    {
        right,
        up,
        left,
        down
    }
    public class Compare
    {
        public string actionName;
        public Toward toward;

        public Compare() { }
        public Compare(string name, Toward toward)
        {
            actionName = name;
            this.toward = toward;
        }
    }
}
public class AnimatedSprite
{
    public List<Frame> frames;
    public bool animated = false;
    public void Load(Sprite sp, float time)
    {
        frames ??= new();
        if (frames.Count == 1)
            animated = true;

        Frame f = new(sp, time);
        int index = frames.FindIndex(obj => time < obj.time);
        if (index == -1)
            frames.Add(f);
        else
            frames.Insert(index, f);
    }
    public Sprite Get(int index)
    {
        if (frames.Count < index)
            return null;
        return frames[index].sprite;
    }
    public Sprite Get(float time) => Get(frames.FindIndex(obj => time < obj.time));
    /// <summary>
    /// return frame of index 0
    /// </summary>
    /// <returns></returns>
    public Frame Get() => frames[0];
    public class Frame
    {
        public Sprite sprite;
        public float time;

        public Frame(Sprite sp, float time)
        {
            sprite = sp;
            this.time = time;
        }
    }

}