using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public static List<ObjSprite> sprites = new();
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
    public virtual void Start()
    {
        c++;

        if (!CompareTag("Player"))
        {
            if(CompareTag("Object"))
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

            transform.position = ld.relapos.ToVec(cp);
            transform.rotation = Quaternion.Euler(ld.rotation.ToVec());

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
    }
    public static int c = 0;

    public static void LoadDefualtInteractions()
    {
        interactions.Add("container", Container);

        Page.Add("container", () => {
            Ct.DestroyAll(Ct.ct.chestView);
            Ct.ct.chestView.gameObject.SetActive(true);
            Ct.ct.invp.gameObject.SetActive(true);
        }, () => {
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
        if(!state.states.ContainsKey("container"))
        {
            state.Regist("container", new Inventory(data.container.cell));
        }
        Inventory inv = state.GetState("container") as Inventory;
        Ct.selectContainerInv = inv;
        inv.WhenInvChange += InventoryPage.OnUpdate;

        GameObject grid = Resources.Load("chestgrid") as GameObject;
        int x, y, len = 49;

        for(int i = 0; i < inv.invt.Count; i++)
        {
            y = i / 4;
            x = i % 4;
            GameObject g = Instantiate(grid, Ct.ct.chestView);
            g.name = i.ToString();
            g.GetComponent<RectTransform>().localPosition = new(x * len - len *2, -y * len + len * 2);
            SButton sb = g.GetComponent<SButton>();
            sb.onClick.AddListener(() => { inv.Switch(int.Parse(g.name), Ct.mouseSelected.select, out Ct.mouseSelected.select); Ct.mouseSelected.WhenSwitch(); });
        }
        inv.Invchange();
    } 

    public void ChangeVision()
    {
        spt.rotation = facing;
        ObjSprite os = sprites[index];
        if (os.towardable)
        {
            Sprite sp = os.Get(ld.angleinit);
            if(sp != spriteR.sprite)
            {
                spriteR.sprite = sp;
            }
        }
    }
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
    public static void SetSprite(int index, ObjSprite sprite)
    {
        sprites.Insert(index, sprite);
    }
    public static void SetSprite(string typeName, ObjSprite sprite)
    {
        sprites.Insert(oTy.IndexOf(typeName), sprite);
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
        GameObject o = Instantiate((GameObject)Resources.Load("object"));
        Obj ob = o.GetComponent<Obj>();
        ob.cp = cp;
        ob.ld = ld;

        if (registIn)
        {
            Vector3 p = ld.relapos.ToVec(cp);
            if (!Ct.world.loadedChunk.ContainsKey(cp))
            {
                Ct.world.loadedChunk.Add(cp, Ct.world.ChunkManager(cp));
            }
            Ct.world.loadedChunk[cp].LoadinObj(ld, out int ind);
            ob.state = Ct.world.loadedChunk[cp].objs[ind];
            ob.stateIndex = ind;
            if (ob.state != Ct.world.loadedChunk[cp].objs[ind])
                Debug.Log("not same");
        }

        return o;
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

    public delegate void Interaction(ObjData data, Obj obj);

    private void OnDestroy()
    {

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
    public string itc;

    public string[] initstates;

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
            sprites.Add(Mod.LoadSprite(p += paths,pivot, ppu));
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
        if(index >= sprites.Count)
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
