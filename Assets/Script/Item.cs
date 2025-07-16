using IPGModAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public class Item
{
    public static List<string> iTy = new();
    public static List<ItemData> data = new();
    public static List<Sprite> sprites = new();
    public static Dictionary<string, Interaction> interactions = new();

    public static void Change(string changedTypeName, ItemData changer)
    {
        int index = iTy.IndexOf(changedTypeName);
        data[index] = changer;
        iTy[index] = changer.name;
    }
    public static void Add(ItemData adder)
    {
        data.Add(adder);
        iTy.Add(adder.name);
    }
    public static void PartialChange(string changedObjType, Mod.PartialChanger[] changer)
    {
        int index = iTy.IndexOf(changedObjType);
        Type t = typeof(ObjData);

        for (int i = 1; i < changer.Length; i++)
        {
            FieldInfo f = t.GetField(changer[index].changedName, BindingFlags.Public | BindingFlags.Instance);
            f.SetValue(data[index], changer[index].changer);
        }
    }

    public static void MothernalAdd(string mothernalType, Mod.PartialChanger[] changer)
    {
        ItemData od = Data.ReadJson<Mod.MItem>(Mod.modPath + Mod.curLoadModName + "/items/" + mothernalType + ".json").data;

        foreach (Mod.PartialChanger pc in changer)
        {
            Data.SetPropertyPath(od, pc.changedName, pc.changer);
        }

        Add(od);
    }



    public static void SetSprite(int index, Sprite sprite)
    {
        sprites.Insert(index, sprite);
    }
    public static void SetSprite(string typeName, Sprite sprite)
    {
        sprites.Insert(iTy.IndexOf(typeName), sprite);
    }
    public static ItemData GetData(int index) => data[index];
    public static ItemData GetData(string type) => GetData(iTy.IndexOf(type));

    public static Sprite GetSprite(int index)
    {
        try
        {
            return sprites[index];
        }
        catch
        {
            Debug.LogError($"[GetSprite] the sprite is not exist : {index}, {iTy[index]}");
            return null;
        }
    }
    public static Sprite GetSprite(string type)
    {
        try
        {
            return GetSprite(iTy.IndexOf(type));
        }
        catch
        {
            Debug.LogError($"[GetSprite] the sprite is not exist : {type}");
            return null;
        }
    }

    public static bool IsTool(string item)
    {
        return GetData(item).tool != null;
    }

    public delegate void Interaction(ItemData data);

    public static void Interact(InteractionType ictt, ItemData data)
    {
        if (!Page.IsPage("main"))
            return;
        int i;
        switch (ictt)
        {
            case InteractionType.lefttap:
                i = 0;
                break;
            case InteractionType.righttap:
                i = 1;
                break;
            case InteractionType.leftpress:
                i = 2;
                break;
            case InteractionType.rightpress:
                i = 3;
                break;
            default:
                return;
        }

        string s = data.IctDet(i);
        if (s != null)
        {
            interactions[s].Invoke(data);
            if (data.IsTool())
                Ct.mouseSelected.Fray(1);
            else
                Ct.mouseSelected.Remove(1);
        }
    }
    public static void InctInitializzation()
    {
        interactions.Add("place", Place);
        interactions.Add("attack", Attack);
    }
    public static void Place(ItemData data)
    {
        if (!Ct.po.Placeable)
            return;

        if (data.placeable.condition == null)
        {
            Obj.Load(data.placeable.placed, Ct.po.transform.position);
        }
        else
        {
            string s = "";
            s= Ct.GetObjData()?.name;//raycasted
            if (s == data.placeable.condition)
            {
                Obj.Load(data.placeable.placed, Ct.ct.casted.transform.position);
                Obj.Destroy(Ct.ct.casted, s);
            }
        }
    }
    public static void Attack(ItemData data)
    {
        foreach (Entity e in Player.entitiesAround)
        {
            Vector3 p = e.gameObject.transform.position;
            Vector3 ward = p - Ct.ppw;
            float l = ward.magnitude;
            if(l <= data.tool.attackLength)
            {
                float h = data.tool.attackAngle / 2;
                float mina = Ct.wmp_a - h;
                float maxa = Ct.wmp_a + h;
                float ang = SMath.AngleStandardization(SMath.Angle(ward));

                if (ang > mina && ang < maxa)
                {
                    e.Repelling(ward.normalized * data.tool.knockback, data.tool.damage);
                }
            }
        }
    }
    public enum InteractionType
    {
        lefttap, righttap, leftpress, rightpress
    }
}
[Serializable]
public class ItemData
{
    public string name = "";
    /// <summary>
    /// max amount of item
    /// </summary>
    public int maxAmt = 60;
    /// <summary>
    /// /Has 4 interactions: lefttap, righttap, leftpress, rightpress
    /// </summary>
    public object[] itc = new object[4];
    public List<string> tags;

    public Placeable placeable;
    public Tool tool;
    public Consumable consumable;
    public Dictionary<string, Trasformable> trasformables = new();
    public int burnPw;
    public enum Interaction
    {
        n,
        custom,
        place,
        attack,
        broken
    }
    [Serializable]
    public class Placeable
    {
        public string placed;
        /// <summary>
        /// condition obj to place the placed
        /// </summary>
        public string condition;
    }
    [Serializable]
    public class Tool
    {
        public int durability;
        public string type = "";
        public float efficiency;

        public bool arm = false;
        public int damage = 0;
        public float attackAngle;
        public float attackLength;
        public float knockback;
    }
    [Serializable]
    public class Consumable
    {
        public string[] effects;
    }
    [Serializable]
    public class Trasformable
    {
        public string trasformed;
        public float degree;
        public float time;
    }

    public bool IsTool() => tool != null;
    public string IctDet(int index)
    {
        switch (itc[index])
        {
            case int i:
                if (i == 0)
                    return null;
                else
                    return ((Interaction)i).ToString();
            case string s:
                return s;
            default:
                return null;
        }
    }
    public Vector3[] AttackViewer()
    {
        int rendered = 45;

        List<Vector3> list = new();
        Vector3 forward = Ct.wmp - Ct.ppw;
        float apr = tool.attackAngle / rendered;//angle per every rendered line
        Ct.wmp_a = SMath.AngleStandardization(SMath.Angle(forward));
        float initAngle = Ct.wmp_a - tool.attackAngle / 2;
        Debug.Log("length: " + tool.attackLength);

        for(int i = 0; i< rendered; i++)
        {
            float angle = initAngle + apr * i;
            float x = SMath.CosA(angle) * tool.attackLength;
            float z = SMath.SinA(angle) * tool.attackLength;
            Vector3 rp = new(x, 0, z);

            list.Add(rp + Ct.ppw);
        }

        return list.ToArray();
    }
}

[Serializable]
public class Inventory
{
    public List<Grid> invt = new();
    public event Update WhenInvChange;
    public static Transform collection;

    public bool full = false;
    public Grid GetGrid(int index)
    {
        return invt[index];
    }
    /// <summary>
    /// add never use the index, it can auto look for it
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amt"></param>
    /// <param name="full">if return 0, the inventory is full</param>
    public void Add(string item, int amt, out int full)
    {
        Grid grid = new(amt,item);
        Add(grid, out full);
        if(full> 0)
        {
            Drops.Load(item, full, Ct.ppw);
        }
    }
    public void Add(List<string> items, out bool full, bool excludeTool = false)
    {
        List<Grid> backup = JsonConvert.DeserializeObject<List<Grid>>(JsonConvert.SerializeObject(invt));

        foreach (string o in items)
        {
            if (excludeTool && Item.GetData(o).IsTool())
                continue;
            Add(o, 1, out int f);
            if (f > 0)
            {
                full = true;
                invt = backup;
                return;
            }
        }
        full = false;
    }
    public void Add(string items, out bool full, bool excludeTool = false)
    {
        string[] s = items.Split(',');
        Add(s.ToList(), out full, excludeTool);
    }
    public void Add(int index, int amt, out int full)
    {
        Grid g = GetGrid(index);
        g.Add(amt, out full);
        Invchange();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="add">if would remove input -1</param>
    public void Add(Grid grid, out int full, int add = 1)
    {
        Grid g = SearchItemGrid(grid.item);
        g ??= SearchEmptyGrid(grid.item);
        if(g == null)
        {
            full = grid.amt;
            this.full = true;
            return;
        }
        else
        {
            this.full = false;
        }

        g.Add(grid.amt * add, out int f2);
        if (f2 > 0)
        {
            grid.amt = f2;
            Add(grid, out int f3, add);
            if (f3 > 0)
            {
                full = f3;
                g.durab = SetTool(grid.item);
                Invchange();
                return;
            }
        }
        full = 0;
        g.durab = SetTool(grid.item);
        Invchange();

    }
    public void Switch(int index, Grid from, out Grid to)
    {
        to = invt[index];
        invt[index] = from;
        Invchange();
    }
    /// <summary>
    /// search the grid,if have not the item grid return null
    /// </summary>
    /// <param name="item"></param>
    /// <param name="searchfullgrid"></param>
    /// <returns></returns>
    public Grid SearchItemGrid(string item, bool searchfullgrid = false)
    {
        for (int ind = 0; ind < invt.Count; ind++)
        {
            if (invt[ind].item == item)
            {
                if (!searchfullgrid && invt[ind].amt < Item.GetData(item).maxAmt)
                {
                    return invt[ind];
                }
                else if (searchfullgrid)
                    return invt[ind];
                else
                    continue;
            }
        }
        return null;
    }
    public bool HasFreeItemGrid(string item)
    {
        foreach (Grid grid in invt)
        {
            if(grid.item == item && grid.amt < Item.GetData(item).maxAmt)
                return true;
        }
        return false;
    }
    public Grid SearchEmptyGrid(string insertItem = "n")
    {
        for (int ind = 0; ind < invt.Count; ind++)
        {
            if (invt[ind].item == "n")
            {
                invt[ind].item = insertItem;
                return invt[ind];
            }
        }
        return null;
    }
    public Inventory(int grids)
    {
        while (grids-- > 0)
        {
            invt.Add(new());
        }
    }
    [Serializable]
    public class Grid
    {
        /// <summary>
        /// amount
        /// </summary>
        public int amt;
        /// <summary>
        /// type of item
        /// </summary>
        public string item = "n";
        /// <summary>
        /// durability
        /// </summary>
        public int durab = -1;

        /// <summary>
        /// add in the grid, if it is full return surplus, if the grid is empty return full complete
        /// remove in the grid, if it is <0 return full = -amtT
        /// </summary>
        public void Add(int amt, out int full)
        {
            if (item == "n")
            {
                full = amt;
                return;
            }

            full = 0;
            int max = Item.GetData(item).maxAmt;
            this.amt += amt;
            switch (this.amt)
            {
                case int i when i > max:
                    full = this.amt - max;
                    this.amt = max;
                    break;
                case 0:
                    item = "n";
                    durab = -1;
                    break;
                case < 0:
                    full = -this.amt;
                    this.amt = max;
                    item = "n";
                    durab = -1;
                    break;
            }
        }
        public void Fray(int frayed)
        {
            durab -= frayed;
            if (durab <= 0)
            {
                amt -= 1;
                durab = Item.GetData(item).tool.durability;
                if (amt <= 0)
                    item = "n";
            }

        }

        public void Insert(Grid grid, out Grid ot)
        {
            if (grid.item == item)//add to
            {
                Add(grid.amt, out int full);
                if (full > 0)
                {
                    ot = new(full, grid.item, grid.durab);
                }
                else
                    ot = new();
            }
            else//switch
            {
                ot = this;

                item = grid.item;
                amt = grid.amt; ;
                durab = grid.durab;
            }
        }
        public Grid(int amt = 0, string item = "n", int durab = -1)
        {
            this.amt = amt;
            this.item = item;
            this.durab = durab;
        }
    }
    public void Invchange()
    {
        WhenInvChange?.Invoke(this);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns>return burability of item if it is a tool</returns>
    public static int SetTool(string item)
    {
        if (Item.IsTool(item))
            return Item.GetData(item).tool.durability;
        else return -1;
    }

    public delegate void Update(Inventory inv);
}

public static class Crafting
{
    public static Dictionary<string, string> recipes = new();

    public static void Craft(List<string> materials, out bool craft)
    {
        craft = true;
        List<List<string>> a = Split(materials);
        foreach (List<string> o in a)
        {
            string key = Recipe.ToList(o);
            if (recipes.ContainsKey(key))
            {
                Ct.curWd.inventory.Add(recipes[key], out craft);
            }
            else
            {
                Ct.curWd.inventory.Add(key, out craft, true);
            }
        }
    }
    public static List<List<string>> Split(List<string> mats)
    {
        List<List<string>> s = new();
        int index = 0;
        s.Add(new());
        foreach (string o in mats)
        {
            if (o == "n")
            {
                index++;
                s.Add(new());
            }
            else
            {
                s[index].Add(o);
            }
        }
        return s;
    }
    public static void Load(Recipe[] recipes)
    {
        Debug.Log("[Crafting]Load the recipes");
        foreach (Recipe recipe in recipes)
        {
            string key = recipe.GetKey();
            string val = recipe.GetProduct();
            Crafting.recipes.Add(key, val);
        }
    }
}
[Serializable]
public class Recipe
{
    /// <summary>
    /// cantain also tools
    /// </summary>
    public string[] materials;
    public string[] products;


    public enum Mode
    {
        toolworking,
        crafting
    }
    public string GetKey() => ToList(materials);
    public string GetProduct() => ToList(products);


    public static string ToList(string[] items)
    {
        StringBuilder s = new();
        foreach (string item in items)
        {
            s.Append(item);
            s.Append(",");
        }
        return s.ToString().TrimEnd(',');
    }
    public static string ToList(List<string> items) => ToList(items.ToArray());
}