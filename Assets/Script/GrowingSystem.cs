using System.Collections;
using UnityEngine;

public static class GrowingSystem
{
    public static IEnumerator Growing()
    {
        Transform objects = GameObject.Find("Objects").transform;
        float time;
        while (true)
        {
            if (Ct.world.loadedChunk.Count == 0)
            {
                yield return new WaitForSeconds(1);
                continue;
            }
            time = Ct.curWd.growingTime / Ct.world.loadedChunk.Count;

            int count = objects.childCount;
            int index = SMath.Random(count, 0);
            if (index != 0)
                Grow(objects.GetChild(index).gameObject);

            yield return new WaitForSeconds(time);
        }
    }
    public static IEnumerator Summoning()
    {
        int r = Ct.curWd.radius_renderChunk;
        while (true)
        {
            yield return null;
            Vector2Int cp = SMath.V2.Random(new Vector2Int(Ct.cp.x + r, Ct.cp.y + r), new Vector2Int(Ct.cp.x - r, Ct.cp.y - r));
            if (!Ct.world.loadedChunk.ContainsKey(cp))
                continue;

            Chunk ch = Ct.world.loadedChunk[cp];
            string type = WorldGenerator.biomes[WorldGenerator.GetBiome(ch.BiomeType(ch.GetRandomPosition()))].GetEntity(SMath.RandomInt());
            if (type != null)
                Summon(WorldGenerator.To3DPos(Ct.world.loadedChunk[cp].GetPositionInChunk()), type);

            yield return new WaitForSeconds(Ct.curWd.summmonTime);
        }
    }
    public static void Grow(GameObject o)
    {
        Obj obj = o.GetComponent<Obj>();
        ObjData od = Obj.GetData(obj.ld.name);

        if (od.growable == null)
            return;
        float p = SMath.Random(100, 0f);
        if (p <= od.growable.possibility)
        {
            Obj.Load(od.growable.nextPhase, o.transform.position);
            Obj.Destroy(o, od.name);
            Debug.Log("summon");
        }
    }
    public static void Summon(Vector3 p, string type)
    {
        Actor.Load(type, p);
    }
}
