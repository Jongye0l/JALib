using UnityEngine;

namespace JALib.Core.Setting.GUI;

public class SettingContents : MonoBehaviour {
    public static SettingContents Instance;
    public ContentsType contentsType;
    
    private void Awake() {
        Instance ??= this;
    }
}