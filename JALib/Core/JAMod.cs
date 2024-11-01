using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
    private static string loadScene;

    internal static ModuleBuilder ModuleBuilder {
        get {
            if(_moduleBuilder == null) {
                assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("JALib.CustomPatch"), AssemblyBuilderAccess.Run);
                _moduleBuilder = assemblyBuilder.DefineDynamicModule("JALib.CustomPatch");
            }
            return _moduleBuilder;
        }
    }

    protected internal UnityModManager.ModEntry ModEntry { get; private set; }
    public UnityModManager.ModEntry.ModLogger Logger => ModEntry.Logger;
    public string Name { get; private set; }
    public Version Version => ModEntry.Version;
    public string Path => ModEntry.Path;
    protected Version LatestVersion => ModSetting.LatestVersion;
    public bool IsLatest => LatestVersion <= Version;
    protected internal List<Feature> Features { get; private set; }
    protected SystemLanguage[] AvailableLanguages => ModSetting.AvailableLanguages;
    internal JAModSetting ModSetting;
    protected JASetting Setting => ModSetting.Setting;
    protected string Discord = "https://discord.jongyeol.kr/";
    public bool Enabled => ModEntry.Enabled;
    public bool Active => ModEntry.Active;
    internal int Gid;
    internal JAModInfo JaModInfo; // TODO : Move JALib When Beta end
    internal FieldInfo staticField;
    internal bool Initialized;
    internal List<JAMod> usedMods = [];
    internal List<JAMod> usingMods = [];
    internal AppDomain Domain;

    protected internal SystemLanguage? CustomLanguage {
        get => ModSetting.CustomLanguage;
        set {
            ModSetting.CustomLanguage = value;
            Localization.Load();
        }
    }

    public JALocalization Localization { get; private set; }

    protected JAMod(UnityModManager.ModEntry modEntry, bool localization, Type settingType = null, string settingPath = null, string discord = null, int gid = -1) {
        try {
            ModEntry = modEntry;
            Domain = AppDomain.CurrentDomain;
            Name = ModEntry.Info.Id;
            ModSetting = new JAModSetting(this, settingPath, settingType);
            Features = [];
            Localization = localization ? new JALocalization(this) : null;
            Discord = ModSetting.Discord ?? discord ?? Discord;
            Gid = gid;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload0;
            mods[Name] = this;
            SaveSetting();
            SetupStaticField();
            Log("JAMod " + Name + " is Initialized");
        } catch (Exception e) {
            ModEntry.Info.DisplayName = $"{Name} <color=red>[Fail to load]</color>";
            Error("Failed to Initialize JAMod " + Name);
            LogException(e);
            throw;
        }
    }

    internal void SetupEvent() {
        if(IsExistMethod(nameof(OnUpdate))) ModEntry.OnUpdate = OnUpdate0;
        if(IsExistMethod(nameof(OnFixedUpdate))) ModEntry.OnFixedUpdate = OnFixedUpdate0;
        if(IsExistMethod(nameof(OnLateUpdate))) ModEntry.OnLateUpdate = OnLateUpdate0;
    }

    internal void SetupEventMain() {
        ModEntry.Info.HomePage = ModSetting.Homepage ?? ModEntry.Info.HomePage ?? Discord;
        if(CheckGUIRequire()) ModEntry.OnGUI = OnGUI0;
        if(CheckGUIEventRequire(nameof(OnShowGUI))) ModEntry.OnShowGUI = OnShowGUI0;
        if(CheckGUIEventRequire(nameof(OnHideGUI))) ModEntry.OnHideGUI = OnHideGUI0;
    }

    private bool CheckGUIRequire() => IsExistMethod(nameof(OnGUI)) || IsExistMethod(nameof(OnGUIBehind)) || Features.Any(feature => feature.CanEnable || feature.IsExistMethod(nameof(OnGUI)));

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
        MainThread.Run(JALib.Instance, () => {
            foreach(JAMod mod in mods.Values.Where(mod => mod != JALib.Instance && mod.Enabled)) {
                if(mod.ModEntry.Active) continue;
                mod.ModEntry.Active = true;
                mod.OnEnable();
            }
        });
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
        if(!Enabled || !ModEntry.Active || !Initialized) return;
        MainThread.Run(this, () => {
            foreach(Feature f in feature) if(f.Enabled) f.Enable();
        });
    }

    internal void ModInfo(GetModInfo getModInfo) {
        if(ModEntry == null || !getModInfo.Success) return;
        ModSetting.LatestVersion = getModInfo.LatestVersion;
        ModSetting.AvailableLanguages = getModInfo.AvailableLanguages;
        ModSetting.Homepage = getModInfo.Homepage;
        ModSetting.Discord = getModInfo.Discord;
        Gid = getModInfo.Gid;
        SaveSetting();
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
            Initialized = true;
            foreach(Feature feature in Features) if(feature.Enabled) feature.Enable();
            foreach(JAMod mod in usedMods) mod.OnToggle(null, true);
        } else {
            foreach(Feature feature in Features) if(feature.Enabled) feature.Disable();
            Initialized = false;
            OnDisable();
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

    private bool OnUnload0(UnityModManager.ModEntry modEntry) {
        modEntry.OnToggle = null;
        modEntry.OnUnload = null;
        modEntry.OnUpdate = null;
        modEntry.OnFixedUpdate = null;
        modEntry.OnLateUpdate = null;
        modEntry.SetValue("OnSessionStart", null);
        modEntry.SetValue("OnSessionStop", null);
        ModSetting.Dispose();
        ModSetting = null;
        foreach(Feature feature in Features) feature.Unload();
        if(mods[Name] == this) mods.Remove(Name);
        try {
            OnDisable();
        } catch (Exception e) {
            LogException(e);
        }
        try {
            OnUnload();
        } catch (Exception e) {
            LogException(e);
        }
        foreach(JAMod mod in usedMods) mod.OnUnload0(null);
        DomainHandler.UnloadDomain(Domain);
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

    public void Inactive() => ModEntry.SetValue("mActive", false);

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

    internal static void LogPatchException(Exception e, JAMod mod, string id, bool prefix) => mod.LogException("An error occurred while invoking a " + (prefix ? "Pre" : "Post") + "fix Patch " + id, e);

    public void SaveSetting() => ModSetting?.Save();

    internal async Task ForceReloadMod() {
        try {
            // TODO : Remove This
            if(GetType().Assembly.GetTypes().Any(type => typeof(MonoBehaviour).IsAssignableFrom(type))) return;
            string modName = ModEntry.Info.Id;
            ModEntry.Info.DisplayName = modName + " <color=gray>[Force Reload...]</color>";
            string path = System.IO.Path.Combine(ModEntry.Path, "Info.json");
            if(!File.Exists(path)) path = System.IO.Path.Combine(ModEntry.Path, "info.json");
            UnityModManager.ModInfo info = (await File.ReadAllTextAsync(path)).FromJson<UnityModManager.ModInfo>();
            ModEntry.SetValue("Info", info);
            bool beta = typeof(JABootstrap).Invoke<bool>("InitializeVersion", [ModEntry]);
            JAModInfo modInfo = typeof(JABootstrap).Invoke<JAModInfo>("LoadModInfo", ModEntry, beta);
            SetupModInfo(modInfo);
            GetModInfo getModInfo = null;
            try {
                if(JApi.Instance != null) {
                    modInfo.ModEntry.Info.DisplayName = modName + " <color=gray>[Loading Info...]</color>";
                    Log("Force Reload: Loading Info...");
                    getModInfo = await JApi.Send(new GetModInfo(modInfo), false);
                    if(getModInfo.Success && getModInfo.ForceUpdate && getModInfo.LatestVersion > modInfo.ModEntry.Version) {
                        _ = JApi.Send(new DownloadMod(modName, getModInfo.LatestVersion), false);
                        return;
                    }
                }
            } catch (Exception e) {
                modInfo.ModEntry.Logger.Log("Failed to Load ModInfo " + modName);
                modInfo.ModEntry.Logger.LogException(e);
            }
            if(modInfo.Dependencies != null) {
                List<Task> tasks = [];
                ModEntry.Info.DisplayName = modName + " <color=gray>[Loading Dependencies...]</color>";
                Log("Force Reload: Loading Dependencies...");
                foreach(KeyValuePair<string, string> dependency in modInfo.Dependencies) {
                    try {
                        Version version = new(dependency.Value);
                        UnityModManager.ModEntry modEntry = UnityModManager.modEntries.Find(entry => entry.Info.Id == dependency.Key);
                        if(modEntry != null && modEntry.Version >= version) continue;
                        tasks.Add(JApi.Send(new DownloadMod(dependency.Key, version), false));
                    } catch (Exception e) {
                        ModEntry.Logger.Log($"Failed to Load Dependency {dependency.Key}({dependency.Value})");
                        ModEntry.Logger.LogException(e);
                    }
                }
                ModEntry.Info.DisplayName = modName + " <color=aqua>[Waiting Dependencies...]</color>";
                Log("Force Reload: Waiting Dependencies...");
                foreach(Task task in tasks) {
                    try {
                        await task;
                    } catch (Exception e) {
                        ModEntry.Logger.Log("Failed to Download 1 Dependency");
                        ModEntry.Logger.LogException(e);
                    }
                }
            }
            modInfo.ModEntry.Info.DisplayName = modName + " <color=gray>[Unloading...]</color>";
            Log("Force Reload: Unloading...");
            MainThread.Run(new JAction(JALib.Instance, () => {
                try {
                    JAPatchAttribute patchAttribute = new(ADOBase.LoadScene, PatchType.Replace, false);
                    JAPatcher patcher = new(JALib.Instance);
                    patcher.AddPatch(LoadScenePatch, patchAttribute);
                    patcher.Patch();
                    OnUnload0(ModEntry);
                    GC.Collect();
                    modInfo.ModEntry.Info.DisplayName = modName + " <color=gray>[loading...]</color>";
                    modInfo.ModEntry.Logger.Log("Force Reload: loading...");
                    Type type;
                    try {
                        type = typeof(JABootstrap).Invoke<Type>("LoadMod", [modInfo]);
                    } catch (Exception e) {
                        modInfo.ModEntry.Logger.Log("Failed to Load JAMod " + modName);
                        modInfo.ModEntry.Logger.LogException(e);
                        modInfo.ModEntry.SetValue("mErrorOnLoading", true);
                        modInfo.ModEntry.SetValue("mActive", false);
                        modInfo.ModEntry.Info.DisplayName = modName + " <color=red>[Error: Need Restart]</color>";
                        return;
                    }
                    modInfo.ModEntry.Info.DisplayName = modName + " <color=gray>[Activing...]</color>";
                    modInfo.ModEntry.Logger.Log("Force Reload: Activing...");
                    JAMod mod = GetMods(modName);
                    try {
                        mod.OnToggle(null, true);
                    } catch (Exception e) {
                        mod.LogException(e);
                        mod.ModEntry.SetValue("mActive", false);
                    }
                    patcher.Dispose();
                    if(loadScene != null) {
                        ADOBase.LoadScene(loadScene);
                        loadScene = null;
                    }
                    if(getModInfo != null) mod.ModInfo(getModInfo);
                    modInfo.ModEntry.Info.DisplayName = modName;
                    modInfo.ModEntry.Logger.Log("Force Reload: Complete");
                } catch (Exception e) {
                    JALib.Instance.Error("Failed to Force Reload Mod " + Name);
                    JALib.Instance.LogException(e);
                }
            }));
        } catch (Exception e) {
            JALib.Instance.Error("Failed to Force Reload Mod " + Name);
            JALib.Instance.LogException(e);
        }
    }

    private static void LoadScenePatch(string sceneName) {
        loadScene = sceneName;
    }

    internal static void SetupModInfo(JAModInfo modInfo) {
        string modName = modInfo.ModEntry.Info.Id;
        JALib lib = JALib.Instance;
        bool beta = lib.Setting.Beta[modName]?.ToObject<bool>() ?? false;
        if(beta != modInfo.IsBetaBranch) {
            lib.Setting.Beta[modName] = modInfo.IsBetaBranch;
            lib.SaveSetting();
        }
        modInfo.ModEntry.Info.DisplayName = modName + " <color=gray>[Waiting...]</color>";
    }
}