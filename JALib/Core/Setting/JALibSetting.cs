using Newtonsoft.Json.Linq;

namespace JALib.Core.Setting;

class JALibSetting : JASetting {

    public JASetting Beta;

    protected JALibSetting(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
    }
}