using System;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Tools;
using UnityEngine;

namespace JALib.Core;

public abstract class Feature {

    private static GUIStyle _expandStyle;
    private static GUIStyle _enableStyle;
    private static GUIStyle _enableLabelStyle;

    public bool Enabled {
        get => FeatureSetting.Enabled;
        set {
            if(FeatureSetting.Enabled != value) {
                FeatureSetting.Enabled = value;
                Mod.SaveSetting();
            }
            if(value && !Active) Enable();
            else if(!value && Active) Disable();
        }
    }

    public bool Active { get; private set; }
    internal bool _expanded;
    private readonly bool _canExpand;
    public bool CanEnable { get; protected set; }
    internal JAFeatureSetting FeatureSetting;
    public JASetting Setting => FeatureSetting.Setting;
    public JAMod Mod { get; private set; }
    public string Name { get; private set; }
    protected JAPatcher Patcher { get; private set; }
    private byte _critical;

    protected Feature(JAMod mod, string name, bool canEnable = true, Type patchClass = null, Type settingType = null) {
        Mod = mod;
        Name = name;
        Patcher = new JAPatcher(mod);
        if(patchClass != null) Patcher.AddPatch(patchClass);
        CanEnable = canEnable;
        FeatureSetting = new JAFeatureSetting(this, settingType);
        _canExpand = IsExistMethod(nameof(OnGUI)) || IsExistMethod(nameof(OnShowGUI)) || IsExistMethod(nameof(OnHideGUI));
    }

    internal bool IsExistMethod(string name) => GetType().Method(name).DeclaringType == GetType();

    internal void Enable() {
        try {
            if(Active) return;
            Patcher.Patch();
            OnEnable();
            Active = true;
        } catch (Exception e) {
            Mod.LogException(e);
        }
    }

    internal void Disable() {
        try {
            if(!Active) return;
            Patcher.Unpatch();
            OnDisable();
            Active = false;
        } catch (Exception e) {
            Mod.LogException(e);
        }
    }

    internal void Unload() {
        Disable();
        Patcher.Dispose();
        FeatureSetting.Dispose();
        try {
            OnUnload();
        } catch (Exception e) {
            Mod.LogException(e);
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
        _expandStyle ??= new GUIStyle {
            fixedWidth = 10f,
            normal = new GUIStyleState { textColor = Color.white },
            fontSize = 15,
            margin = new RectOffset(4, 2, 6, 6)
        };
        _enableStyle ??= new GUIStyle(GUI.skin.toggle) {
            fontStyle = FontStyle.Normal,
            margin = new RectOffset(0, 4, 4, 4)
        };
        _enableLabelStyle ??= new GUIStyle(GUI.skin.label) {
            fontStyle = FontStyle.Normal,
            margin = new RectOffset(4, 4, 4, 4)
        };
        GUILayout.BeginHorizontal();
        bool enabled, expanded;
        try {
            expanded = GUILayout.Toggle(_expanded, Enabled && _canExpand ? _expanded ? "◢" : "▶" : "", _expandStyle);
            if(!CanEnable) {
                enabled = Enabled;
                GUILayout.Space(15f);
                GUILayout.Label(Name, _enableLabelStyle);
            } else enabled = GUILayout.Toggle(Enabled, Name, _enableStyle);
        } finally {
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        if(enabled != Enabled) {
            Enabled = enabled;
            if(enabled && _canExpand) expanded = true;
        }
        if(expanded != _expanded) {
            _expanded = expanded;
            if(!expanded) OnHideGUI0();
            else OnShowGUI0();
        }
        if(!_expanded || !Enabled) return;
        GUILayout.BeginHorizontal();
        GUILayout.Space(24f);
        GUILayout.BeginVertical();
        try {
            OnGUI();
            _critical = 0;
        } catch (Exception e) {
            Mod.Error("Error OnGUI in " + Name);
            Mod.LogException(e);
            if(++_critical > 3) {
                _expanded = false;
                OnHideGUI0();
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.Space(12f);
    }

    protected virtual void OnGUI() {
    }

    internal void OnShowGUI0() {
        try {
            OnShowGUI();
        } catch (Exception e) {
            Mod.Error("Error OnShowGUI in " + Name);
            Mod.LogException(e);
            _expanded = false;
        }
    }

    protected virtual void OnShowGUI() {
    }

    internal void OnHideGUI0() {
        try {
            OnHideGUI();
        } catch (Exception e) {
            Mod.Error("Error OnHideGUI in " + Name);
            Mod.LogException(e);
        }
    }

    protected virtual void OnHideGUI() {
    }
}