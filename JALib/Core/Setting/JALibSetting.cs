using Newtonsoft.Json.Linq;

namespace JALib.Core.Setting;

#pragma warning disable CS0649
class JALibSetting : JASetting {

    public JASetting Beta;
    public bool logPatches;
    public bool logPrefixWarn;

    protected JALibSetting(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
    }
}