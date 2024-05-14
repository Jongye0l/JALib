using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JALib.Core.Setting.GUI;

public class SettingCategory : MonoBehaviour {
    public SettingPanel panel;
    public Image icon;
    public TMP_Text text;
    public Button button;
    public Image background;
    public bool selected;
    public bool available;
    public SettingContents contents;
    
    public void OnClick() {
        if(selected) return;
        foreach(SettingCategory category in panel.categories.Where(category => category.selected)) {
            category.selected = false;
            category.background.color = new Color(0.1019608f, 0.1137255f, 0.1254902f);
        }
        selected = true;
        background.color = new Color(0.1839623f, 0.8226005f, 1f);
    }
}