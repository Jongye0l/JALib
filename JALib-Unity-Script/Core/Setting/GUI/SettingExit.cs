using UnityEngine;
using UnityEngine.UI;

namespace JALib.Core.Setting.GUI;

public class SettingExit : MonoBehaviour {
    public SettingPanel panel;
    public Button button;

    public void OnClick() {
        panel.gameObject.SetActive(false);
    }
}