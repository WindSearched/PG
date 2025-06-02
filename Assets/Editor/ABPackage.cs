using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetBundleBuilder
{
    private static string outputPath = Path.Combine(Application.dataPath, "StreamingAssets/abp");

    [MenuItem("Tools/Build AssetBundles/Windows")]
    public static void BuildForWindows() => BuildAllAssetBundles(BuildTarget.StandaloneWindows);

    [MenuItem("Tools/Build AssetBundles/Android")]
    public static void BuildForAndroid() => BuildAllAssetBundles(BuildTarget.Android);

    [MenuItem("Tools/Build AssetBundles/iOS")]
    public static void BuildForIOS() => BuildAllAssetBundles(BuildTarget.iOS);

    public static void BuildAllAssetBundles(BuildTarget target)
    {
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);
        AssetDatabase.Refresh();
        Debug.Log($"AssetBundles 打包完成！输出路径：{Path.GetFullPath(outputPath)}");
    }
}