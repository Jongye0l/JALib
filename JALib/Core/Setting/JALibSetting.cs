using Newtonsoft.Json.Linq;

namespace JALib.Core.Setting;

// Disable CS0649 because these fields are assigned by reflection
class JALibSetting : JASetting {

    public JASetting Beta;

    protected JALibSetting(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
    }
}