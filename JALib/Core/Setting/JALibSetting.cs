using Newtonsoft.Json.Linq;

namespace JALib.Core.Setting;

#pragma warning disable CS0649
class JALibSetting : JASetting {

    public bool logPatches;
    public bool logPrefixWarn;
    public int loggerLogDetail;
    public JASetting Beta;

    protected JALibSetting(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
    }
}