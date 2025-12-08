using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using JALib.API;
using JALib.API.Packets;
using JALib.Bootstrap;
using JALib.Core.Setting;
using JALib.Tools;
using UnityModManagerNet;

namespace JALib.Core.ModLoader;

class RawModData {
    public JAModLoader data;
    public string name;
    public JAModInfo info;
    public JAModSetting setting;
    public UnityModManager.ModInfo modInfo;
    public Task<GetModInfo> modInfoTask;
    public bool checkUpdated;
    public bool loadDependencies;
    public List<JAModLoader> waitingLoad;
    public int repeatCount;

    public RawModData(JAModLoader data, JAModInfo info, int repeatCount) {
        data.LoadState = ModLoadState.Initializing;
        this.data = data;
        this.info = info;
        this.repeatCount = repeatCount;
        modInfo = info.ModEntry.Info;
        name = modInfo.Id;
        modInfo.DisplayName = name + " <color=gray>[Loading Info...]</color>";
        setting = new JAModSetting((typeof(JABootstrap).Assembly.GetName().Version != new Version(1, 0,0, 0) ? GetSettingPath() : null) ?? Path.Combine(info.ModEntry.Path, "Settings.json"));
        if(info.IsBetaBranch) setting.UnlockBeta = setting.Beta = true;
        modInfoTask = JApi.Send(new GetModInfo(info, setting.Beta), false);
        modInfoTask.GetAwaiter().UnsafeOnCompleted(CheckUpdate);
        LoadDependencies();
    }

    private string GetSettingPath() => info.SettingPath;

    public void CheckUpdate() {
        try {
            if(repeatCount == 0)
                if(!modInfoTask.IsCompletedSuccessfully) info.ModEntry.Logger.LogException("Failed to get mod info", modInfoTask.Exception.InnerException ?? modInfoTask.Exception);
                else {
                    GetModInfo apiInfo = modInfoTask.Result;
                    if(apiInfo.Success) {
                        bool notLatest = (setting.Beta ? apiInfo.LatestBetaVersion : apiInfo.LatestVersion) > info.ModEntry.Version;
                        modInfo.Version = (notLatest ? "<color=red>" : "<color=cyan>") + modInfo.Version + "</color>";
                        if(apiInfo.RequestedVersion != null) data.DownloadRequest(apiInfo.RequestedVersion);
                    }
                }
            checkUpdated = true;
            CheckFinishInit();
        } catch (Exception e) {
            info.ModEntry.Logger.LogException(e);
        }
    }

    private void LoadDependencies() {
        if(info.Dependencies != null) {
            waitingLoad ??= [];
            foreach(KeyValuePair<string, string> pair in info.Dependencies) {
                try {
                    JAModLoader loadData = JAModLoader.GetModLoadData(pair.Key);
                    waitingLoad.Add(loadData);
                    loadData.AddCompleteHandle(data);
                    if(!Version.TryParse(pair.Value, out Version version)) {
                        info.ModEntry.Logger.Log($"Failed to Load {pair.Key} Mod's Version: {pair.Value}");
                        version = new Version();
                    }
                    UnityModManager.ModEntry modEntry = UnityModManager.modEntries.Find(entry => entry.Info.Id == pair.Key);
                    if(modEntry != null && modEntry.Version < version) {
                        if(loadData.LoadState is ModLoadState.Loading or ModLoadState.Loaded) data.LoadState = ModLoadState.NeedRestart;
                        loadData.DownloadRequest(version);
                    }
                    if(modEntry?.Enabled != false) continue;
                    info.ModEntry.Logger.Log($"Dependency {pair.Key} is disabled");
                    modInfo.DisplayName = name + " <color=red>[Dependency Mod is disabled]</color>";
                    SetError();
                    loadData.LoadState = ModLoadState.Disabled;
                    data.LoadState = ModLoadState.DependencyDisabled;
                } catch (Exception e) {
                    info.ModEntry.Logger.Log($"Failed to Load Dependency {pair.Key}({pair.Value})");
                    info.ModEntry.Logger.LogException(e);
                }
            }
        }
        loadDependencies = true;
        JAModLoader.CheckDependenciesLoadComplete();
        CheckFinishInit();
    }

    public void CheckFinishInit() {
        if(!checkUpdated || !loadDependencies || !JAModLoader.LoadComplete) return;
        if(data.DownloadModData != null) {
            modInfo.DisplayName = name + " <color=aqua>[Updating...]</color>";
            data.DownloadModData.Download();
            return;
        }
        RecheckDependencies();
    }

    public void RecheckDependencies() {
        if(data.LoadState is ModLoadState.Loaded or ModLoadState.NeedRestart) return;
        modInfo.DisplayName = name + " <color=gray>[Waiting Dependency]</color>";
        if(waitingLoad != null) foreach(JAModLoader loadData in waitingLoad)
            switch(loadData.LoadState) {
                case ModLoadState.None:
                case ModLoadState.Initializing:
                case ModLoadState.Downloading:
                case ModLoadState.Loading:
                    return;
                case ModLoadState.Failed:
                case ModLoadState.DependencyFailed:
                    modInfo.DisplayName = name + " <color=red>[Dependency Mod is Failed]</color>";
                    SetError();
                    data.LoadState = ModLoadState.DependencyFailed;
                    return;
                case ModLoadState.Disabled:
                case ModLoadState.DependencyDisabled:
                    modInfo.DisplayName = name + " <color=red>[Dependency Mod is Disabled]</color>";
                    SetError();
                    data.LoadState = ModLoadState.DependencyDisabled;
                    return;
                case ModLoadState.NeedRestart:
                    modInfo.DisplayName = name + " <color=red>[Need Restart]</color>";
                    SetError();
                    data.LoadState = ModLoadState.NeedRestart;
                    return;
            }
        Start();
    }

    public void Start() {
        data.LoadState = ModLoadState.Loading;
        info.ModEntry.Info.DisplayName = name;
        try {
            LoadMod();
        } catch (Exception e) {
            info.ModEntry.Logger.Log("Failed to Load JAMod " + name);
            info.ModEntry.Logger.LogException(e);
            SetError();
            return;
        }
        MainThread.Run(data.mod, Enable);
        data.LoadState = ModLoadState.Loaded;
        info.ModEntry.SetValue("mErrorOnLoading", false);
        info.ModEntry.SetValue("mActive", true);
        if(waitingLoad != null)
            foreach(JAModLoader loadData in waitingLoad) {
                loadData.mod.usedMods.Add(data.mod);
                data.mod.usingMods.Add(loadData.mod);
            }
        data.Complete();
    }

    private void SetError() {
        info.ModEntry.SetValue("mErrorOnLoading", true);
        info.ModEntry.SetValue("mActive", false);
    }

    private void Enable() {
        JAMod mod = data.mod;
        bool active = false;
        try {
            active = mod.OnToggle(info.ModEntry, true);
        } catch (Exception e) {
            mod.LogReportException("Fail Enable Mod", e);
        }
        if(!active) mod.ModEntry.SetValue("mActive", false);
    }

    private void LoadMod() {
        string cachePath = Path.Combine(info.ModEntry.Path, "assembly_cache");
        if(info.DependencyPath != null) {
            string dependencyPath = info.DependencyRequireModPath ? Path.Combine(info.ModEntry.Path, info.DependencyPath) : info.DependencyPath;
            string cacheDependencyPath = Path.Combine(cachePath, "dependency");
            if(!Directory.Exists(cacheDependencyPath)) Directory.CreateDirectory(cacheDependencyPath);
            List<string> cacheFiles = [];
            foreach(string file in Directory.GetFiles(dependencyPath)) {
                try {
                    string cacheFile = Path.Combine(cacheDependencyPath, Path.GetFileNameWithoutExtension(file) + "-" + new FileInfo(file).LastWriteTimeUtc.GetHashCode() + ".dll");
                    if(!File.Exists(cacheFile)) File.Copy(file, cacheFile);
                    cacheFiles.Add(cacheFile);
                    Assembly.LoadFrom(cacheFile);
                } catch (Exception e) {
                    info.ModEntry.Logger.LogException(e);
                }
            }
            foreach(string file in Directory.GetFiles(cacheDependencyPath)) {
                if(cacheFiles.Contains(file) || !file.EndsWith(".dll")) continue;
                try {
                    File.Delete(file);
                } catch (Exception) {
                    // ignored
                }
            }
        }
        if(!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);
        string assemblyPath = info.AssemblyRequireModPath ? Path.Combine(info.ModEntry.Path, info.AssemblyPath) : info.AssemblyPath;
        string cacheAssemblyPath = Path.Combine(cachePath, Path.GetFileNameWithoutExtension(assemblyPath) + "-" + new FileInfo(assemblyPath).LastWriteTimeUtc.GetHashCode() + ".dll");
        FieldInfo field;
        bool noChangeAssemblyName = (field = info.Field("NoChangeAssemblyName")) != null ? field.GetValue<bool>() : info.NoChangeAssemblyName;
        if(!File.Exists(cacheAssemblyPath)) {
            if(noChangeAssemblyName || repeatCount == 0) AssemblyLoader.CreateCacheAssembly(assemblyPath, cacheAssemblyPath, noChangeAssemblyName);
            else AssemblyLoader.CreateCacheReloadAssembly(assemblyPath, cacheAssemblyPath, repeatCount);
        }
        foreach(string file in Directory.GetFiles(cachePath)) {
            if(file == cacheAssemblyPath || !file.EndsWith(".dll")) continue;
            try {
                File.Delete(file);
            } catch (Exception) {
                // ignored
            }
        }
        Assembly modAssembly = AssemblyLoader.LoadAssembly(cacheAssemblyPath, noChangeAssemblyName);
        Type modType = modAssembly.GetType(info.ClassName);
        if(modType == null) throw new TypeLoadException("Type not found.");
        ConstructorInfo constructor = modType.Constructor([]) ?? modType.Constructor(typeof(UnityModManager.ModEntry));
        data.mod = (JAMod) constructor.Invoke(constructor.GetParameters().Length == 0 ? [] : [info.ModEntry]);
        data.mod.reloadCount = repeatCount;
        data.mod.Setup(info.ModEntry, info, modInfoTask.IsCompletedSuccessfully ? modInfoTask.Result : null, setting);
    }

    public void InstallFinish() {
        loadDependencies = false;
        data.LoadState = ModLoadState.Initializing;
        modInfo.DisplayName = name + " <color=gray>[Loading Info...]</color>";
        GetModInfo apiInfo = modInfoTask.IsCompletedSuccessfully ? modInfoTask.Result : null;
        if(apiInfo != null) {
            bool notLatest = (setting.Beta ? apiInfo.LatestBetaVersion : apiInfo.LatestVersion) > info.ModEntry.Version;
            modInfo.Version = (notLatest ? "<color=red>" : "<color=cyan>") + modInfo.Version + "</color>";
        }
        LoadDependencies();
    }
}