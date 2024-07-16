using System;
using System.IO;
using System.Reflection;
using JALib.Tools;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JALib.Core.Setting;

internal class JAModSetting : JASetting {
    private string path;
    public Version LatestVersion;
    public bool ForceUpdate;
    public SystemLanguage[] AvailableLanguages;
    public string Homepage;
    public bool IsBetaBranch;
    public string Discord;
    public SystemLanguage? CustomLanguage;
    internal JASetting Setting;
    
    public JAModSetting(JAMod mod, string path = null, Type type = null) : base(mod, LoadJson(mod, ref path)) {
        this.path = path;
        if(type != null && !JsonObject.ContainsKey(nameof(Setting))) JsonObject[nameof(Setting)] = new JObject();
        Setting = type?.New<JASetting>(Mod, JsonObject[nameof(Setting)] as JObject);
        if(!JsonObject.ContainsKey(nameof(Feature))) JsonObject[nameof(Feature)] = new JObject();
    }
    
    private static JObject LoadJson(JAMod mod, ref string path) {
        path ??= Path.Combine(mod.Path, "Settings.json");
        return !File.Exists(path) ? new JObject() : JObject.Parse(File.ReadAllText(path));
    }

    public new void PutFieldData() {
        try {
            Setting.PutFieldData();
            JArray features = new();
            foreach (Feature f in Mod.Features) {
                f.FeatureSetting.PutFieldData();
                features.Add(f.FeatureSetting.JsonObject);
            }
            JsonObject[nameof(Feature)] = features;
            base.PutFieldData();
        } catch (Exception e) {
            JALib.Instance.LogException(e);
        }
    }
    
    public new void RemoveFieldData() {
        try {
            Setting.RemoveFieldData();
            foreach(Feature f in Mod.Features) f.FeatureSetting.RemoveFieldData();
            JsonObject.Remove(nameof(Feature));
            base.RemoveFieldData();
        } catch (Exception e) {
            JALib.Instance.LogException(e);
        }
    }
    
    public void Save() {
        try {
            PutFieldData();
            File.WriteAllText(path, JsonObject.ToString());
            RemoveFieldData();
        } catch (Exception e) {
            JALib.Instance.LogException(e);
        }
    }
    
    protected override void Dispose0() {
        try {
            Setting.Dispose();
            base.Dispose0();
        } catch (Exception e) {
            JALib.Instance.LogException(e);
        }
    }
}