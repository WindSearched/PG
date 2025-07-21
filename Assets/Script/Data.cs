using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
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
public static class HotUpdate
{
    public static ILRuntime.Runtime.Enviorment.AppDomain LoadDLL(string path, bool hasPdb = false)
    {
        ILRuntime.Runtime.Enviorment.AppDomain appDomain = new();
        appDomain = Start(appDomain);

        byte[] dll = File.ReadAllBytes(path + ".dll");
        byte[] pdb = hasPdb ? File.ReadAllBytes(path + ".pdb") : null;

        MemoryStream dllStream = new(dll);
        if (pdb == null)
        {
            appDomain.LoadAssembly(dllStream);
        }
        else
        {
            using MemoryStream pdbStream = new(pdb);
            appDomain.LoadAssembly(dllStream, pdbStream, new ILRuntime.Mono.Cecil.Pdb.PdbReaderProvider());
        }

        var type = appDomain.LoadedTypes["SPGM.Component/DealerPage"];
        if (type != null)
        {
            Debug.Log("找到了热更类型 DealerPage");
        }
        else
        {
            Debug.LogError("找不到热更类型 DealerPage");
        }

        return appDomain;
    }
    public static ILRuntime.Runtime.Enviorment.AppDomain Start(ILRuntime.Runtime.Enviorment.AppDomain appDomain)
    {
        appDomain.RegisterCrossBindingAdaptor(new IPGMAdapter());
        appDomain.RegisterCrossBindingAdaptor(new BehvLoaderAdapter());
        appDomain.DelegateManager.RegisterMethodDelegate<ActorData, Actor>();
        appDomain.DelegateManager.RegisterDelegateConvertor<ActItc>((act) =>
        {
            return new ActItc((data, actor) =>
            {
                ((Action<ActorData, Actor>)act)(data, actor);
            });
        });
        appDomain.DelegateManager.RegisterMethodDelegate<ItemData>();
        appDomain.DelegateManager.RegisterDelegateConvertor<Item.Interaction>((act) =>
        {
            return new Item.Interaction((item) =>
            {
                ((Action<ItemData>)act)(item);
            });
        });
        appDomain.DelegateManager.RegisterFunctionDelegate<global::BehvLoader>();
        appDomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction>((act) =>
        {
            return new UnityEngine.Events.UnityAction(() =>
            {
                ((Action)act)();
            });
        });

        return appDomain;
    }
}
public static class SPack
{
    public static string Paking(List<string> packs)
    {
        StringBuilder sb = new();
        sb.Append("{");
        foreach (var item in packs)
        {
            sb.Append(item);
            sb.Append(",");
        }
        sb.Remove(sb.Length - 1, 1);
        sb.Append("}");

        return sb.ToString();
    }
    public static string Paking(string pack) => $"{{{pack}}}";
    
    public static List<string> Depack(string pack)
    {
        List<string> list = new();

        if(pack.StartsWith('{')&& pack.EndsWith('}'))
            pack = pack.Substring(1, pack.Length - 2);
        else
        {
            Debug.Log("the pack format is not correct");
            return null;
        }

        int deep = 0;
        StringBuilder sb = new();
        foreach (char c in pack)
        {
            if(c == ',')
            {
                if(deep == 0)
                {
                    list.Add(sb.ToString());
                    sb = new();
                    continue;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '{')
                    deep++;
                else if (c == '}')
                    deep--;
                sb.Append(c);
            }
        }
        list.Add(sb.ToString());
        return list;
    }
}