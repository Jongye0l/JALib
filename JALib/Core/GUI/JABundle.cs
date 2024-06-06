using System.IO;
using JALib.Core.Setting.GUI.Notification;
using UnityEngine;

namespace JALib.Core.GUI;

public class JABundle {
    
    private static AssetBundle bundle;
    public static GameObject JASettings;
    public static GameObject FeatureContent;
    public static GameObject Feature;
    public static GameObject Notification;
    public static NotificationInfo NotificationInfo;
    public static NotificationWarning NotificationWarning;
    public static NotificationError NotificationError;
    
    internal static void Initialize() {
        bundle = AssetBundle.LoadFromFile(Path.Combine(JALib.Instance.Path, "jalib"));
        if(!bundle) throw new FileNotFoundException("SettingBundle not found.");
        JASettings = bundle.LoadAsset<GameObject>("JASettings.prefab");
        FeatureContent = bundle.LoadAsset<GameObject>("FeatureContent.prefab");
        Feature = bundle.LoadAsset<GameObject>("Feature.prefab");
        Notification = bundle.LoadAsset<GameObject>("Notification.prefab");
        NotificationInfo = bundle.LoadAsset<GameObject>("NotificationInfo.prefab").GetComponent<NotificationInfo>();
        NotificationWarning = bundle.LoadAsset<GameObject>("NotificationWarning.prefab").GetComponent<NotificationWarning>();
        NotificationError = bundle.LoadAsset<GameObject>("NotificationError.prefab").GetComponent<NotificationError>();
    }
    
    internal static void Dispose() {
        JASettings = null;
        FeatureContent = null;
        Feature = null;
        bundle.Unload(true);
        bundle = null;
    }
}