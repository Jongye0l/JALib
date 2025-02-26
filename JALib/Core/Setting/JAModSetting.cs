using System.IO;
using JALib.Tools;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JALib.Core.Setting;

class JAModSetting : JASetting {
    private string path;
    public Version LatestVersion;
    public Version LatestBetaVersion;
    public bool ForceUpdate;
    public bool ForceBetaUpdate;
    public SystemLanguage[] AvailableLanguages;
    public string Homepage;
    public string Discord;
    public SystemLanguage? CustomLanguage;
    public bool UnlockBeta;
    public bool Beta;
    internal JASetting Setting;

    public JAModSetting(string path) : base(null, LoadJson(path)) {
        this.path = path;
        if(!JsonObject.ContainsKey(nameof(Feature))) JsonObject[nameof(Feature)] = new JObject();
    }

    internal void SetupType(Type type, JAMod mod) {
        Mod = mod;
        if(type != null && !JsonObject.ContainsKey(nameof(Setting))) JsonObject[nameof(Setting)] = new JObject();
        Setting = type?.New<JASetting>(Mod, JsonObject[nameof(Setting)] as JObject);
    }

    private static JObject LoadJson(string path) {
        try {
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

    internal void Combine(JAModSetting setting) {
        UnlockBeta = setting.UnlockBeta;
        Beta = setting.Beta;
    }

    public override void PutFieldData() {
        try {
            Setting?.PutFieldData();
            foreach(Feature f in Mod.Features) f.FeatureSetting.PutFieldData();
            base.PutFieldData();
        } catch (Exception e) {
            (Mod ?? JALib.Instance).LogReportException("Fail PutFieldData Setting", e);
        }
    }

    public override void RemoveFieldData() {
        try {
            Setting?.RemoveFieldData();
            foreach(Feature f in Mod.Features) f.FeatureSetting.RemoveFieldData();
            base.RemoveFieldData();
        } catch (Exception e) {
            (Mod ?? JALib.Instance).LogReportException("Fail Save ModSetting", e);
        }
    }

    public void Save() {
        try {
            if(File.Exists(path)) File.Copy(path, path + ".bak", true);
            PutFieldData();
            File.WriteAllText(path, JsonObject.ToString());
            RemoveFieldData();
        } catch (Exception e) {
            (Mod ?? JALib.Instance).LogReportException("Fail Save ModSetting", e);
        }
    }

    protected override void Dispose0() {
        try {
            Setting?.Dispose();
            base.Dispose0();
        } catch (Exception e) {
            (Mod ?? JALib.Instance).LogReportException("Fail Dispose ModSetting", e, [Mod, JALib.Instance]);
        }
    }
}