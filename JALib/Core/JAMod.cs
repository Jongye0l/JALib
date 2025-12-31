using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using JALib.API;
using JALib.API.Packets;
using JALib.Bootstrap;
using JALib.Core.ModLoader;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Tools;
using TinyJson;
using UnityEngine;
using UnityModManagerNet;

namespace JALib.Core;

public abstract class JAMod {
    private static Dictionary<string, JAMod> mods = new();
    private static ModuleBuilder _moduleBuilder;
    internal static AssemblyBuilder assemblyBuilder;

    internal static ModuleBuilder ModuleBuilder {
        get {
            if(_moduleBuilder == null) {
                assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("JALib.CustomPatch"), AssemblyBuilderAccess.Run);
                _moduleBuilder = assemblyBuilder.DefineDynamicModule("JALib.CustomPatch");
            }
            return _moduleBuilder;
        }
    }

    public UnityModManager.ModEntry ModEntry { get; private set; }
    public UnityModManager.ModEntry.ModLogger Logger => ModEntry.Logger;
    public string Name { get; private set; }
    public Version Version => ModEntry.Version;
    public string Path => ModEntry.Path;
    protected Version LatestVersion => ModSetting.Beta ? ModSetting.LatestBetaVersion : ModSetting.LatestVersion;
    public bool IsLatest => LatestVersion == null || LatestVersion <= Version;
    protected internal List<Feature> Features { get; private set; }
    protected SystemLanguage[] AvailableLanguages => ModSetting.AvailableLanguages;
    internal JAModSetting ModSetting;
    protected JASetting Setting => ModSetting.Setting;
    protected string Discord = "https://discord.jongyeol.kr/";
    public bool Enabled => ModEntry.Enabled;
    public bool Active => ModEntry.Active;
    internal int Gid = -1;
    internal JAModInfo JaModInfo; // TODO : Move JALib When Beta end
    internal FieldInfo staticField;
    internal bool Initialized;
    internal List<JAMod> usedMods = [];
    internal List<JAMod> usingMods = [];
    protected JAPatcher Patcher { get; private set; }
    private readonly Type SettingType;
    private Task<DownloadMod> downloadTask;
    internal int reloadCount;

    protected internal SystemLanguage? CustomLanguage {
        get => ModSetting.CustomLanguage;
        set {
            ModSetting.CustomLanguage = value;
            Localization.Load();
        }
    }

    public JALocalization Localization { get; private set; }

    [Obsolete]
    protected JAMod(UnityModManager.ModEntry modEntry, bool localization, Type settingType = null, string settingPath = null, string discord = null, int gid = -1) : this(settingType) {
        ModEntry = modEntry;
        Discord = discord ?? Discord;
        Gid = gid;
        ModSetting = new JAModSetting(settingPath ?? System.IO.Path.Combine(modEntry.Path, "Settings.json"));
        ModSetting.SetupType(settingType, this);
        SetupStaticField();
    }

    protected JAMod() {
        Features = [];
        Patcher = new JAPatcher(this);
        Patcher.OnFailPatch += OnFailPatch;
        SetupStaticField();
    }

    protected JAMod(Type settingType) : this() {
        SettingType = settingType;
    }

    internal void Setup(UnityModManager.ModEntry modEntry, JAModInfo modInfo, GetModInfo apiInfo, JAModSetting setting) {
        modEntry.SetValue("mAssembly", GetType().Assembly);
        if(typeof(JABootstrap).Assembly.GetName().Version == new Version(1, 0, 0, 0)) SetupOldBootstrap(modEntry, apiInfo, setting);
        else SetupCurBootstrap(modEntry, modInfo, apiInfo, setting);
    }

    internal void SetupCurBootstrap(UnityModManager.ModEntry modEntry, JAModInfo modInfo, GetModInfo apiInfo, JAModSetting setting) {
        try {
            ModEntry = modEntry;
            modEntry.SetValue("mAssembly", GetType().Assembly);
            Name = ModEntry.Info.Id;
            if(ModSetting == null) {
                ModSetting = setting;
                setting.SetupType(SettingType, this);
            } else ModSetting.Combine(setting);
            Gid = apiInfo?.Gid ?? modInfo.Gid;
            Localization = Gid != -1 ? new JALocalization(this) : null;
            Discord = ModSetting.Discord ?? modInfo.Discord ?? Discord;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload0;
            mods[Name] = this;
            if(apiInfo != null) ModInfo(apiInfo);
            SaveSetting();
            OnSetup();
            Log("JAMod " + Name + " is Initialized");
        } catch (Exception e) {
            modEntry.Info.DisplayName = $"{Name} <color=red>[Fail to load]</color>";
            LogReportException("Failed to Initialize JAMod " + Name, e);
            throw;
        }
    }

    internal void SetupOldBootstrap(UnityModManager.ModEntry modEntry, GetModInfo apiInfo, JAModSetting setting) {
        try {
            ModEntry = modEntry;
            modEntry.SetValue("mAssembly", GetType().Assembly);
            Name = ModEntry.Info.Id;
            if(ModSetting == null) {
                ModSetting = setting;
                setting.SetupType(SettingType, this);
            } else ModSetting.Combine(setting);
            Gid = apiInfo?.Gid ?? 0;
            Localization = Gid != -1 ? new JALocalization(this) : null;
            Discord = ModSetting.Discord ?? Discord;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload0;
            mods[Name] = this;
            if(apiInfo != null) ModInfo(apiInfo);
            SaveSetting();
            OnSetup();
            Log("JAMod " + Name + " is Initialized");
        } catch (Exception e) {
            modEntry.Info.DisplayName = $"{Name} <color=red>[Fail to load]</color>";
            LogReportException("Failed to Initialize JAMod " + Name, e);
            throw;
        }
    }

    protected virtual void OnSetup() {
    }

    private void OnFailPatch(string name, bool disabled) {
        if(!disabled || !Enabled) return;
        Disable();
        ModEntry.Enabled = true;
    }

    internal void SetupEvent() {
        if(IsExistMethod(nameof(OnUpdate))) ModEntry.OnUpdate = OnUpdate0;
        if(IsExistMethod(nameof(OnFixedUpdate))) ModEntry.OnFixedUpdate = OnFixedUpdate0;
        if(IsExistMethod(nameof(OnLateUpdate))) ModEntry.OnLateUpdate = OnLateUpdate0;
    }

    internal void SetupEventMain() {
#if TEST
        VersionControl.SetupVersion();
#endif
        Thread.CurrentThread.Name ??= "Main Thread";
        ModEntry.Info.HomePage = ModSetting.Homepage ?? ModEntry.Info.HomePage ?? Discord;
        if(CheckGUIRequire()) ModEntry.OnGUI = OnGUI0;
        if(CheckGUIEventRequire(nameof(OnShowGUI))) ModEntry.OnShowGUI = OnShowGUI0;
        if(CheckGUIEventRequire(nameof(OnHideGUI))) ModEntry.OnHideGUI = OnHideGUI0;
    }

    private bool CheckGUIRequire() => IsExistMethod(nameof(OnGUI)) || IsExistMethod(nameof(OnGUIBehind)) || Features.Any(feature => feature.CanEnable || feature.IsExistMethod(nameof(OnGUI))) ||
                                      ModSetting.UnlockBeta && ModSetting.LatestVersion != null || !IsLatest;

    private bool CheckGUIEventRequire(string name) => IsExistMethod(name) || Features.Any(feature => feature.IsExistMethod(name));

    private bool IsExistMethod(string name) => GetType().Method(name).DeclaringType == GetType();

    private void SetupStaticField() {
        foreach(FieldInfo field in GetType().Fields()) {
            if(!field.IsStatic || field.FieldType != GetType()) continue;
            staticField = field;
            field.SetValue(null, this);
            return;
        }
        TypeBuilder typeBuilder = ModuleBuilder.DefineType($"JALib.StaticField.{Name}.{GetHashCode()}", TypeAttributes.Public);
        typeBuilder.DefineField("Mod", GetType(), FieldAttributes.Private | FieldAttributes.Static);
        Type type = typeBuilder.CreateType();
        staticField = type.GetField("Mod");
        staticField.SetValue(null, this);
    }

    public static JAMod GetMods(string name) => mods.GetValueOrDefault(name);

    public static ICollection<JAMod> GetMods() => mods.Values;

    internal static void EnableInit() {
        foreach(JAMod mod in mods.Values) if(mod != JALib.Instance && mod.Enabled && !mod.Active) mod.OnToggle(null, true);
    }

    internal static void DisableInit() {
        foreach(JAMod mod in mods.Values) if(mod != JALib.Instance) mod.OnToggle(null, false);
    }

    internal static void Dispose() {
        mods.Clear();
        mods = null;
    }

    protected void AddFeature(params Feature[] feature) {
        Features.Add(feature);
        if(!Enabled || !ModEntry.Active || !Initialized) return;
        MainThread.Run(this, () => {
            foreach(Feature f in feature) if(f.Enabled) f.Enable();
        });
    }

    internal void ModInfo(GetModInfo getModInfo) {
        if(ModEntry == null || !getModInfo.Success) return;
        ModSetting.LatestVersion = getModInfo.LatestVersion;
        ModSetting.LatestBetaVersion = getModInfo.LatestBetaVersion;
        ModSetting.ForceUpdate = getModInfo.ForceUpdate;
        ModSetting.ForceBetaUpdate = getModInfo.ForceBetaUpdate;
        ModSetting.AvailableLanguages = getModInfo.AvailableLanguages;
        ModSetting.Homepage = getModInfo.Homepage;
        ModSetting.Discord = getModInfo.Discord;
        Gid = getModInfo.Gid;
        if(IsLatest) return;
        ModEntry.NewestVersion = LatestVersion;
        Log($"JAMod {Name} is Outdated (current: {Version}, latest: {LatestVersion})");
    }

    internal bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
        if(this != JALib.Instance && !JALib.Instance.Active) {
            Error("JALib is Disabled");
            return false;
        }
        if(value == Initialized) return true;
        if(value) {
            foreach(JAMod mod in usingMods) {
                if(!mod.Enabled) {
                    if(modEntry != null) Error("Dependency Mod " + mod.Name + " is Disabled");
                    return false;
                }
                if(!mod.Initialized) {
                    if(modEntry != null) Error("Dependency Mod " + mod.Name + " is Inactive");
                    return false;
                }
            }
            if(modEntry == null) ModEntry.SetValue("mActive", true);
            SetupEvent();
            SetupEventMain();
            OnEnable();
            Task.Run(OnEnableAsync).OnCompleted(OnEnableAsyncAfter);
            Patcher.Patch();
            Initialized = true;
            foreach(Feature feature in Features) if(feature.Enabled) feature.Enable();
            foreach(JAMod mod in usedMods) mod.OnToggle(null, true);
        } else {
            foreach(Feature feature in Features) if(feature.Enabled) feature.Disable();
            Initialized = false;
            Patcher.Unpatch();
            OnDisable();
            Task.Run(OnDisableAsync).OnCompleted(OnDisableAsyncAfter);
            foreach(JAMod mod in usedMods) {
                if(mod.Initialized) {
                    mod.Error("Dependency Mod " + Name + " is Disabled");
                    mod.ModEntry.Active = false;
                    mod.ModEntry.Enabled = true;
                }
            }
        }
        return true;
    }

    private void OnEnableAsyncAfter(Task task) {
        if(task.IsCompletedSuccessfully) return;
        LogReportException("Failed to Async Enable JAMod " + Name, task.Exception.InnerException ?? task.Exception);
        if(Enabled) ModEntry.SetValue("mActive", false);
    }

    private void OnDisableAsyncAfter(Task task) {
        if(task.IsCompletedSuccessfully) return;
        LogReportException("Failed to Async Disable JAMod " + Name, task.Exception.InnerException ?? task.Exception);
        if(!Enabled) ModEntry.SetValue("mActive", true);
    }

    private bool OnUnload0(UnityModManager.ModEntry modEntry) {
        modEntry.OnGUI = null;
        modEntry.OnShowGUI = null;
        modEntry.OnHideGUI = null;
        modEntry.OnToggle = null;
        modEntry.OnUnload = null;
        modEntry.OnUpdate = null;
        modEntry.OnFixedUpdate = null;
        modEntry.OnLateUpdate = null;
        modEntry.SetValue("OnSessionStart", null);
        modEntry.SetValue("OnSessionStop", null);
        Log("Mod Entry Unloaded");
        ModSetting.Dispose();
        ModSetting = null;
        Log("Mod Setting Unloaded");
        Patcher.Dispose();
        Patcher = null;
        Log("Patcher Unloaded");
        MainThread.Run(JALib.Instance, OnUnloadMainThread);
        return true;
    }

    private void OnUnloadMainThread() {
        foreach(Feature feature in Features) feature.Unload();
        Log("Features Unloaded");
        if(mods[Name] == this) mods.Remove(Name);
        Log("JAMod Unloaded");
        try {
            OnDisable();
        } catch (Exception e) {
            LogReportException("Failed to Disable JAMod " + Name, e);
        }
        Log("OnDisable Completed");
        try {
            Task task = OnDisableAsync();
            if(!task.IsCompleted) task.RunSynchronously();
        } catch (Exception e) {
            LogReportException("Failed to Async Disable JAMod " + Name, e);
        }
        Log("OnDisableAsync Completed");
        foreach(JAMod mod in usedMods) {
            mod.Error("Dependency Mod " + Name + " is Unloaded");
            mod.OnUnload0(null);
        }
        Log("Dependency Mods Unloaded");
        foreach(JAMod mod in usingMods) mod.usedMods.Remove(this);
        Log("Using Mods Unloaded");
        try {
            OnUnload();
        } catch (Exception e) {
            LogReportException("Failed to Unload JAMod " + Name, e);
        }
        Log("OnUnload Completed");
        ModEntry = null;
        Name = null;
        Features = null;
        Discord = null;
        Localization.Dispose();
        Localization = null;
        usedMods = null;
        usingMods = null;
        GC.Collect();
        Log("JAMod Unload Completed");
    }

    public void Enable() {
        ModEntry.Enabled = true;
        ModEntry.Active = true;
    }

    public void Disable() {
        ModEntry.Enabled = false;
        ModEntry.Active = false;
    }

    public void Inactive() => ModEntry.SetValue("mActive", false);

    protected virtual void OnUnload() {
    }

    protected virtual void OnEnable() {
    }

    protected virtual Task OnEnableAsync() => Task.CompletedTask;

    protected virtual void OnDisable() {
    }

    protected virtual Task OnDisableAsync() => Task.CompletedTask;

    internal void OnGUI0(UnityModManager.ModEntry modEntry) {
        JALocalization localization = JALib.Instance.Localization;
        if(ModSetting.UnlockBeta && ModSetting.LatestVersion != null) {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mod Branch: ");
            if(GUILayout.Button(Bold(localization["Default"], !ModSetting.Beta), GUILayout.Width(60)) && ModSetting.Beta) {
                ModSetting.Beta = false;
                ChangeBranch();
            }
            if(GUILayout.Button(Bold(localization["Beta"], ModSetting.Beta), GUILayout.Width(60)) && !ModSetting.Beta) {
                ModSetting.Beta = true;
                ChangeBranch();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        if(!IsLatest && downloadTask == null) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(localization["Outdated"]);
            if(GUILayout.Button(localization["Update"], GUILayout.Width(120))) DownloadLatest();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        if(downloadTask != null) GUILayout.Label($"<color=cyan>{localization["Updating"]}</color>");
        else {
            OnGUI();
            foreach(Feature feature in Features) feature.OnGUI0();
            OnGUIBehind();
        }
    }

    private static string Bold(string text, bool bold) => bold ? $"<b>{text}</b>" : text;

    private void ChangeBranch() {
        SaveSetting();
        if(Version != LatestVersion) DownloadLatest();
    }

    private void DownloadLatest() {
        ModEntry.Info.DisplayName = Name + " <color=aqua>[Updating...]</color>";
        downloadTask = JApi.Send(new DownloadMod(Name, LatestVersion, Path), true);
        downloadTask.GetAwaiter().UnsafeOnCompleted(DownloadComplete);
    }

    public void DownloadComplete() {
        if(!downloadTask.IsCompletedSuccessfully) {
            LogException($"Failed to download {Name} mod", downloadTask.Exception);
            downloadTask = null;
            return;
        }
        ForceReloadMod();
        downloadTask = null;
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
            LogReportException("Failed to Show GUI", e);
        }
    }

    protected virtual void OnShowGUI() {
    }

    internal void OnHideGUI0(UnityModManager.ModEntry modEntry) {
        try {
            OnHideGUI();
            foreach(Feature feature in Features.Where(feature => feature.Enabled && feature._expanded)) feature.OnHideGUI0();
        } catch (Exception e) {
            LogReportException("Failed to Hide GUI", e);
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

    internal void OnLocalizationUpdate0() {
        OnLocalizationUpdate();
    }

    protected virtual void OnLocalizationUpdate() {
    }

    public void Log(object o) => JALogger.Log(this, o?.ToString(), 1);

    public void Log(object o, int stackTraceSkip) => JALogger.Log(this, o?.ToString(), stackTraceSkip + 1);

    public void Error(object o) => JALogger.Error(this, o?.ToString(), 1);

    public void Error(object o, int stackTraceSkip) => JALogger.Error(this, o?.ToString(), stackTraceSkip + 1);

    public void Warning(object o) => JALogger.Warn(this, o?.ToString(), 1);
    
    public void Warning(object o, int stackTraceSkip) => JALogger.Warn(this, o?.ToString(), stackTraceSkip + 1);

    public void Critical(object o) => JALogger.Critical(this, o?.ToString(), 1);

    public void Critical(object o, int stackTraceSkip) => JALogger.Critical(this, o?.ToString(), stackTraceSkip + 1);

    public void NativeLog(object o) => JALogger.NativeLog(this, o?.ToString(), 1);

    public void NativeLog(object o, int stackTraceSkip) => JALogger.NativeLog(this, o?.ToString(), stackTraceSkip + 1);

    public void LogException(Exception e) => JALogger.LogException(this, null, e, 1);

    public void LogException(Exception e, int stackTraceSkip) => JALogger.LogException(this, null, e, stackTraceSkip + 1);

    public void LogException(string key, Exception e) => JALogger.LogException(this, key, e, 1);

    public void LogException(string key, Exception e, int stackTraceSkip) => JALogger.LogException(this, key, e, stackTraceSkip + 1);

    public void ReportException(Exception e) => ReportException(e, [this]);

    public void ReportException(string key, Exception e) => ReportException(key, e, [this]);

    public void ReportException(Exception e, JAMod[] mod) => ReportException(null, e, mod);

    public void ReportException(string key, Exception e, JAMod[] mod) {
        // Report Exception is will be generated by BugReporter
        try {
            OnReportException(key, e, mod);
        } catch (Exception exception) {
            LogException("Failed to Report Exception Event", exception);
        }
    }
    
    protected virtual void OnReportException(string key, Exception e, JAMod[] mod) {
    }

    public void LogReportException(Exception e) {
        LogException(e, 1);
        ReportException(e);
    }

    public void LogReportException(Exception e, int stackTraceSkip) {
        LogException(e, stackTraceSkip + 1);
        ReportException(e);
    }

    public void LogReportException(string key, Exception e) {
        LogException(key, e, 1);
        ReportException(key, e);
    }

    public void LogReportException(string key, Exception e, int stackTraceSkip) {
        LogException(key, e, stackTraceSkip + 1);
        ReportException(key, e);
    }

    public void LogReportException(Exception e, JAMod[] mod) {
        LogException(e, 1);
        ReportException(e, mod);
    }

    public void LogReportException(Exception e, JAMod[] mod, int stackTraceSkip) {
        LogException(e, stackTraceSkip + 1);
        ReportException(e, mod);
    }

    public void LogReportException(string key, Exception e, JAMod[] mod) {
        LogException(key, e, 1);
        ReportException(key, e, mod);
    }

    public void LogReportException(string key, Exception e, JAMod[] mod, int stackTraceSkip) {
        LogException(key, e, stackTraceSkip + 1);
        ReportException(key, e, mod);
    }

#pragma warning disable CS8509
    internal static void LogPatchException(Exception e, JAMod mod, string id, int patchId) {
        mod.LogReportException("An error occurred while invoking a " + patchId switch {
            0 => "Prefix",
            1 => "Postfix",
            2 => "Override",
        } + " Patch " + id, e);
    }
#pragma warning restore CS8509

    public void SaveSetting() => ModSetting?.Save();

    internal void ForceReloadMod() {
        try {
            string modName = ModEntry.Info.Id;
            ModEntry.Info.DisplayName = modName + " <color=gray>[Force Reload...]</color>";
            Log("Force Reload: Unloading...");
            UnityModManager.ModEntry modEntry = ModEntry;
            OnUnload0(ModEntry);
            string path = System.IO.Path.Combine(modEntry.Path, "Info.json");
            if(!File.Exists(path)) path = System.IO.Path.Combine(modEntry.Path, "info.json");
            UnityModManager.ModInfo info = File.ReadAllText(path).FromJson<UnityModManager.ModInfo>();
            modEntry.SetValue("Info", info);
            bool beta = typeof(JABootstrap).Invoke<bool>("InitializeVersion", [modEntry]);
            JAModInfo modInfo = typeof(JABootstrap).Invoke<JAModInfo>("LoadModInfo", modEntry, beta);
            JAModLoader.AddMod(modInfo, reloadCount + 1);
        } catch (Exception e) {
            JALib.Instance.LogReportException("Failed to Force Reload Mod " + Name, e, [JALib.Instance, this]);
        }
    }
}