using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JALib.Core.Setting.GUI.Features;

public class FeatureMenu : SettingElement {
    public Button main;
    public TMP_Text text;
    public Button enableButton;
    public Image enableImage;
    public TMP_Text enableText;
    public bool enable = true;
    public event Action<bool> OnEnableChange;
    public event Action OnClick;
    
    private void Awake() {
        SetEnable(enable);
    }

    public void SetEnable(bool enable) {
        enableImage.color = enable ? new Color(0f, 0.6415094f, 0.05031446f) : new Color(0.4811321f, 0.01134745f, 0.0142121f);
        enableText.text = enable ? "Enabled" : "Disabled";
        enableText.color = enable ? Color.green : Color.red;
        if(this.enable == enable) return;
        this.enable = enable;
        OnEnableChange?.Invoke(enable);
    }

    public void ChangeEnable() {
        SetEnable(!enable);
    }
    
    public void OnMainClick() {
        OnClick?.Invoke();
    }
}