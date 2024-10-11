using System.IO;
using JALib.Tools;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JALib.Core.Setting;

class JAModSetting : JASetting {
    private string path;
    public Version LatestVersion;
    public SystemLanguage[] AvailableLanguages;
    public string Homepage;
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
        try {
            path ??= Path.Combine(mod.Path, "Settings.json");
            return !File.Exists(path) ? new JObject() : JObject.Parse(File.ReadAllText(path));
        } catch (Exception e) {
            JALib.Instance.Error("Failed to load settings.");
            JALib.Instance.LogException(e);
            try {
                path += ".bak";
                return !File.Exists(path) ? new JObject() : JObject.Parse(File.ReadAllText(path));
            } catch (Exception e2) {
                JALib.Instance.Error("Failed to load backuped settings.");
                JALib.Instance.LogException(e2);
                return new JObject();
            }
        }
    }

    public override void PutFieldData() {
        try {
            Setting?.PutFieldData();
            foreach(Feature f in Mod.Features) f.FeatureSetting.PutFieldData();
            base.PutFieldData();
        } catch (Exception e) {
            JALib.Instance.LogException(e);
        }
    }

    public override void RemoveFieldData() {
        try {
            Setting?.RemoveFieldData();
            foreach(Feature f in Mod.Features) f.FeatureSetting.RemoveFieldData();
            base.RemoveFieldData();
        } catch (Exception e) {
            JALib.Instance.LogException(e);
        }
    }

    public void Save() {
        try {
            if(File.Exists(path)) File.Copy(path, path + ".bak", true);
            PutFieldData();
            File.WriteAllText(path, JsonObject.ToString());
            RemoveFieldData();
        } catch (Exception e) {
            JALib.Instance.LogException(e);
        }
    }

    protected override void Dispose0() {
        try {
            Setting?.Dispose();
            base.Dispose0();
        } catch (Exception e) {
            JALib.Instance.LogException(e);
        }
    }
}