using System.Collections.Generic;
using UnityEngine;

namespace JALib.Core.Setting.GUI;

public class SettingPanel : MonoBehaviour {
    public static SettingPanel Instance;
    public List<SettingCategory> categories;
    
    private void Awake() {
        Instance ??= this;
    }
}