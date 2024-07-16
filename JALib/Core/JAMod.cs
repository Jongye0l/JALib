using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JALib.API;
using JALib.API.Packets;
using JALib.Core.Setting;
using JALib.Stream;
using JALib.Tools;
using UnityEngine;
using UnityModManagerNet;

namespace JALib.Core;

public abstract class JAMod {
    private static Dictionary<string, JAMod> mods = new();
    protected internal UnityModManager.ModEntry ModEntry { get; private set; }
    public UnityModManager.ModEntry.ModLogger Logger => ModEntry.Logger;
    public string Name { get; private set; }
    public Version Version => ModEntry.Version;
    public string Path => ModEntry.Path;
    protected bool ForceUpdate => ModSetting.ForceUpdate;
    protected Version LatestVersion => ModSetting.LatestVersion;
    public bool IsLatest => LatestVersion <= Version;
    public readonly bool IsBeta;
    public bool IsBetaBranch => ModSetting.IsBetaBranch;
    protected Dependency[] Dependencies { get; private set; }
    protected internal List<Feature> Features { get; private set; }
    protected SystemLanguage[] AvailableLanguages => ModSetting.AvailableLanguages;
    internal JAModSetting ModSetting;
    protected JASetting Setting => ModSetting.Setting;
    protected internal string Discord = "https://discord.jongyeol.kr/";
    public bool Enabled => ModEntry.Enabled;
    protected internal SystemLanguage? CustomLanguage {
        get => ModSetting.CustomLanguage;
        set {
            ModSetting.CustomLanguage = value;
            Localization.Load();
        }
    }
    public JALocalization Localization { get; private set; }

    protected JAMod(UnityModManager.ModEntry modEntry, bool localization, Dependency[] dependencies = null, Type settingType = null, string settingPath = null, string discord = null) {
        try {
            ModEntry = modEntry;
            Name = ModEntry.Info.DisplayName;
            ModSetting = new JAModSetting(this, settingPath, settingType);
            string version = modEntry.Info.Version;
            string onlyVersion = version;
            string behindVersion = "";
            if(version.Contains('-') || version.Contains(' ')) {
                int index = version.IndexOf('-');
                if(index == -1) index = version.IndexOf(' ');
                onlyVersion = version[..index];
                behindVersion = version[index..];
                IsBeta = true;
                if(!ModSetting.IsBetaBranch) {
                    ModSetting.IsBetaBranch = true;
                    SaveSetting();
                }
            }
            modEntry.SetValue("Version", Version.Parse(onlyVersion));
            modEntry.Info.Version = (Version.Build == 0 ? new Version(Version.Major, Version.Minor) : Version.Revision == -1 ? Version : new Version(Version.Major, Version.Minor, Version.Build)) + behindVersion;
            Dependencies = dependencies ?? Array.Empty<Dependency>();
            Features = new List<Feature>();
            Localization = localization ? new JALocalization(this) : null;
            Discord = ModSetting.Discord ?? discord ?? Discord;
            modEntry.Info.HomePage = ModSetting.Homepage ?? ModEntry.Info.HomePage ?? Discord;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload0;
            InitializeGUI();
            if(IsExistMethod(nameof(OnUpdate))) modEntry.OnUpdate = OnUpdate0;
            if(IsExistMethod(nameof(OnFixedUpdate))) modEntry.OnFixedUpdate = OnFixedUpdate0;
            if(IsExistMethod(nameof(OnLateUpdate))) modEntry.OnLateUpdate = OnLateUpdate0;
            if(IsExistMethod(nameof(OnSessionStart))) modEntry.SetValue("OnSessionStart", (Action<UnityModManager.ModEntry>) OnSessionStart0);
            if(IsExistMethod(nameof(OnSessionStop))) modEntry.SetValue("OnSessionStop", (Action<UnityModManager.ModEntry>) OnSessionStop0);
            mods.Add(Name, this);
            JApi.Send(new GetModInfo(this));
            Log("JAMod " + Name + " is Initialized");
        } catch (Exception e) {
            ModEntry.Info.DisplayName = $"{Name} <color=#FF0000>[Fail to load]</color>";
            Error("Failed to Initialize JAMod " + Name);
            LogException(e);
            throw;
        }
    }

    private async void InitializeGUI() {
        await Task.Yield();
        if(CheckGUIRequire()) ModEntry.OnGUI = OnGUI0;
        if(CheckGUIEventRequire(nameof(OnShowGUI))) ModEntry.OnShowGUI = OnShowGUI0;
        if(CheckGUIEventRequire(nameof(OnHideGUI))) ModEntry.OnHideGUI = OnHideGUI0;
    }
    

    private bool CheckGUIRequire() => IsExistMethod(nameof(OnGUI)) || IsExistMethod(nameof(OnGUIBehind)) || Features.Any(feature => feature.CanEnable || feature.IsExistMethod(nameof(OnGUI)));

    private bool CheckGUIEventRequire(string name) => IsExistMethod(name) || Features.Any(feature => feature.IsExistMethod(name));
    
    private bool IsExistMethod(string name) => GetType().Method(name).DeclaringType == GetType();

    public static JAMod GetMods(string name) => mods[name];
    
    internal static void EnableInit() {
        foreach(JAMod mod in mods.Values.Where(mod => mod != JALib.Instance && mod.Enabled)) {
            if(mod.ModEntry.Active) continue;
            mod.ModEntry.Active = true;
            mod.OnEnable();
        }
    }

    internal static void DisableInit() {
        foreach(JAMod mod in mods.Values.Where(mod => mod != JALib.Instance)) {
            mod.ModEntry.Active = false;
            foreach(Feature feature in mod.Features.Where(feature => feature.Enabled)) feature.Disable();
            mod.OnDisable();
        }
    }
    
    internal static void Dispose() {
        mods.Clear();
        mods = null;
    }

    protected void AddFeature(params Feature[] feature) {
        Features.Add(feature);
    }

    internal void ModInfo(ByteArrayDataInput input) {
        if(ModEntry == null) return;
        ModSetting.LatestVersion = Version.Parse(input.ReadUTF());
        ModSetting.ForceUpdate = input.ReadBoolean();
        var languages = new SystemLanguage[input.ReadByte()];
        for(int i = 0; i < languages.Length; i++) languages[i] = (SystemLanguage) input.ReadByte();
        ModSetting.AvailableLanguages = languages;
        if(input.ReadBoolean()) {
            ModSetting.Homepage = input.ReadUTF();
            ModEntry.Info.HomePage = ModSetting.Homepage;
        } else ModSetting.Homepage = null;
        if(input.ReadBoolean()) {
            ModSetting.Discord = input.ReadUTF();
            Discord = ModSetting.Discord;
        } else ModSetting.Discord = null;
        SaveSetting();
        Log("JAMod " + Name + "'s Info is Updated");
        if(IsLatest) return;
        ModEntry.NewestVersion = LatestVersion;
        Log($"JAMod {Name} is Outdated (current: {Version}, latest: {LatestVersion})");
        if(!ForceUpdate) return;
        Log("JAMod " + Name + " is Forced to Update");
        JAWebApi.DownloadMod(this, true);
    }

    private bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
        if(this != JALib.Instance && !JALib.Active) {
            Error("JALib is Disabled");
            return false;
        }
        if(value) {
            OnEnable();
            foreach(Feature feature in Features.Where(feature => feature.Enabled)) feature.Enable();
        } else {
            foreach(Feature feature in Features.Where(feature => feature.Enabled)) feature.Disable();
            OnDisable();
        }
        return true;
    }
    
    private bool OnUnload0(UnityModManager.ModEntry modEntry) {
        foreach(Feature feature in Features) feature.Unload();
        if(mods[Name] == this) mods.Remove(Name);
        OnDisable();
        OnUnload();
        ModEntry = null;
        Name = null;
        Dependencies = null;
        Features = null;
        Discord = null;
        Localization.Dispose();
        Localization = null;
        ModSetting.Dispose();
        ModSetting = null;
        return true;
    }

    public void Enable() {
        ModEntry.Enabled = true;
        ModEntry.Active = true;
    }

    public void Disable() {
        ModEntry.Enabled = false;
        ModEntry.Active = false;
    }

    protected virtual void OnUnload() {
    }

    protected virtual void OnEnable() {
    }

    protected virtual void OnDisable() {
    }

    internal void OnGUI0(UnityModManager.ModEntry modEntry) {
        OnGUI();
        foreach(Feature feature in Features) feature.OnGUI0();
        OnGUIBehind();
    }

    protected virtual void OnGUI() {
    }

    protected virtual void OnGUIBehind() {
    }

    private void OnShowGUI0(UnityModManager.ModEntry modEntry) {
        try {
            OnShowGUI();
            foreach(Feature feature in Features.Where(feature => feature.Enabled && feature._expanded)) feature.OnShowGUI0();
        } catch (Exception e) {
            LogException(e);
        }
    }
    
    protected virtual void OnShowGUI() {
    }
    
    internal void OnHideGUI0(UnityModManager.ModEntry modEntry) {
        try {
            OnHideGUI();
            foreach(Feature feature in Features.Where(feature => feature.Enabled && feature._expanded)) feature.OnHideGUI0();
        } catch (Exception e) {
            LogException(e);
        }
    }
    
    protected virtual void OnHideGUI() {
    }

    private void OnUpdate0(UnityModManager.ModEntry modEntry, float deltaTime) {
        OnUpdate(deltaTime);
    }

    protected virtual void OnUpdate(float deltaTime) {
    }
    
    private void OnFixedUpdate0(UnityModManager.ModEntry modEntry, float deltaTime) {
        OnFixedUpdate(deltaTime);
    }
    
    protected virtual void OnFixedUpdate(float deltaTime) {
    }
    
    private void OnLateUpdate0(UnityModManager.ModEntry modEntry, float deltaTime) {
        OnLateUpdate(deltaTime);
    }
    
    protected virtual void OnLateUpdate(float deltaTime) {
    }
    
    private void OnSessionStart0(UnityModManager.ModEntry modEntry) {
        OnSessionStart();
    }
    
    protected virtual void OnSessionStart() {
    }
    
    private void OnSessionStop0(UnityModManager.ModEntry modEntry) {
        OnSessionStop();
    }
    
    protected virtual void OnSessionStop() {
    }
    
    internal void OnLocalizationUpdate0() {
        OnLocalizationUpdate();
    }

    protected virtual void OnLocalizationUpdate() {
    }

    public void Log(object o) => Logger.Log(o?.ToString());
    
    public void Error(object o) => Logger.Error(o?.ToString());
    
    public void Warning(object o) => Logger.Warning(o?.ToString());

    public void Critical(object o) => Logger.Critical(o?.ToString());
    
    public void NativeLog(object o) => Logger.NativeLog(o?.ToString());

    public void LogException(string key, Exception e) => Logger.LogException(key, e);

    public void LogException(Exception e) => Logger.LogException(e);

    public void SaveSetting() => ModSetting.Save();
}