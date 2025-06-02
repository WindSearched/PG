using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GrowingSystem
{
    public static IEnumerator Growing()
    {
        Transform objects = GameObject.Find("Objects").transform;
        float time = Ct.curWd.growingTime / Ct.world.loadedChunk.Count;
        while (true)
        {
            int count = objects.childCount;
            int index = SMath.Random(count, 0);
            if(index != 0)
                Grow(objects.GetChild(index).gameObject);

            yield return new WaitForSeconds(time);
        }
    }
    public static IEnumerator Summoning()
    {
        int r = Ct.curWd.radius_renderChunk;
        while (true)
        {
            yield return new WaitForSeconds(Ct.curWd.summmonTime);

            Vector2Int cp = SMath.V2.Random(new(Ct.cp.x + r, Ct.cp.y + r), new(Ct.cp.x - r, Ct.cp.y + r));
            if (!Ct.world.loadedChunk.ContainsKey(cp))
                continue;

            //string type = Entity.entityTypes[SMath.Random(Entity.entityTypes.Count, 0)];
            Chunk ch = Ct.world.loadedChunk[cp];
            string type = WorldGenerator.biomes[WorldGenerator.GetBiome(ch.BiomeType(ch.GetRandomPosition()))].GetEntity(SMath.RandomInt());
            Summon(Ct.world.loadedChunk[cp].GetRandomPosition(), type);
        }
    }
    public static void Grow(GameObject o)
    {
        Obj obj = o.GetComponent<Obj>();
        ObjData od = Obj.GetData(obj.ld.name);

        if (od.growable == null)
            return;
        
        if(SMath.Random(100, 0f) <= od.growable.possibility)
        {
            Obj.Load(od.growable.nextPhase, o.transform.position);
            Obj.Destroy(o, od.name);
        }
    }
    public static void Summon(Vector3 rp, string type)
    {
        Vector2Int cp = WorldGenerator.ToChunkOfPos(rp);

        Entity.Load(type, Ct.world.loadedChunk[cp].GetRandomPosition());
    }
}
