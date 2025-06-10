using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using static WorldGenerator.BiomeData;

public class World : MonoBehaviour
{
    public Dictionary<Vector2Int, Chunk> loadedChunk = new();

    public Vector2Int precp = new(114514, 1919810);
    private void Start()
    {
        Ct.world = this;
        Ct.evn.WhenPlayerMoving += PlayerMoving;
        if (!Data.DIrectioryExists(WorldGenerator.chunksPath))
            Data.Create(WorldGenerator.chunksPath);

        Ct.ct.CT(GrowingSystem.Growing());

        Ct.command.CommandByPath(Data.worldPath + Ct.set.preWorld + "/preload.txt");
    }

    public void PlayerMoving()
    {
        Vector2Int cp = Ct.cp;
        Ct.pp = WorldGenerator.ToPlanPos(Ct.ppw);
        Ct.cp = WorldGenerator.ToChunkOfPos(Ct.pp);

        if (cp == precp)
            return;

        Dictionary<Vector2Int, Chunk> renderedChunk = new();
        for (int i = -Ct.curWd.radius_renderChunk; i < Ct.curWd.radius_renderChunk; i++)
        {
            for (int j = -Ct.curWd.radius_renderChunk; j < Ct.curWd.radius_renderChunk; j++)
            {
                Vector2Int rp = new(i, j);
                if (rp.magnitude <= Ct.curWd.radius_renderChunk)
                {
                    Vector2Int ocp = cp + rp;
                    renderedChunk.Add(ocp, ChunkManager(ocp));
                }
            }
        }
        WorldGenerator.Comparing(renderedChunk);

        loadedChunk = renderedChunk;

        precp = cp;
    }
    public Chunk ChunkManager(Vector2Int cp)
    {
        if (loadedChunk.ContainsKey(cp)) return loadedChunk[cp];
        string path = WorldGenerator.chunksPath + WorldGenerator.ToPath(cp) + ".bin";
        if (Data.FileExists(path)) return Data.ReadBinary<Chunk>(path);
        else return WorldGenerator.Generating(new(cp));
    }
    public void Saving()
    {
        foreach(var v in loadedChunk.Keys)
        {
            Data.WriteBinary(Ct.world.loadedChunk[v], WorldGenerator.chunksPath + WorldGenerator.ToPath(v) + ".bin");
        }
    }
}
public static class WorldGenerator
{
    public static List<string> biomeNames = new();
    public static Dictionary<string, BiomeData> biomes = new();
    public static int units_of_biome = Ct.curWd.units_of_biome;
    public static int half_of_chunk = units_of_chunk / 2;
    public static int units_of_chunk = Ct.curWd.units_of_chunk;
    public static int chunk_per_biome = units_of_biome / units_of_chunk;
    public static List<Generator> generators = new();

    public static string chunksPath = Data.worldPath + Ct.curWd.name + "/chunks/";

    public static Dictionary<Vector2Int, List<GameObject>> objInChunk = new();

    public static Chunk Generating(Chunk ch)
    {

        if (generators != null)
            foreach (Generator g in generators)
            {
                ch = g.Invoke(ch);
            }
        return ch;
    }
    private static void Rendering(Chunk ch, Vector2Int cp)
    {
        int i = 0;
        foreach (Chunk.ObjState os in ch.objs)
        {
            if(!objInChunk.ContainsKey(cp))
                objInChunk.Add(cp, new());
            if (os.ld.name == "n")
                continue;
            objInChunk[cp].Add(Obj.Load(os, cp));

            i++;
        }
    }
    /// <summary>
    /// compare the ex-rendered chunks and current chunks
    /// </summary>
    /// <param name="chunks"></param>
    public static void Comparing(Dictionary<Vector2Int, Chunk> chunks)
    {
        HashSet<Vector2Int> removed = new();
        foreach (Vector2Int p in objInChunk.Keys)
        {
            if (!chunks.ContainsKey(p))
            {
                Data.WriteBinary(Ct.world.loadedChunk[p], chunksPath + ToPath(p) + ".bin");

                foreach (GameObject g in objInChunk[p])
                    UnityEngine.Object.Destroy(g);
                removed.Add(p);
            }
        }
        foreach (Vector2Int p in removed)
        {
            objInChunk.Remove(p);
        }
        foreach (Vector2Int p in chunks.Keys)
        {
            if (!objInChunk.ContainsKey(p))
            {
                objInChunk.Add(p, new());
                //Ct.ct.CT(Rendering(chunks[p], p));
                Rendering(chunks[p], p);
            }
        }
    }
    public static bool ExisistChunkFile(Vector2Int cp)
    {
        return Data.FileExists(chunksPath + ToPath(cp));
    }
    public static void SaveChunk(Chunk ch)
    {
        if (Data.DIrectioryExists(chunksPath))
            Data.Create(chunksPath);

        Data.WriteJson(ch, chunksPath + ch.GetCP());
    }
    public static string ToPath(Vector2Int cp)
    {
        return cp.x.ToString() + "_" + cp.y.ToString();
    }
    public static string GetObj(int biome, int seed)
    {
        try
        {
            return biomes[biomeNames[biome]].GetObj(seed);
        }

        catch
        {
            Debug.Log($"Error: in {biome}, is not exists");
            return default;
        }
    }
    
    public static void LoadingFromPath(string path)
    {
        BiomeData[] bd = Data.ReadJson<BiomeData[]>(path);
        foreach(BiomeData v in bd)
        {
            biomeNames.Add(v.name);
            biomes.Add(v.name,v);
        }
    }
    public static string GetBiome(int index) => biomeNames[index];
    public static int GetBiome(string biome) => biomeNames.IndexOf(biome);

    [Serializable]
    public class BiomeData
    {
        public string name;
        /// <summary>
        /// 0-1
        /// </summary>
        public float deafultSuccessGeneratePossibility = 1;
        public Obj[] objects;
        public Obj[] entities;
        public string GetObj(int seed)
        {
                Obj o = objects[SMath.Random(seed, objects.Length, 0)];
                float p = SMath.Random(1, 0f);
                if (o.generatePossibility > p)
                {
                    return o.type;
                }
                else
                    return "n";
        }
        public string GetEntity(int seed)
        {
            Obj o = entities[SMath.Random(seed, entities.Length-1, 0)];
            float p = SMath.Random(1, 0f);

            if (o.generatePossibility > p)
            {
                return o.type;
            }
            else
                return "n";
        }
        [Serializable]
        public class Obj
        {
            public string type;
            public float generatePossibility = 1;//0-1
        }
    }
    public delegate Chunk Generator(Chunk ch);

    //
    //Built-in generators and method
    //
    /// <summary>
    public static Chunk Bioming(Chunk ch)
    {
        int nog = SMath.Random(Ct.curWd.GetSeed(ch.GetCP()), Ct.curWd.worldMaxGenerateTimeOfChunk, Ct.curWd.worldMinGenerateTimeOfChunk);//number of generation 
        while (nog-- > 0)
        {
            Vector2 p = ch.GetRandomPosition(nog);
            int biome = ch.BiomeType(p);
            string type = GetObj(biome, nog);
            if (type == "n")
                continue;
            ch.LoadinObj(type, p, out _);
        }
        return ch;
    }

    /// <summary>
    /// the position relative at chunk
    /// </summary>
    /// <param name="pos"></param>
    /// <returns>return rela_pos</returns>
    public static Vector2 ToRelaPos(Vector2Int ch_pos, Vector2 pos)
    {
        Vector2 cp = ch_pos * units_of_chunk;
        return pos - cp;
    }
    public static Vector2 ToRelaPos(Vector3 pos)
    {
        return ToRelaPos(ToChunkOfPos(pos), ToPlanPos(pos));
    }
    public static Vector2 ToPlanPos(Vector3 pos)
    {
        return new(pos.x, pos.z);
    }
    public static Vector2Int ToChunkOfPos(Vector2 pos)
    {
        return SMath.V2.Floor(pos / units_of_chunk);
    }
    public static Vector2Int ToChunkOfPos(Vector3 d3)
    {
        return ToChunkOfPos(ToPlanPos(d3));
    }
    public static Vector3 To3DPos(Vector2 pos, float h = 0)
    {
        return new(pos.x, h, pos.y);
    }
}
[Serializable]
public class Chunk
{
    public int chunkx, chunky;
    public List<ObjState> objs = new();
    public List<EntityState> entities = new();

    public int Biome
    {
        get => GetBiome(GetBiomePosition());
    }

    public Vector2Int GetCP() => new(chunkx, chunky);
    public void SetCP(Vector2Int p)
    {
        chunkx = p.x;
        chunky = p.y;
    }
    [Serializable]
    public class ObjState
    {
        public Dictionary<string, object> states = new();
        public ObjLoadData ld;

        public Vector3 GetPosition(Vector2Int ch)
        {
            return ld.relapos.ToVec(ch);
        }
        public void SetPosition(Vector3 p)
        {
            ld.relapos.FromVec(WorldGenerator.ToRelaPos(p));
        }
        public void SetPosition(Vector2 rp)
        {
            ld.relapos.FromVec(WorldGenerator.To3DPos(rp));
        }
        public object GetState(string stateName)
        {
            return states[stateName];
        }
        public void Regist(string name, object val)
        {
            if(states.ContainsKey(name))
            {
                Debug.Log("The state has just regist in");
                return;
            }    
            else
            {
                states.Add(name, val);
            }
        }
    }
    [Serializable]
    public class EntityState
    {
        float x, y, z;
        public Dictionary<string,object> states;
        public string name;

        public Vector3 GetPosition()
        {
            return new(x, y, z);
        }
        public void SetPosition(Vector3 p)
        {
            x = p.x; y = p.y; z = p.z;
        }
        public void SetPosition(Vector2 p)
        {
            x = p.x; y = p.y;
        }
        public object GetState(string name)
        {
            return states[name];
        }
    }
    public void LoadinObj(ObjState obj,out int index)
    {
        index = objs.Count;
        objs.Add(obj);
    }
    /// <summary>
    /// Load obj by type neme and this position, getting init states in the data
    /// 通过类型名称和位置设置物体，会从data获取初始状态
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="pos">is relative position</param>
    public ObjState LoadinObj(string obj, Vector2 rela_pos, out int index)
    {
        index = -1;
        if (obj == "n")
            return null;
        string[] states = Obj.GetData(obj).initstates;
        ObjState os = new()
        {
            ld = new()
            {
                name = obj
            },
            states = new()
        };
        os.SetPosition(rela_pos);
        LoadinObj(os, out index);

        return os;
    }
    public ObjState LoadinObj(ObjLoadData ld, out int index)
    {
        ObjState os = new()
        {
            ld = ld,
            states = new()
        };
        LoadinObj(os, out index);
        return os;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="index">is index of oobjTypes</param>
    /// <param name="rela_pos"></param>
    public void LoadinObj(int index, Vector2 rela_pos, out int stateindex)
    {
        LoadinObj(Obj.oTy[index], rela_pos, out stateindex);
    }
    public void LoadoutObj(ObjState os)
    {
        objs.Remove(os);
    }
    public Vector2 GetRelaPos(Vector2 pos)
    {
        return WorldGenerator.ToRelaPos(GetCP(), pos);
    }
    /// <summary>
    /// get the domain of chunk's biome
    /// </summary>
    /// <returns></returns>
    public static Vector2Int GetBiomePosition(Vector2Int cp)
    {
        return SMath.V2.Floor(cp / WorldGenerator.chunk_per_biome);
    }
    public Vector2Int GetBiomePosition() => GetBiomePosition(GetCP());
    public Vector2 GetRandomPosition(int seed)
    {
        Vector2 p = new()
        {
            x = SMath.Random(Ct.curWd.GetSeed(GetCP()) * seed - 114, WorldGenerator.units_of_chunk, 0f),
            y = SMath.Random(Ct.curWd.GetSeed(GetCP()) + 514 * seed, WorldGenerator.units_of_chunk, 0f)
        };
        return p;
    }
    public Vector2 GetRandomPosition()
    {
        return GetRandomPosition(SMath.Random(int.MaxValue, int.MinValue));
    }
    /// <summary>
    /// biome type int the chunk
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public int BiomeType(Vector2 pos)
    {
        Vector2Int tw = new()
        {
            x = pos.x - WorldGenerator.half_of_chunk < 0 ? -1 : 1,
            y = pos.y - WorldGenerator.half_of_chunk < 0 ? -1 : 1,
        };

        float _closest = WorldGenerator.units_of_biome;
        int type = -1;

        Vector2Int cbp = GetBiomePosition();
        {
            Vector2Int bp = cbp;
            float l = SMath.V2.Length(pos, GetBiomePoint(bp));
            if (_closest > l)
            {
                _closest = l;
                type = GetBiome(bp);
            }
        }
        {
            Vector2Int bp = cbp + new Vector2Int(0, tw.y);
            float l = SMath.V2.Length(pos, GetBiomePoint(bp));
            if (_closest > l)
            {
                _closest = l;
                type = GetBiome(bp);
            }
        }
        {
            Vector2Int bp = cbp + new Vector2Int(tw.x, 0);
            float l = SMath.V2.Length(pos, GetBiomePoint(bp));
            if (_closest > l)
            {
                _closest = l;
                type = GetBiome(bp);
            }
        }
        {
            Vector2Int bp = cbp + new Vector2Int(tw.x, tw.y);
            float l = SMath.V2.Length(pos, GetBiomePoint(bp));
            if (_closest > l)
            {
                _closest = l;
                type = GetBiome(bp);
            }
        }

        return type;
    }

    /// <summary>
    /// get center of biome domain
    /// </summary>
    /// <returns></returns>
    public static Vector2Int GetBiomePoint(Vector2Int biome_position)
    {
        UnityEngine.Random.InitState(Ct.curWd.GetSeed(biome_position.x));
        int x = UnityEngine.Random.Range(0, WorldGenerator.units_of_biome);

        UnityEngine.Random.InitState(Ct.curWd.GetSeed(biome_position.y + 2));
        int y = UnityEngine.Random.Range(0, WorldGenerator.units_of_biome);

        return new(x, y);
    }
    public bool IsBiomePointInChunk(out Vector2 pb)
    {
        Vector2Int bpt = GetBiomePoint(GetBiomePosition());
        Vector2Int bcpt = WorldGenerator.ToChunkOfPos(bpt);
        pb = GetRelaPos(bpt);
        return bcpt == GetCP();
    }
    /// <summary>
    /// Get biome of biome position
    /// </summary>
    /// <param name="bp"></param>
    /// <returns></returns>
    public static int GetBiome(Vector2Int bp)
    {
        int i = SMath.Random(bp.x * 3 - bp.y, WorldGenerator.biomes.Count, 0);
        return i;
    }
    public Vector2 ToRealPos(Vector2 p)
    {
        Vector2 crp = GetCP() * WorldGenerator.units_of_chunk;
        return crp + p;
    }

    public Chunk() { }
    public Chunk(Vector2Int cp)
    {
        chunkx = cp.x;
        chunky = cp.y;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.AppendLine("Chunk: " + GetCP());
        sb.AppendLine("objs number: " + objs.Count);
        if (objs != null)
            foreach (ObjState o in objs)
            {
                sb.AppendLine(o.ToString());
            }

        return sb.ToString();
    }
}


[Serializable]
public class WorldData
{
    public string name = "deafault";
    public string seed = "n";
    public int units_of_biome = 128;
    public int units_of_chunk = 16;
    public int worldMaxGenerateTimeOfChunk = 300;
    public int worldMinGenerateTimeOfChunk = 100;
    /// <summary>
    /// time to try summon a entity
    /// </summary>
    public float summmonTime = 3;
    public int radius_renderChunk = 3;
    public float growingTime = 8;

    public float camAngle = 0f;
    public float camYp = 0.22f, camDist = 3, camDeafDist = 4, camElevatepower = 2;
    public Vector3 CamPos = new();
    public Vector3 plyPos = new();
    public float playerSpeed = 8;

    public Inventory inventory;
    /// <summary>
    /// distance to approach drops of player
    /// </summary>
    public float approacherDistance = 6;
    public float dropsApporachSpeed = 500;
    public float maxDistanceOfInteraction = 5;
    /// <summary>
    /// minimum dianstance of playe to absorb drops
    /// </summary>
    public float absorbDistance = 1;

    public int GetSeed(int[] index)
    {
        int count = 0;
        for (int i = 0; i < index.Length; i++)
        {
            count += GetSeed(index[i]);
        }
        return count;
    }
    public int GetSeed(int index)
    {
        index %= seed.Length;
        return seed[index] / 3 + seed[index];
    }

    public int GetSeed(Vector2Int p)
    {
        int s = seed[p.x % seed.Length] + seed[p.y % seed.Length];
        return s * p.y * p.x + 2 + p.x;
    }
    public void Save()
    {
        string path = Data.worldPath + name + "/setting.json";
        Data.WriteJson(this, path);
        Debug.Log("[WorldData]World data is saved at: " + path);
    }
    /// <summary>
    /// Read the world setting file
    /// </summary>
    /// <param name="name">Input just the name</param>
    /// <returns></returns>
    public static WorldData Load(string name)
    {
        if (!Data.DIrectioryExists(Data.worldPath + name))
            Data.Create(Data.worldPath + name);

        string path = Data.worldPath + name + "/setting.json";
        WorldData data = new();
        if (Data.FileExists(path))
        {
            data = Data.ReadJson<WorldData>(path);
            Debug.Log("[WorldData]Path Loaded: " + path);
        }
        else
            Debug.Log("Path not found:" + path);

        data.inventory ??= new(32);

        Ct.evn.OnGameSave += data.Save;

        return data;
    }

}