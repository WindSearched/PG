using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class Entity : MonoBehaviour
{
    public string type;
    public float speed;
    public int Health
    {
        get => hp;
        set
        {
            hp = value;
            if (hp >= 0)
            {
                Debug.Log("entity destoryed");
                Destroy(gameObject);
            }
        }
    }
    private int hp;
    public Vector3 direction = new();
    public Vector3 position = new();
    public Rigidbody rb;
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        parent = GameObject.Find("Entities").transform;
    }
    protected virtual void Update()
    {
        Move();
    }

    public void Repelling(Vector3 forward, int damage)
    {
        rb.AddForce(forward);
        Health -= damage;
    }
    private void Move()
    {
        if (speed != 0)
        {
            rb.velocity = speed * direction;
        }
        else
            rb.velocity = Vector3.zero;
    }

    public static Transform parent;
    public static List<string> entityTypes = new();
    public static List<EntityData> data = new();
    public static List<GameObject> entities = new();
    public static Dictionary<string, Interact> interactions = new();
    public static void Load(string type, Vector3 pos)
    {   
        if (!entityTypes.Contains(type))
            return;

        int index = entityTypes.IndexOf(type);

        Vector2Int cp = WorldGenerator.ToChunkOfPos(pos);
        Chunk.EntityState es = new()
        {
            name = type
        };
        es.SetPosition(pos);
        Ct.world.loadedChunk[cp].entities.Add(es);

        GameObject g = Instantiate(entities[index], parent);
        g.name = type;
        g.transform.position = pos;
        g.GetComponent<Entity>().type = type;
    }
    public static void Add(string type, EntityData data, GameObject obj)
    {
        entityTypes.Add(type);
        Entity.data.Add(data);
        entities.Add(obj);
    }
    public static void Add(string type, EntityData data, GameObject obj, Type added)
    {
        obj.AddComponent(added);
        Add(type, data, obj);
    }

    public delegate void Interact(Entity en);
}
public class EntityData 
{

}