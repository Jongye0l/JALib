using System;
using JALib.Core.Patch;
using JALib.Core.Setting;
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
    private bool expendable => Active && (IsExistMethod(nameof(OnGUI)) || Setting != null);
    internal bool expanded;

    protected Feature(JAMod mod, string name, bool canEnable = true, Type patchClass = null, Type settingType = null) {
        Mod = mod;
        Name = name;
        Patcher = new JAPatcher(mod);
        if(patchClass != null) Patcher.AddPatch(patchClass);
        CanEnable = canEnable;
        FeatureSetting = new JAFeatureSetting(this, settingType);
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

    internal void OnGUI0() {
        bool ex = expendable;
        if(!ex && !CanEnable) return;
        GUILayout.BeginHorizontal();
        bool expend = GUILayout.Toggle(expanded, ex ? expanded ? "◢" : "▶" : "", new GUIStyle {
          fixedWidth = 10f,
          normal = new GUIStyleState { textColor = Color.white },
          fontSize = 15,
          margin = new RectOffset(4, 2, 6, 6)
        }, Array.Empty<GUILayoutOption>());
        bool enabled = !CanEnable || GUILayout.Toggle(Enabled, Name, new GUIStyle(GUI.skin.toggle) {
            fontStyle = FontStyle.Normal,
            font = null,
            margin = new RectOffset(0, 4, 4, 4)
        }, Array.Empty<GUILayoutOption>());
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        if(enabled != Enabled) {
            Enabled = enabled;
            if(enabled) expend = ex;
        }
        if(expend != expanded) {
            expanded = expend;
            if(!expend) OnHideGUI();
            else OnShowGUI();
        }
        if(!expend || !Enabled) return;
        GUILayout.BeginHorizontal();
        GUILayout.Space(24f);
        GUILayout.BeginVertical();
        OnGUI();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.Space(12f);
    }
    
    private bool IsExistMethod(string name) {
        return GetType().Method(name).DeclaringType == GetType();
    }

    public virtual void OnGUI() {
        
    }

    public virtual void OnShowGUI() {
        
    }
    
    public virtual void OnHideGUI() {
    }
}