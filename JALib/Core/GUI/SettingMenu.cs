using System;
using JALib.Core.Setting.GUI;
using JALib.Core.Setting.GUI.Features;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JALib.Core.GUI;

public class SettingMenu {

    public static SettingPanel Panel;
    public static GameObject Content;
    public static JAMod activeMod { get; private set; }
    public static Feature activeFeature { get; private set; }
    
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
    
    public static void ShowMod(JAMod mod) {
        activeMod = mod;
        activeFeature = null;
        Reset();
        GameObject featureContent = null;
        bool first = true;
        foreach(Feature feature in mod.Features) {
            if(first) featureContent = Object.Instantiate(JABundle.FeatureContent, Content.transform);
            GameObject featureOb = Object.Instantiate(JABundle.Feature, featureContent.transform);
            FeatureMenu menu = featureOb.GetComponent<FeatureMenu>();
            menu.text.text = feature.Name;
            menu.SetEnable(feature.Enabled);
            menu.OnClick += () => ShowFeature(feature);
            first = !first;
        }
        Panel.gameObject.SetActive(true);
    }

    public static void ShowFeature(Feature feature) {
        activeFeature = feature;
        Reset();
        // TODO: Show feature settings
        switch(feature.contentsType) {
            case ContentsType.Feature:
                throw new NotSupportedException();
            case ContentsType.Setting:
                break;
            case ContentsType.Full:
                break;
            case ContentsType.SettingWithDescription:
                break;
        }
        feature.OnShowGUI0();
    }
    
    internal static void Dispose() {
        if(!Panel) return; 
        Object.Destroy(Panel);
        Content = null;
        Panel = null;
        JABundle.Dispose();
    }

    internal static void OnUpdate() {
        if(!Panel.gameObject.activeSelf | activeMod == null) return;
        activeMod.OnGUI0();
        activeFeature?.OnGUI0();
    }
}