using System.Reflection;
using Newtonsoft.Json.Linq;

namespace JALib.Core.Setting;

class JAFeatureSetting : JASetting {
    public bool Enabled = true;

    internal JASetting Setting;

    public JAFeatureSetting(Feature feature, Type type = null) : base(feature.Mod, GetFeatureSetting(feature)) {
        if(type == null) return;
        if(!JsonObject.ContainsKey(nameof(Setting))) JsonObject[nameof(Setting)] = new JObject();
        Setting = SetupJASetting(type, JsonObject[nameof(Setting)]);
    }

    private static JObject GetFeatureSetting(Feature feature) {
        JObject jObject = feature.Mod.ModSetting[nameof(Feature)][feature.Name] as JObject;
        if(jObject == null) {
            jObject = new JObject();
            feature.Mod.ModSetting[nameof(Feature)][feature.Name] = jObject;
        }
        return jObject;
    }

    public override void PutFieldData() {
        base.PutFieldData();
        Setting?.PutFieldData();
    }

    public override void RemoveFieldData() {
        base.RemoveFieldData();
        Setting?.RemoveFieldData();
    }

    protected override void Dispose0() {
        Setting?.Dispose();
        base.Dispose0();
    }
}