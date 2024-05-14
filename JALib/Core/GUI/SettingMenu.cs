using JALib.Core.Setting.GUI;
using JALib.Core.Setting.GUI.Features;
using UnityEngine;

namespace JALib.Core.GUI;

public class SettingMenu {

    public static SettingPanel Panel;
    public static GameObject Content;
    private static JAMod activeMod;
    
    internal static void Initialize() {
        GameObject ob = Object.Instantiate(JABundle.JASettings);
        ob.SetActive(false);
        Object.DontDestroyOnLoad(ob);
        Panel = ob.GetComponent<SettingPanel>();
        Content = ob.GetComponentsInChildren<SettingContents>()[0].gameObject;
    }

    public static void Reset() {
        for(int i = 0; i < Content.transform.childCount; i++) Object.Destroy(Content.transform.GetChild(i));
    }
    
    public static void ShowFeature(JAMod mod) {
        activeMod = mod;
        Reset();
        GameObject featureContent = null;
        bool first = true;
        foreach(Feature feature in mod.Features) {
            if(first) featureContent = Object.Instantiate(JABundle.FeatureContent, Content.transform);
            GameObject featureOb = Object.Instantiate(JABundle.Feature, featureContent.transform);
            FeatureMenu menu = featureOb.GetComponent<FeatureMenu>();
            menu.text.text = feature.Name;
            menu.SetEnable(feature.Enabled);
            first = !first;
        }
        Panel.gameObject.SetActive(true);
    }
    
    internal static void Dispose() {
        if(!Panel) return; 
        Object.Destroy(Panel);
        Content = null;
        Panel = null;
        JABundle.Dispose();
    }
}