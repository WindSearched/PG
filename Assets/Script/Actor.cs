using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Actor : MonoBehaviour
{
    public static Dictionary<string, ActorAction> actions = new();
    public static Dictionary<string, ActorAction> controllers = new();
    public static Dictionary<string, ActItc> interactions = new();
    /// <summary>
    /// inizializzation of every type of actor
    /// </summary>
    public static Dictionary<string, ActItc> starts = new();
    public static Dictionary<string, ActorData> data = new();
    public static List<SpriteManager> sprites = new();
    public static List<string> aTy = new();
    public static List<Actor> actors = new();
    public static Transform parent;
    public static void Add(ActorData ad, string type)
    {
        aTy.Add(type);
        data.Add(type, ad);
    }
    public static void Add(string path)
    {
        var ad = Data.ReadJson<ActorData>(path);
        Add(ad, ad.name);
    }
    public static void Add(string path, string type)
    {
        Add(Data.ReadJson<ActorData>(path), type);
    }
    public static GameObject Load(string type, Vector3 pos, bool regist = true)
    {
        ActorState st = new()
        {
            name = type,
            position = V3.Get(pos)
        };
        return Load(st, regist);
    }
    public static GameObject Load(ActorState state, bool regist = true)
    {
        Ct.ct.CT(LoadCt(state, regist));
        return loadedAct;
    }
    public static GameObject loadedAct;
    public static System.Collections.IEnumerator LoadCt(ActorState state, bool regist = true)
    {
        if (parent == null)
            parent = GameObject.Find("Actors").transform;
        var o = Resources.Load("actor") as GameObject;
        var go = loadedAct = Instantiate(o, parent);
        var act = go.GetComponent<Actor>();
        act.state = state;
        var cp = act.cp = WorldGenerator.ToChunkOfPos(state.position.ToVec());
        if (regist)
        {
            if (!Ct.world.loadedChunk.ContainsKey(cp))
            {
                yield return Ct.ct.CT(Ct.world.ChunkManager(cp));
                Ct.world.loadedChunk.Add(cp, Ct.world.managingChunk);
            }
            Debug.Log($"[actinit] {Ct.world.loadedChunk[act.cp].actors.Count}");
            Ct.world.loadedChunk[act.cp].actors.Add(state);
            Debug.Log($"[actend] {Ct.world.loadedChunk[act.cp].actors.Count}");
        }
    }
    public static ActorData GetData(string type)
    {
        if (data.ContainsKey(type))
            return data[type];
        else
            return null;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key">if is not present in the dict. retun the zero action</param>
    public static ActorAction ReadAction(string key)
    {
        if (actions.ContainsKey(key))
            return actions[key];
        else
            return actions.Values.ToArray()[0];
    }
    public static void InitActions()
    {
        controllers.Add(nameof(DefaultActions.RandomChooseController), DefaultActions.RandomChooseController);

        actions.Add(nameof(DefaultActions.RandomMoveRestricted), DefaultActions.RandomMoveRestricted);
    }



    public bool inAction = false;
    public Coroutine action;
    public ActorData Actdata
    {
        get
        {
            return data[type];
        }
    }
    public ActorData dat;
    public ActorState state;

    public Collider col;
    /// <summary>
    /// is the child component
    /// </summary>
    public SpriteRenderer spr;
    /// <summary>
    /// child transform, where is the sprite component
    /// </summary>
    public Transform spt;
    public Rigidbody rig;
    /// <summary>
    /// index of type of actor
    /// </summary>
    public int index;
    public string type = "";
    public SpriteManager Sprmanage
    {
        get
        {
            if (index != -1)
                return sprites[index];
            return null;
        }
    }
    public Coroutine animCor, actionCor, ctrlCor;
    public SpriteManager.Compare animDt;
    public Vector2Int cp;

    public Methd OnCollision, OnTrgger;
    private void Start()
    {
        type = state.name;
        transform.position = state.position.ToVec();
        if (type == "")
            type = aTy[index];
        dat = Actdata;
        actors.Add(this);

        if (state == null)
            Destroy(this);
        state.curAction = Sprmanage.initAnimation;
        ToAnimate();
        transform.position = state.position.ToVec();
        AddCollider(Actdata.collider);

        Ct.evn.WhenVisionRotating += OnVisionRotating;
        Ct.evn.WhenVisionElevate += () => spt.rotation = Obj.facing;

        starts[type].Invoke(dat, this);
        ctrlCor = Ct.ct.CT(controllers[dat.controller].Invoke(dat, this));

        spt.rotation = Obj.facing;
    }
    private void Update()
    {
        ChunkChange();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Plane"))
            return;

        OnCollision?.Invoke(collision.gameObject);
        OnCollision = null;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Plane"))
            return;

        OnTrgger?.Invoke(other.gameObject);
        OnTrgger = null;
    }
    private void OnVisionRotating()
    {
        spt.rotation = Obj.facing;
        ToAnimate();
    }
    public void ToAnimate()
    {
        if (state.curAction == "#stop")
        {
            Ct.ct.Cta(animCor);
        }
        else
        {
            var sm = Sprmanage;
            sm.ToAnimate(state.curAction, state.iniangle, spr, ref animCor, ref animDt);
        }
    }
    public void AddCollider(ActorData.Collider collider)
    {
        if (collider == null)
            return;
        switch (collider.type)
        {
            case ActorData.ColliderType.box:
                BoxCollider c = gameObject.AddComponent<BoxCollider>();
                c.size = collider.size.ToVec();
                c.center = collider.center.ToVec();
                break;
            case ActorData.ColliderType.capsule:
                CapsuleCollider b = gameObject.AddComponent<CapsuleCollider>();
                b.radius = collider.size.x;
                b.height = collider.size.y;
                b.center = collider.center.ToVec();
                break;
            case ActorData.ColliderType.sphere:
                SphereCollider a = gameObject.AddComponent<SphereCollider>();
                a.radius = collider.size.x;
                a.center = collider.center.ToVec();
                break;
        }
    }
    public void Move(Vector3 direction)
    {
        rig.velocity = dat.speed * direction;
        if (dat.canFlip)
            spr.flipX = SMath.V3.DirectionAdjustment(direction, Ct.curWd.camAngle).x > 0;
    }
    public void ChunkChange()
    {
        if (Ct.world.loadedChunk.Count != 0)
        {
            var curp = WorldGenerator.ToChunkOfPos(transform.position);
            if (!Ct.world.loadedChunk.ContainsKey(curp))
            {
                return;
            }

            if (curp != cp && Ct.world.loadedChunk.Count != 0)
            {
                Ct.world.loadedChunk[cp].actors.Remove(state);
                Debug.Log($"[actinit] {Ct.world.loadedChunk[curp].actors.Count}");
                Ct.world.loadedChunk[curp].actors.Add(state);
                Debug.Log($"[actinit] {Ct.world.loadedChunk[curp].actors.Count}");
                cp = curp;
            }
            if (curp != cp && WorldGenerator.actorInChunk[cp].Count != 0)
            {
                WorldGenerator.actorInChunk[cp].Remove(gameObject);
                WorldGenerator.actorInChunk[curp].Add(gameObject);
            }
            cp = curp;
        }
    }
    private void OnDestroy()
    {
        if (!Ct.quit)
        {
            Debug.Log("OnDestroy");
            Ct.evn.WhenVisionRotating -= OnVisionRotating;
            Ct.evn.WhenVisionElevate -= () => spt.rotation = Obj.facing;
            Ct.ct.Cta(animCor);
            Ct.ct.Cta(actionCor);
            Ct.ct.Cta(ctrlCor);
            Ct.world.loadedChunk[cp].actors.Remove(state);
        }
    }
}
[Serializable]
public class ActorData
{
    public string name;
    public List<string> actions;
    public string controller;

    public float interval, possibility;
    public Collider collider;
    public float triggerRadius;
    public bool canFlip = true, useAnimator = false;
    public float livetime;

    public float speed = 1, life;
    public class Collider
    {
        public ColliderType type;
        public V3 center = new();
        /// <summary>
        /// if it is the capsule type x is radius, y is height; if it is sphere just x is radius
        /// </summary>
        public V3 size = new();
    }
    public enum ColliderType
    {
        box,
        capsule,
        sphere
    }
}
[Serializable]
[MessagePackObject]
public class ActorState
{
    [Key(0)] public string name = "";
    [Key(1)] public Dictionary<string, object> states = new();
    [Key(2)] public float livetime;
    [Key(3)] public V3 position;
    [Key(4)] public float iniangle;
    [Key(5)] public string curAction;

    public ActorState() { }
}
public static class DefaultActions
{
    /// <summary>
    /// random choose the action of actor, waits of a inverval, if the action is still executing stop the action and eexecutes a new antion
    /// stop when the livetime overtake the defualt livetime
    /// </summary>
    public static System.Collections.IEnumerator RandomChooseController(ActorData data, Actor ac)
    {
        float time = 0;
        while (ac != null)
        {
            if (SMath.Random(100, 0) < data.possibility)
            {
                int ind = SMath.Random(data.actions.Count, 0);
                string actName = data.actions[ind];
                var act = Actor.ReadAction(actName);

                if (ac.inAction)
                    Stop(ac.action);
                ac.actionCor = Start(act.Invoke(data, ac));
                ac.inAction = true;
            }
            yield return new WaitForSeconds(data.interval);
            time += data.interval;
            if (time > data.livetime)
            {
                UnityEngine.Object.Destroy(ac.gameObject);
                ac.state.curAction = "#stop";
                ac.ToAnimate();
                yield break;
            }
        }
    }
    /// <summary>
    /// random choose a direction and active the walk animation
    /// </summary>

    //public static System.Collections.IEnumerator RandomDirectionMove(ActorData data, Actor actor)
    //{
    //    Vector2 dir = SMath.V2.Random(1, -1);
    //    actor.Move(WorldGenerator.To3DPos(dir).normalized);

    //    actor.OnCollision += (_) =>
    //    {
    //        actor.Move(new());
    //        actor.state.curAction = "wait";
    //        actor.ToAnimate();
    //    };
    //    actor.state.curAction = "walk";
    //    actor.ToAnimate();
    //    yield return null;
    //}
    public static System.Collections.IEnumerator RandomMoveRestricted(ActorData data, Actor actor)
    {
        yield return null;

        bool loop = true;
        Vector3 dir = WorldGenerator.To3DPos(SMath.V2.Random(1, -1)).normalized;
        int restr = Ct.curWd.radius_renderChunk - 1;//restrict area

        actor.state.curAction = "walk";
        actor.ToAnimate();

        actor.OnCollision += (_) =>
        {
            actor.Move(new());
            actor.state.curAction = "wait";
            actor.ToAnimate();
            loop = false;
        };
        Debug.Log(actor.actionCor != null);

        while (actor.actionCor != null && loop)
        {
            actor.Move(dir);
            actor.state.position.FromVec(actor.transform.position);
            if ((actor.cp - Ct.cp).magnitude >= restr)
            {
                dir = SMath.V3.GetVector(SMath.V2.RandomByDirection(-dir, 60));
                actor.Move(dir);
                yield return new WaitForSeconds(1);
            }
            else
                yield return null;
        }

    }
    public static Coroutine Start(System.Collections.IEnumerator enumerator) => Ct.ct.CT(enumerator);
    public static void Stop(Coroutine coroutine) => Ct.ct.Cta(coroutine);
    public static float interval = 0.05f;
}
/// <summary>
/// 
/// </summary>
/// <param name="data"></param>
/// <param name="index">is the index of acter to control</param>
/// <returns></returns>
public delegate System.Collections.IEnumerator ActorAction(ActorData data, Actor actor);
public delegate void Methd(GameObject collided);
public delegate void ActItc(ActorData data, Actor actor);