using Newtonsoft.Json.Linq;

namespace JALib.Core.Setting;

class JALibSetting : JASetting {

    internal JASetting Beta;

    protected JALibSetting(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
    }
}