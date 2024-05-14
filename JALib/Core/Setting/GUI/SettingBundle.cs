using System.IO;
using UnityEngine;

namespace JALib.Core.Setting.GUI;

public class SettingBundle {
    
    private static AssetBundle bundle;
    public static GameObject JASettings;
    public static GameObject FeatureContent;
    public static GameObject Feature;
    
    internal static void Initialize() {
        bundle = AssetBundle.LoadFromFile(Path.Combine(JALib.Instance.Path, "SettingBundle"));
        if(!bundle) throw new FileNotFoundException("SettingBundle not found.");
        JASettings = bundle.LoadAsset<GameObject>("JASettings.prefab");
        FeatureContent = bundle.LoadAsset<GameObject>("FeatureContent.prefab");
        Feature = bundle.LoadAsset<GameObject>("Feature.prefab");
    }
    
    internal static void Dispose() {
        JASettings = null;
        FeatureContent = null;
        Feature = null;
        bundle.Unload(true);
        bundle = null;
    }
}