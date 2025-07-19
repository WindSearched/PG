using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using IPGModAPI;
using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.Networking;

public class Ct : MonoBehaviour
{
    public GameObject player;
    public GameObject cameraO;
    public bool inTrigger = false;
    public GameObject inventory;
    public GameObject commandPage;
    public InventoryPage invp;
    public Material nonmal;
    public Material board;
    public static Transform canvas;
    public static RectTransform canvasrt;
    public Transform objects;
    public Transform destroyedObjects;
    public Transform chestView;
    public TextMeshProUGUI pointedName;
    public TextMeshProUGUI pointedDescription;
    public VirtualJoystick joystick;
    public VirtualJoystick indicatorstick;
    public Indicator indicator;
    public List<Sprite> indicates;
    public Dictionary<string, Sprite> sprites;
    public string PointedName
    {
        set
        {
            pointedName.text = value;
        }
    }
    public string PointedDescription
    {
        set
        {
            pointedDescription.text = value;
        }
    }

    public static Inventory selectContainerInv;

    public MouseState left = MouseState.relased;
    public MouseState right = MouseState.relased;
    public MouseState ocped;//occuped mouse state 
    public int toward;//toward of mouse changing
    public bool specifyInter = false;
    /// <summary>
    /// is themouse position
    /// </summary>
    public Vector2 mP;
    /// <summary>
    /// is the player position
    /// </summary>
    public static Vector2 pp;
    public static Vector3 ppw;
    /// <summary>
    /// distance from plater pos and world mouse pos
    /// </summary>
    public static float dmp;
    /// <summary>
    /// the player loaction's chunk pos
    /// </summary>
    public static Vector2Int cp;
    /// <summary>
    /// mouse position in the world
    /// </summary>
    public static Vector3 wmp;
    public static float wmp_a;

    public static Ct ct;
    public static Ac act;
    public static List<InputAction> addActions;
    public static CEvent evn = new();
    /// <summary>
    /// The current world data
    /// </summary>
    public static WorldData curWd;
    public static Setting set = new();
    public static DePa dePa;
    public static Cam cam;
    public static World world;
    public static CommandPage command;

    public static List<RectTransform> scalableui = new();
    public static List<Vector2> realPositions = new();
    public static bool attackingMode = false;
    public static float scale;
    public static MouseSelect mouseSelected;
    public static LineRenderer attackViewer;
    /// <summary>
    /// prelload obj
    /// </summary>
    public static PreloadObj po;
    public static FadeUIManager fadeUIManager;
    public static MainCanvs mcanvas;
    public static bool visionElevate = false, visionRotate = false;
    public static bool quit = false;
    private void Start()
    {
        act.Main.leftM.performed +=
            c =>
            {
                if (Page.IsPage("main"))
                    MouseDet(c, out left);
            };
        act.Main.rightM.performed +=
            c =>
            {
                if (Page.IsPage("main"))
                    MouseDet(c, out right);
            };
        act.Main.leftM.canceled +=
            c => left = MouseState.relased;
        act.Main.rightM.canceled +=
            c => right = MouseState.relased;


        act.Main.CommandPage.performed += c =>
        {
            if (commandPage.activeInHierarchy)
                Page.ChangePage("main");
            else
                Page.ChangePage("command");
        };
        act.Main.esc.performed += c =>
        {
            if (!Page.IsPage("main"))
                Page.ChangePage("main");
            else
                Page.ChangePage("esc");
        };

        ocped = left;
        Setting.Load(out set);
        curWd = WorldData.Load(set.preWorld);

        cam = cameraO.GetComponent<Cam>();
        canvas = GameObject.Find("Canvas").transform;
        canvasrt = canvas.GetComponent<RectTransform>();
        inventory.GetComponent<InventoryPage>().Binding();
        attackViewer = player.GetComponent<LineRenderer>();
        commandPage.GetComponent<CommandPage>().Starte();

        GetScale(canvas.GetComponent<RectTransform>().sizeDelta);

        Page.Add("main", () => { }, () => { });
        Page.Add("command", () => { commandPage.SetActive(true); }, () => { commandPage.SetActive(false); });
        Page.curPage = "main";

        if(!Mod.modLoaded)
            Mod.LoadMods();
        Mod.LoadModsInWorld();

        Item.InctInitializzation();
        Obj.LoadDefualtInteractions();
        Actor.InitActions();

        evn.BeforeGameSave += () => { curWd.plyPos = player.transform.position; };

        invp.SStart();
        NoteManager.Init(canvas.Find("notes"));

#if UNITY_ANDROID
        joystick.gameObject.SetActive(true);
        indicatorstick.gameObject.SetActive(true);
#endif

        OnCastIn += () =>
        {
            casted.GetComponentInChildren<SpriteRenderer>().material = board;
            casted.GetComponentInChildren<SpriteRenderer>().material.SetFloat("_lineWidth", 0.5f);

            indicator.Indicate = "breakable";
        };
        OnCastOut += () =>
        {
            precasted.GetComponentInChildren<SpriteRenderer>().material = nonmal;

            indicator.Indicate = "";
        };

        indicatorstick.OnInputing += () =>
        {
            indicator.rt.anchoredPosition += Time.deltaTime * set.indicatorJoystickVelocity * indicatorstick.InputVector;


            // 限制在 Canvas 范围内
            Vector2 clampedPos = indicator.rt.anchoredPosition;
            clampedPos.x = Mathf.Clamp(clampedPos.x, -canvasrt.rect.width / 2, canvasrt.rect.width / 2);
            clampedPos.y = Mathf.Clamp(clampedPos.y, -canvasrt.rect.height / 2, canvasrt.rect.height / 2);
            indicator.rt.anchoredPosition = clampedPos;

            foreach (var button in ButtonMouseHandler.MouseHandlers)
            {
                button.CheckPointer(indicator.rt.position);
            }
        };
        //
        //finish preload
        ///
        MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard
       .WithResolver(CompositeResolver.Create(
        UnityResolver.Instance,
        StandardResolver.Instance
        ));
    }
    private void Update()
    {
        mP = act.Main.mousePos.ReadValue<Vector2>();


        MouseOcped();
        toward = MouseToward();

        if (visionRotate) evn.IWhenVisionRotating();
        if (visionElevate) evn.IWhenVisionElevate();
        evn.IWhenUpdate();
    }
    private void FixedUpdate()
    {
        RayCast();
        RayPos();
        ppw = transform.position;
        if (casted != null)
            dmp = (casted.transform.position - ppw).magnitude;
    }
    private void Awake()
    {
        act = new();
        ct = this;

    }
    private void OnEnable()
    {
        act.Enable();
    }
    private void OnDisable()
    {
        act.Disable();
    }
    private void OnApplicationQuit()
    {
        quit = true;
        if (set.quitSave)
        {
            evn.IBeforeGameSave();
            evn.IOnGameSave();
            world.Saving();
        }
    }

    public static void DestroyAll(Transform parent)
    {
        while (parent.childCount > 0)
        {
            Transform child = parent.GetChild(0);

            if (child.TryGetComponent<RectTransform>(out var rectTransform))
            {
                rectTransform.SetParent(ct.destroyedObjects, false);
            }
            else
            {
                child.SetParent(ct.destroyedObjects);
            }

            Destroy(child.gameObject);
        }
    }
    public void RayCast()
    {
        if (inTrigger)
            return;

        Ray ray = Camera.main.ScreenPointToRay(indicator.position);
        precasted = casted;

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
        {
            casted = hit.collider.gameObject;
        }
        else
        {
            casted = null;
        }

        if (precasted != casted)
        {
            // 射线进入一个新的非Plane物体
            if (casted != null && !casted.CompareTag("Plane"))
            {
                cast = true;
                OnCastIn?.Invoke();
            }
            // 射线离开非Plane物体()
            if (precasted != null && !precasted.CompareTag("Plane"))
            {
                OnCastOut?.Invoke();
                cast = false;
            }
        }

    }

    /// <summary>
    /// is object under the cutsor, if placing become triggered
    /// </summary>
    public GameObject casted, precasted;
    public event dPGM OnCastIn, OnCastOut;
    public bool cast;
    public void RayPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(mP);
        float y = 0;

        if (ray.direction.y != 0)
        {
            float t = (y - ray.origin.y) / ray.direction.y;
            if (t >= 0)
            {
                wmp = ray.GetPoint(t);
                return;
            }
        }
        wmp = deafv;
    }
    public static Vector3 deafv = new(float.MaxValue, float.MinValue);
    public void MouseDet(InputAction.CallbackContext cxt, out MouseState state)
    {
        if (cxt.interaction is MultiTapInteraction)
            state = MouseState.tap2;
        //left = MultiTapDet(cxt);
        else if (cxt.interaction is HoldInteraction)
        {
            state = MouseState.hold;
        }
        else
            state = MouseState.relased;

        Debug.Log($"[Mouse detected] {state}");
    }
    public MouseState MultiTapDet(InputAction.CallbackContext cxt)
    {
        int count = cxt.interaction is MultiTapInteraction multiTapInteraction
            ? multiTapInteraction.tapCount
            : 0;

        return count switch
        {
            //3 => MouseState.tap3,
            2 => MouseState.tap2,
            _ => MouseState.error
        };
    }
    public void MouseOcped()
    {
        if (!specifyInter)
        {
            ocped = MouseState.relased;
            return;
        }
        if (left != MouseState.relased)
            ocped = left;
        else if (right != MouseState.relased)
            ocped = right;
        else
            ocped = MouseState.relased;
    }
    public void ChangeState(MouseState state)
    {
        switch (toward)
        {
            case 1:
                left = state;
                break;
            case -1:
                right = state;
                break;
        }
        ocped = state;
    }
    public int MouseToward()
    {
        if (left != MouseState.relased)
            return 1;
        else if (right != MouseState.relased)
            return -1;
        else return 0;
    }

    /// <summary>
    /// Start couroutine
    /// </summary>
    /// <param name="e"></param>
    public Coroutine CT(IEnumerator e)
    {
        return StartCoroutine(e);
    }
    public void Cta(Coroutine cr)
    {
        if (cr != null)
            StopCoroutine(cr);
    }
    public static void GetScale(Vector2 size)
    {
        Vector2 deaf = new(600, 400);
        Vector2 rela = size / deaf;
        if (rela.x < rela.y)
            scale = rela.x;
        else
            scale = rela.y;
    }
    /// <summary>
    /// the rect has must a ui in the real position(300*200)
    /// </summary>
    /// <param name="rect"></param>
    public static void AddScalable(RectTransform rect)
    {
        Vector2 realPosition = rect.localPosition;
        ToScale(rect, realPosition);
        scalableui.Add(rect);
        realPositions.Add(realPosition);
    }
    public static void ToScale(RectTransform rect, Vector2 realPosition)
    {
        rect.localScale = new(scale, scale);
        rect.localPosition = realPosition * scale;
    }
    public static void ToScale(RectTransform rect)
    {
        rect.localScale = new(scale, scale);
    }
    public static void ToScale(Vector2 realPosition, RectTransform rect)
    {
        rect.localPosition = realPosition * scale;
    }
    /// <summary>
    /// get the data of raycasted object
    /// </summary>
    public static ObjData GetObjData()
    {
        try
        {
            return Obj.GetData(ct.casted.GetComponent<Obj>().ld.name);
        }
        catch
        {
            return null;
        }
    }
}

public class CEvent
{
    public delegate void Method();

    public event Method WhenVisionRotating;
    public event Method WhenVisionElevate;
    public event Method WhenUpdate;
    public event Method WhenPlayerMoving;
    public event Method InMouseMoving;

    public event Method OnGameSave;
    public event Method BeforeGameSave;
    public void Invoke(Method method)
        => method?.Invoke();
    public void IWhenVisionRotating()
    {
        WhenVisionRotating?.Invoke();
    }
    public void IWhenUpdate()
    {
        WhenUpdate?.Invoke();
    }
    public void IWhenVisionElevate()
    {
        WhenVisionElevate?.Invoke();
    }
    public void IOnGameSave()
    {
        OnGameSave?.Invoke();
    }
    public void IBeforeGameSave()
    {
        BeforeGameSave?.Invoke();
    }
    public void IWhenPlayerMoving()
    {
        WhenPlayerMoving?.Invoke();
    }
    public void IInMouseMoving() => InMouseMoving?.Invoke();
}


public static class SMath
{
    public static float Angle(Vector3 dir)
    {
        return Vector3.SignedAngle(Vector3.right, dir, Vector3.down);
    }
    public static float Angle(Vector2 dir)
    {
        Vector3 v = dir;
        return Angle(v);
    }
    public static float AngleStandardization(float angle)
    {
        angle %= 360;
        if (angle < 0)
            angle += 360;
        return angle;
    }
    public static float Smooth(float x)
    {
        x *= degRad;
        return math.sin(x);
    }
    public static float Smooth(float timeMax, float time)
    {
        float t = time / timeMax * 90 * degRad;
        return Sin(t);
    }
    public static float Parabola(float x, float p)
        => math.pow(x, p);
    public static float Abs(float v)
        => Mathf.Abs(v);
    public static int Abs(int v) => Mathf.Abs(v);

    public static float degRad = Mathf.Deg2Rad;

    public static float pi = math.PI;
    public static float Cos(float x)
        => math.cos(x);
    public static float CosA(float angle)
    {
        angle *= degRad;
        return math.cos(angle);
    }
    public static float Sin(float x)
        => math.sin(x);
    public static float SinA(float angle)
    {
        angle *= degRad;
        return math.sin(angle);
    }
    public static int Random(int seed, int max, int min)
    {
        UnityEngine.Random.InitState(seed);
        return UnityEngine.Random.Range(min, max);
    }
    public static float Random(int seed, float max, float min)
    {
        UnityEngine.Random.InitState(seed);
        return UnityEngine.Random.Range(min, max);
    }
    public static float Random(float max, float min)
    {
        return UnityEngine.Random.Range(min, max);
    }
    public static int Random(int max, int min)
    {
        return UnityEngine.Random.Range(min, max);
    }
    public static bool Random()
    {
        return Random(1, 0) == 0;
    }
    public static int RandomInt()
    {
        return Random(int.MaxValue, int.MinValue);
    }
    public static int Floor(float var)
    {
        return (int)math.floor(var);
    }
    /// <summary>
    /// get vec2 from angle
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static Vector2 GetVector(float angle) => new(CosA(angle), SinA(angle));
    public static class V3
    {
        /// <summary>
        /// around parallele by plane xz
        /// </summary>
        public static Vector3 ParaAround(Vector3 center, float angle, float radius)
        {
            angle *= degRad;
            Vector3 rela = new Vector3(Cos(angle), 0, Sin(angle)) * radius;

            return center + rela;
        }
        public static float Length(Vector3 to, Vector3 from)
        {
            Vector3 r = to - from;
            return r.magnitude;
        }
        public static Vector3 GetVector(float x = 0, float y = 0, float z = 0)
            => new(x, y, z);
        /// <summary>
        /// get a plan position
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Vector3 GetVector(Vector2 vec, float height = 0) => new(vec.x, height, vec.y);
        public static Vector3 Parse(string p)
        {
            try
            {
                p = p.TrimStart('{');
                p = p.TrimEnd('}');
                string[] s = p.Split(',');
                return new(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }
            catch
            {
                return Vector3.zero;
            }
        }

        public static Vector3 DirectionAdjustment(Vector3 dir, float angle)
        {
            float b = Angle(dir);
            float r = b - 90 + angle;

            return GetVector(SMath.GetVector(r)) * dir.magnitude;
        }
    }
    public static class V2
    {
        public static Vector2Int Floor(Vector2 position)
        {
            return new(SMath.Floor(position.x), SMath.Floor(position.y));
        }
        public static float Length(Vector2 from, Vector2 to)
        {
            Vector2 v = from - to;
            return v.magnitude;
        }
        public static Vector2Int Random(Vector2Int max, Vector2Int min)
        {
            return new(SMath.Random(max.x, min.x), SMath.Random(max.y, min.y));
        }
        public static Vector2 Random(float max, float min)
        {
            return new(SMath.Random(max, min), SMath.Random(max, min));
        }
        public static Vector2 RandomByDirection(float dirangle, float angleArea)
        {
            float a = angleArea / 2;
            float b = SMath.Random(a, -a);
            float c = dirangle + b;
            return GetVector(c);
        }
        public static Vector2 RandomByDirection(Vector2 dir, float dirangle)
        {
            float a = Angle(dir);
            return RandomByDirection(a, dirangle);
        }
    }
    public static class Spr
    {
        public static int pxPerUnit = 32;
        public static Vector2Int GetDistance(Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            Color co = new();
            Vector2Int v = new();
            for (int x = 0; x < 32; x++)
            {
                bool found = false;
                for (int i = 0; i < 32; i++)
                {
                    if (tex.GetPixel(x, i) != co)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    v.x = x + 1;
                    break;
                }
            }
            for (int y = 0; y < 32; y++)
            {
                bool found = false;
                for (int i = 0; i < 32; i++)
                {
                    if (tex.GetPixel(i, y) != co)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    v.y = y + 1;
                    break;
                }
            }
            return v;
        }
        /// <summary>
        /// Get area of opaque pixels
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public static Rect GetValidPixels(Sprite sprite)
        {
            return GetValidPixels(sprite.texture, sprite.rect);
        }
        public static Rect GetValidPixels(Texture2D texture, Rect spriteRect)
        {
            //get sprite area
            int startX = (int)spriteRect.x;
            int startY = (int)spriteRect.y;
            int width = (int)spriteRect.width;
            int height = (int)spriteRect.height;

            int minX = width, maxX = 0, minY = height, maxY = 0;
            bool hasOpaquePixel = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = texture.GetPixel(startX + x, startY + y);

                    if (pixel.a > 0) //check just opaque pixel
                    {
                        hasOpaquePixel = true;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (hasOpaquePixel)
            {
                Debug.Log($"[SMath.Spr]Area of opaque px: minX={minX}, maxX={maxX}, minY={minY}, maxY={maxY}");
            }
            else
            {
                Debug.Log("[SMath.Spr]Has not opaque area!!");
            }

            return new(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

    }
}
public enum MouseState
{
    hold,
    tap2,
    relased,
    error,
}

[Serializable]
public class Setting
{
    public string preWorld = "deafault";
    public bool quitSave = true;
    public string language = "en";
    public Color startSceneBackGround = Color.black;
    /// <summary>
    /// color of preloadobj when the item can placed
    /// </summary>
    public Color objPlaceable = new(0, 1, 0, 0.2f);
    /// <summary>
    /// color of preloadobj when the item cannot placed
    /// </summary>
    public Color objCannotPlace = new(1, 0, 0, 0.2f);
    public float indicatorJoystickVelocity = 160;
    /// <summary>
    /// max distance to do not move
    /// </summary>
    public float indicatorJoystickDistance = 0.4f;
    public void Save()
    {
        Data.WriteJson(this, Data.setting);
        Debug.Log("[Setting]Game setting is saved at: " + Data.setting);
    }
    public static void Load(out Setting set)
    {
        if (Data.FileExists(Data.setting))
            set = Data.ReadJson<Setting>(Data.setting);
        else
            set = new();
        Ct.evn.OnGameSave += set.Save;
    }
}
public static class Data
{
    /// <summary>
    /// thisis the worlds data path
    /// </summary>
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    /// <summary>
    /// thisis the worlds data path
    /// </summary>
    public static string worldPath = Application.streamingAssetsPath + "/world/";
    /// <summary>
    /// this is the setting dataa path
    /// </summary>
    public static string setting = Application.streamingAssetsPath + "/setting.json";
#else
    /// <summary>
    /// thisis the worlds data path
    /// </summary>
    public static string worldPath = Application.persistentDataPath + "/world/";
    /// <summary>
    /// this is the setting dataa path
    /// </summary>
    public static string setting = Application.persistentDataPath + "/setting.json";
#endif
    public static void WriteJson<T>(T data, string path, Formatting formatting = Formatting.Indented)
    {

        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new DefaultContractResolver
            {
                // 忽略只读属性（比如 normalized）
                IgnoreSerializableInterface = true
            }
        };
        string json = JsonConvert.SerializeObject(data, formatting, settings);
        string sp = string.Empty;
        using StreamWriter sw = new(sp + path);
        sw.WriteLine(json);
        sw.Close();
    }
    public static string GetJson<T>(T data)
    {
        return JsonConvert.SerializeObject(data);
    }
    public static T SetJson<T>(string data)
    {
        return JsonConvert.DeserializeObject<T>(data);
    }
    public static T ReadJson<T>(string path)
    {
        if (!FileExists(path))
            return default;

        string json = ReadTextFile(path);

        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch
        {
            Debug.Log("errorrr");

            return default;
        }
    }
    public static T ConvertFromJson<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
    public static void SetPropertyPath(object target, string path, object value)
    {
        var parts = path.Split('.');
        object current = target;
        Type currentType = target.GetType();

        for (int i = 0; i < parts.Length; i++)
        {
            string name = parts[i];
            bool isLast = (i == parts.Length - 1);

            FieldInfo field = currentType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new($"字段 '{name}' 不存在于类型 {currentType.Name}");
            if (isLast)
            {
                object converted = ConvertValueIfNeeded(value, field.FieldType);
                field.SetValue(current, converted);
            }
            else
            {
                object next = field.GetValue(current);
                if (next == null)
                {
                    // 初始化嵌套字段（只在中间层）
                    next = Activator.CreateInstance(field.FieldType);
                    field.SetValue(current, next);
                }

                current = next;
                currentType = field.FieldType;
            }
        }
    }
    public static object ConvertValueIfNeeded(object value, Type targetType)
    {
        if (value == null) return null;
        Type valueType = value.GetType();

        if (targetType.IsAssignableFrom(valueType))
            return value;

        if (targetType.IsEnum && value is string s)
            return Enum.Parse(targetType, s);

        if (valueType == typeof(long) && targetType == typeof(int))
            return Convert.ToInt32(value);

        if (valueType == typeof(double) && targetType == typeof(float))
            return Convert.ToSingle(value);

        return Convert.ChangeType(value, targetType);
    }
    //BinaryFormatter formatter = new();
    //using FileStream stream = new(path, FileMode.Create);
    //formatter.Serialize(stream, data);
    public static void WriteBinary<T>(T data, string path)
    {
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                TypelessObjectResolver.Instance,
                UnityResolver.Instance,
                StandardResolver.Instance
            )
        );
        byte[] bytes = MessagePackSerializer.Serialize(data, options);
        File.WriteAllBytes(path, bytes);
    }
    public static T ReadBinary<T>(string path)
    {
        if (!FileExists(path))
            return default;
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                TypelessObjectResolver.Instance,
                UnityResolver.Instance,
                StandardResolver.Instance
            )
        );
        //BinaryFormatter formatter = new();
        //using FileStream stream = new(path, FileMode.Open);
        //return (T)formatter.Deserialize(stream);

        byte[] bytes = File.ReadAllBytes(path);
        return MessagePackSerializer.Deserialize<T>(bytes, options);
    }
    public static string ReadTextFile(string filePath)
    {
        string result = "";

        // 判断路径是否包含 "://" 或 ":///"，以确定是否在 Android 或网络环境中
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            // Android 或 Web 环境，使用 UnityWebRequest 读取文件
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            www.SendWebRequest();

            // 等待请求完成
            while (!www.isDone) { }

            if (www.result == UnityWebRequest.Result.Success)
            {
                result = www.downloadHandler.text;
            }
            else
            {
                Debug.LogError("Error reading file: " + www.error);
            }
        }
        else
        {
            // 其他平台，如 Windows，直接读取文件
            result = File.ReadAllText(filePath);
        }

        return result;
    }
    public static string LoadFile(string path)
    {
        if (!FileExists(path))
            return null;

        return File.ReadAllText(path);
    }

    public static bool DIrectioryExists(string path)
    {
        return Directory.Exists(path.TrimEnd('/'));
    }
    public static bool FileExists(string path)
    {
        return File.Exists(path.TrimEnd('/'));
    }
    /// <summary>
    /// create a directory
    /// </summary>
    /// <param name="path"></param>
    public static void Create(string path)
    {
        Directory.CreateDirectory(path);
    }
    public static FileInfo[] GetFiles(string directory)
    {
        DirectoryInfo di = new(directory);
        return di.GetFiles();
    }
    public static DirectoryInfo[] GetDirectories(string directory)
    {
        DirectoryInfo di = new(directory);
        return di.GetDirectories();
    }
    public static void CopyAll(string source, string dest)
    {
        if (!DIrectioryExists(source))
            return;
        if (!DIrectioryExists(dest))
            Create(dest);

        DirectoryInfo di = new(source);

        foreach (FileInfo fi in di.GetFiles())
        {
            fi.CopyTo(dest + "/" + fi.Name, true);
        }

        foreach (DirectoryInfo d in di.GetDirectories())
        {
            CopyAll(d.FullName, dest + "/" + d.Name);
        }

    }
    public static void Delete(string path)
    {
        if (DIrectioryExists(path))
            Directory.Delete(path, true);
        else if (FileExists(path))
            File.Delete(path);
    }
    /// <summary>
    /// unzip a zip file
    /// </summary>
    /// <param name="to">path of unziped files</param>
    public static void Uncompression(string from, string to)
    {
        if (!File.Exists(from))
        {
            Debug.LogError("ZIP 文件不存在！");
            return;
        }

        FileStream fs = File.OpenRead(from);
        ZipFile zipFile = new(fs);

        foreach (ZipEntry entry in zipFile)
        {
            if (!entry.IsFile)
                continue; // 跳过文件夹

            string entryFileName = entry.Name;

            // 清理非法路径
            string fullPath = Path.Combine(to, entryFileName);

            // 替换所有反斜杠，保证在 Android 上正确
            fullPath = fullPath.Replace("\\", "/");

            string directoryName = Path.GetDirectoryName(fullPath);

            // 安全路径检查
            if (string.IsNullOrEmpty(directoryName))
            {
                Debug.LogWarning("路径为空，跳过: " + entryFileName);
            }
            else
            {
                try
                {
                    if (!Directory.Exists(directoryName))
                        Directory.CreateDirectory(directoryName);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"创建目录失败: {directoryName}, 错误: {ex.Message}");
                    NoteManager.Load("error");
                }
            }

            byte[] buffer = new byte[4096]; // 4KB 缓冲区

            using (Stream zipStream = zipFile.GetInputStream(entry))
            using (FileStream streamWriter = File.Create(fullPath))
            {
                StreamUtils.Copy(zipStream, streamWriter, buffer);
            }
        }

        zipFile.Close();
        fs.Close();
    }
    public static async void UncompressionFromApk(string path, string to)
    {
        await CopyFile(path);
        Uncompression(Application.persistentDataPath + "/" + path, Application.persistentDataPath);
    }
    /// <summary>
    /// copy file in streamingassetspath to pesdentdatapath
    /// </summary>
    /// <param name="path"></param>
    public static async Task CopyFile(string path)
    {
        string sourcePath = Path.Combine(Application.streamingAssetsPath, path);
        string targetPath = Path.Combine(Application.persistentDataPath, path);

        byte[] fileData = await LoadFileAsync(sourcePath);

        if (fileData != null)
        {
            File.WriteAllBytes(targetPath, fileData);
            Debug.Log($"文件已复制到: {targetPath}");
        }
        else
        {
            Debug.LogError("文件读取失败，无法复制。");
        }
    }

    private static async Task<byte[]> LoadFileAsync(string path)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return request.downloadHandler.data; // 返回二进制数据
            }
            else
            {
                Debug.LogError($"读取失败: {request.error}");
                return null;
            }
        }
    }

}
public delegate void SMethod();


public static class TextManager
{
    public static Dictionary<string, Dictionary<string, string>> manager = new();
    public static List<string> languages = new();
    public static string curLangue = Ct.set.language;

    public static void AddLangue(string language)
    {
        if (!ExistLangue(language))
        {
            languages.Add(language);
            manager.Add(language, new());
        }
    }
    public static void ChangeLangue(string language)
    {
        if (ExistLangue(language))
            curLangue = language;
        else
        {

            return;
        }
    }
    public static bool ExistLangue(string language) => languages.Contains(language);
    public static void AddText(string langue, string key, string text, bool addLangue = true)
    {
        if (!ExistLangue(langue))
        {
            if (addLangue)
                AddLangue(langue);
            else
                return;
        }
        manager[langue].Add(key, text);
    }
    public static void AddTextFromFile(string path)
    {
        if (!Data.FileExists(path))
            return;
        string langue = null, prex = "", key = "", val = "";

        foreach (var line in File.ReadAllLines(path))
        {
            string[] pt = line.Split('/');
            if (pt.Length != 2)
                continue;
            if (line[0] == '#')
            {
                string p = pt[0].TrimStart('#');
                switch (p)
                {
                    case "l":
                        langue = pt[1];
                        break;
                    case "p":
                        prex = pt[1];
                        break;
                }
            }
            else
            {
                if (langue == null)
                    continue;
                key = pt[0];
                val = pt[1];

                AddText(langue, prex + key, val);
            }
        }
    }
    /*
# l/zh-cn
# p/itname
    glass/玻璃
     */
    public static string Read(string langue, string key)
    {
        if (ExistLangue(langue))
        {
            var dic = manager[langue];
            if (dic.ContainsKey(key))
            {
                return dic[key];
            }
        }
        return null;
    }
    /// <summary>
    /// particular read 
    /// </summary>
    /// <param name="isItem">if is item or obj</param>
    /// <param name="isName">if is name or desscription</param>
    /// <returns></returns>
    public static string Read(bool isItem, bool isName, string key)
    {
        string k = isItem ? "it" : "ob";
        k += isName ? "name" : "descrp";
        k += "_" + key;

        return Read(k);
    }
    /// <summary>
    /// read the text, by burrent language
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string Read(string key) => Read(curLangue, key);

    public class Text
    {
        public string langue;
        public string key;
        public string text;

        /// <summary>
        /// Add to text manager
        /// </summary>
        /// <param name="addLangue">add langue if it is not exist</param>
        public void AddTo(bool addLangue = true) => AddText(langue, key, text, addLangue);
    }
}