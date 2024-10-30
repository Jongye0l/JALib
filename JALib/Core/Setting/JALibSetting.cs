using Newtonsoft.Json.Linq;

namespace JALib.Core.Setting;

class JALibSetting : JASetting {

    // Resharper Disable CS0649 because these fields are assigned by reflection
    public JASetting Beta;

    protected JALibSetting(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
    }
}