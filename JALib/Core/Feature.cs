using System;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Core.Setting.GUI;
using JALib.Tools;
using UnityEngine;

namespace JALib.Core;

public abstract class Feature {
    public bool Enabled {
        get => FeatureSetting.Enabled;
        private set {
            if(FeatureSetting.Enabled != value) {
                FeatureSetting.Enabled = value;
                Mod.SaveSetting();
            }
            if(value && !Active) Enable();
            else if (!value && Active) Disable();
        }
    }
    public bool Active { get; private set; }
    public bool CanEnable { get; protected set; }
    internal JAFeatureSetting FeatureSetting;
    public JASetting Setting => FeatureSetting.Setting;
    public JAMod Mod { get; private set; }
    public string Name { get; private set; }
    protected JAPatcher Patcher { get; private set; }
    internal ContentsType contentsType;

    protected Feature(JAMod mod, string name, bool canEnable = true, Type patchClass = null, Type settingType = null, ContentsType contentsType = ContentsType.SettingWithDescription) {
        Mod = mod;
        Name = name;
        Patcher = new JAPatcher(mod);
        if(patchClass != null) Patcher.AddPatch(patchClass);
        CanEnable = canEnable;
        FeatureSetting = new JAFeatureSetting(this, settingType);
        this.contentsType = contentsType;
    }

    internal void Enable() {
        try {
            if(Active) return;
            OnEnable();
            Patcher.Patch();
            Active = true;
        } catch (Exception e) {
            Mod.LogException(e);
            ErrorUtils.ShowError(Mod, e);
        }
    }

    internal void Disable() {
        try {
            if(!Active) return;
            OnDisable();
            Patcher.Unpatch();
            Active = false;
        } catch (Exception e) {
            Mod.LogException(e);
            ErrorUtils.ShowError(Mod, e);
        }
    }

    internal void Unload() {
        if(Enabled) OnDisable();
        Patcher.Dispose();
        FeatureSetting.Dispose();
        try {
            OnUnload();
        } catch (Exception e) {
            Mod.LogException(e);
            ErrorUtils.ShowError(Mod, e);
        }
        Patcher = null;
        Mod = null;
        Name = null;
        FeatureSetting = null;
        Active = false;
    }

    protected virtual void OnEnable() {
    }

    protected virtual void OnDisable() {
    }

    protected virtual void OnUnload() {
    }

    internal void OnGUI0() => OnGUI();

    protected virtual void OnGUI() {
    }
    
    internal void OnShowGUI0() => OnShowGUI();

    protected virtual void OnShowGUI() {
    }
    
    internal void OnHideGUI0() => OnHideGUI();
    
    protected virtual void OnHideGUI() {
    }
}