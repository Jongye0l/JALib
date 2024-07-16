using System;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace JALib.Core.Setting;

internal class JAFeatureSetting : JASetting {
    public bool Enabled = true;
    
    internal JASetting Setting;
    
    public JAFeatureSetting(Feature feature, Type type = null) : base(feature.Mod, (feature.Mod.ModSetting[nameof(Feature)]![feature.Name] ??= new JObject()) as JObject) {
        if(type == null) return;
        if(!JsonObject.ContainsKey(nameof(Setting))) JsonObject[nameof(Setting)] = new JObject();
        Setting = SetupJASetting(type, JsonObject[nameof(Setting)]);
    }

    public new void PutFieldData() {
        base.PutFieldData();
        Setting?.PutFieldData();
    }

    public new void RemoveFieldData() {
        base.RemoveFieldData();
        Setting?.RemoveFieldData();
    }

    protected override void Dispose0() {
        Setting?.Dispose();
        base.Dispose0();
    }
}