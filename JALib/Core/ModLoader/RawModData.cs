using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using JALib.API;
using JALib.API.Packets;
using JALib.Bootstrap;
using JALib.Tools;
using UnityModManagerNet;

namespace JALib.Core.ModLoader;

class RawModData {
    private static AppDomain domain = AppDomain.CurrentDomain;
    public JAModLoader data;
    public string name;
    public JAModInfo info;
    public UnityModManager.ModInfo modInfo;
    public Task<GetModInfo> modInfoTask;
    public bool checkUpdated;
    public bool loadDependencies;
    public List<JAModLoader> waitingLoad;

    public RawModData(JAModLoader data, JAModInfo info) {
        data.LoadState = ModLoadState.Initializing;
        this.data = data;
        this.info = info;
        modInfo = info.ModEntry.Info;
        name = modInfo.Id;
        modInfo.DisplayName = name + " <color=gray>[Loading Info...]</color>";
        modInfoTask = JApi.Send(new GetModInfo(info), false);
        modInfoTask.GetAwaiter().UnsafeOnCompleted(CheckUpdate);
        LoadDependencies();
    }

    public void CheckUpdate() {
        try {
            if(!modInfoTask.IsCompletedSuccessfully) {
                info.ModEntry.Logger.LogException("Failed to get mod info", modInfoTask.Exception);
                return;
            }
            GetModInfo apiInfo = modInfoTask.Result;
            if(apiInfo.Success && apiInfo.ForceUpdate && apiInfo.LatestVersion > info.ModEntry.Version) data.DownloadRequest(apiInfo.LatestVersion);
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
        if(modInfoTask.IsCompletedSuccessfully) data.mod.ModInfo(modInfoTask.Result);
        data.LoadState = ModLoadState.Loaded;
        info.ModEntry.SetValue("mErrorOnLoading", false);
        info.ModEntry.SetValue("mActive", true);
        data.Complete();
    }

    private void SetError() {
        info.ModEntry.SetValue("mErrorOnLoading", true);
        info.ModEntry.SetValue("mActive", false);
    }

    private void Enable() {
        JAMod mod = data.mod;
        try {
            mod.OnToggle(null, true);
        } catch (Exception e) {
            mod.LogException(e);
            mod.ModEntry.SetValue("mActive", false);
        }
    }

    private void LoadMod() {
        if(info.DependencyPath != null) {
            string dependencyPath = info.DependencyRequireModPath ? Path.Combine(info.ModEntry.Path, info.DependencyPath) : info.DependencyPath;
            foreach(string file in Directory.GetFiles(dependencyPath)) {
                try {
                    domain.Load(File.ReadAllBytes(file));
                } catch (Exception e) {
                    info.ModEntry.Logger.LogException(e);
                }
            }
        }
        Assembly modAssembly = domain.Load(File.ReadAllBytes(info.AssemblyRequireModPath ? Path.Combine(info.ModEntry.Path, info.AssemblyPath) : info.AssemblyPath));
        Type modType = modAssembly.GetType(info.ClassName);
        if(modType == null) throw new TypeLoadException("Type not found.");
        data.mod = modType.New<JAMod>(info.ModEntry);
    }

    public void InstallFinish() {
        loadDependencies = false;
        data.LoadState = ModLoadState.Initializing;
        modInfo.DisplayName = name + " <color=gray>[Loading Info...]</color>";
        LoadDependencies();
    }
}