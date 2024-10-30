using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JALib.API;
using JALib.API.Packets;
using JALib.Bootstrap;
using JALib.Tools;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Core;

class ModLoader {
    private static Dictionary<string, ModLoader> _loader = new();
    private static int _count;
    private static bool _complete;
    private JAModInfo info;
    private string name;
    private State state;
    private GetModInfo apiModInfo;
    private Version updateVersion;
    private Dictionary<string, Version> downloadRequire;
    private List<string> waitingLoad;
    private List<ModLoader> waitingLoader;
    private TaskCompletionSource<bool> downloadTask;
    private JAMod mod;

    internal static void AddMod(JAModInfo modInfo) {
        if(_complete) {
            if(_loader.TryGetValue(modInfo.ModEntry.Info.Id, out ModLoader modLoader)) modLoader.InitAfter(modInfo);
            else {
                modLoader = new ModLoader(modInfo);
                _loader.Add(modInfo.ModEntry.Info.Id, modLoader);
                modLoader.LoadDependenciesStatic();
                List<Task> tasks = [];
                foreach(ModLoader loader in modLoader.waitingLoader) {
                    if(!loader.state.HasFlag(State.Installing)) {
                        loader.downloadTask ??= new TaskCompletionSource<bool>();
                        tasks.Add(loader.downloadTask.Task);
                        loader.Install();
                    }
                }
                if(modLoader.updateVersion != null) {
                    modLoader.downloadTask ??= new TaskCompletionSource<bool>();
                    tasks.Add(modLoader.downloadTask.Task);
                    modLoader.Install();
                }
                Task.WhenAll(tasks).GetAwaiter().OnCompleted(modLoader.CheckAndApply);
            }
        } else {
            _count++;
            _loader.Add(modInfo.ModEntry.Info.Id, new ModLoader(modInfo));
        }
    }

    internal static void CheckAllLoaded() {
        FieldInfo field = typeof(JABootstrap).Field("LoadCount");
        if(field == null) {
            Task.Delay(100).GetAwaiter().OnCompleted(LoadComplete);
            return;
        }
        if(_count < field.GetValue<int>()) Task.Yield().GetAwaiter().OnCompleted(CheckAllLoaded);
        else LoadComplete();
    }

    private static void LoadComplete() {
        _complete = true;
        bool repeat = true;
        while(repeat) {
            repeat = false;
            ModLoader[] loaders = _loader.Values.ToArray();
            foreach(ModLoader loader in loaders) {
                if(loader.state.HasFlag(State.DependencyLoaded)) continue;
                if(loader.state.HasFlag(State.Dependency)) loader.LoadDependenciesStatic();
                else repeat = true;
            }
        }
        foreach(ModLoader loader in _loader.Values) {
            if(loader.updateVersion != null) loader.Install();
            else {
                loader.state |= State.Installing;
                loader.state |= State.Installed;
                loader.info.ModEntry.Info.DisplayName = loader.name + " <color=gray>[Load Dependencies...]</color>";
            }
        }
        repeat = true;
        while(repeat) {
            repeat = false;
            foreach(ModLoader loader in _loader.Values) {
                if(loader.state.HasFlag(State.Applied)) continue;
                if(loader.state.HasFlag(State.Installed) || loader.CheckWaiting()) loader.Apply();
                else repeat = true;
            }
        }
    }

    private ModLoader(string name) {
        this.name = name;
    }

    private ModLoader(JAModInfo info) {
        this.info = info;
        name = info.ModEntry.Info.Id;
        info.ModEntry.Info.DisplayName = name + " <color=gray>[Loading Info...]</color>";
        LoadAPIModInfo();
        LoadDependencies();
    }

    private void InitAfter(JAModInfo info) {
        this.info = info;
        info.ModEntry.Info.DisplayName = name + " <color=gray>[Loading Info...]</color>";
        LoadAPIModInfo();
        LoadDependencies();
        LoadDependenciesStatic();
    }

    private void LoadAPIModInfo() {
        try {
            if(JApi.Instance != null) {
                apiModInfo = new GetModInfo(info);
                JApi.Send(apiModInfo, false).GetAwaiter().OnCompleted(CheckUpdate);
            } else state |= State.Info;
        } catch (Exception e) {
            info.ModEntry.Logger.Log("Failed to Load ModInfo " + name);
            info.ModEntry.Logger.LogException(e);
            state |= State.Info;
        }
    }

    private void CheckUpdate() {
        if(apiModInfo.Success && apiModInfo.ForceUpdate && apiModInfo.LatestVersion > info.ModEntry.Version) SetUpdateVersion(apiModInfo.LatestVersion);
        state |= State.Info;
    }

    private void SetUpdateVersion(Version version) {
        if(updateVersion == null || updateVersion < version) updateVersion = version;
    }

    private void LoadDependencies() {
        if(info.Dependencies != null) {
            foreach(KeyValuePair<string, string> pair in info.Dependencies) {
                try {
                    Version version = new(pair.Value);
                    UnityModManager.ModEntry modEntry = UnityModManager.modEntries.Find(entry => entry.Info.Id == pair.Key);
                    waitingLoad ??= [];
                    waitingLoad.Add(pair.Key);
                    if(modEntry != null && modEntry.Version >= version) {
                        downloadRequire ??= new Dictionary<string, Version>();
                        downloadRequire.Add(pair.Key, version);
                    }
                } catch (Exception e) {
                    info.ModEntry.Logger.Log($"Failed to Load Dependency {pair.Key}({pair.Value})");
                    info.ModEntry.Logger.LogException(e);
                }
            }
        }
        state |= State.Dependency;
    }

    private void LoadDependenciesStatic() {
        if(downloadRequire != null) {
            foreach(KeyValuePair<string, Version> pair in downloadRequire) {
                if(!_loader.TryGetValue(pair.Key, out ModLoader required)) {
                    required = new ModLoader(pair.Key);
                    _loader.Add(pair.Key, required);
                }
                required.SetUpdateVersion(pair.Value);
                waitingLoader ??= [];
                waitingLoader.Add(required);
            }
            downloadRequire = null;
        }
        if(waitingLoad != null) {
            foreach(string name in waitingLoad) {
                if(!_loader.TryGetValue(name, out ModLoader required)) continue;
                waitingLoader ??= [];
                waitingLoader.Add(required);
            }
            waitingLoad = null;
        }
        state |= State.DependencyLoaded;
    }

    private void Install() {
        state |= State.Installing;
        if(info != null) info.ModEntry.Info.DisplayName = name + " <color=aqua>[Updating...]</color>";
        JApi.Send(new DownloadMod(name, updateVersion, info?.ModEntry.Path), false).ContinueWith(InstallAfter);
    }

    private void InstallAfter(Task<DownloadMod> task) {
        try {
            if(task.Exception != null) throw task.Exception;
            if(info == null) {
                UnityModManager.ModEntry modEntry = UnityModManager.modEntries.Find(entry => entry.Info.Id == name);
                string path = modEntry?.Path ?? Path.Combine(UnityModManager.modsPath, name);
                if(modEntry != null) {
                    modEntry.Enabled = false;
                    modEntry.Active = false;
                    modEntry.OnUnload(modEntry);
                    UnityModManager.modEntries.Remove(modEntry);
                }
                ForceApplyMod.ApplyMod(path);
            } else {
                string path = Path.Combine(info.ModEntry.Path, "Info.json");
                if(!File.Exists(path)) path = Path.Combine(info.ModEntry.Path, "info.json");
                UnityModManager.ModInfo modInfo = File.ReadAllText(path).FromJson<UnityModManager.ModInfo>();
                info.ModEntry.SetValue("Info", modInfo);
                bool beta = typeof(JABootstrap).Invoke<bool>("InitializeVersion", [info.ModEntry]);
                info = typeof(JABootstrap).Invoke<JAModInfo>("LoadModInfo", info.ModEntry, beta);
                JAMod.SetupModInfo(info);
                LoadDependencies();
                LoadDependenciesStatic();
            }
        } catch (Exception e) {
            UnityModManager.ModEntry.ModLogger logger = info?.ModEntry.Logger ?? JALib.Instance.Logger;
            logger.Log("Failed to Update JAMod " + name);
            logger.LogException(e);
        } finally {
            state |= State.Installed;
            downloadTask?.SetResult(true);
        }
    }

    private bool CheckWaiting() {
        if(waitingLoader == null) return true;
        foreach(ModLoader loader in waitingLoader) if(!loader.state.HasFlag(State.Applied)) return false;
        return true;
    }

    private void Apply() {
        info.ModEntry.Info.DisplayName = name;
        try {
            typeof(JABootstrap).Invoke("LoadMod", [info]);
        } catch (Exception e) {
            info.ModEntry.Logger.Log("Failed to Load JAMod " + name);
            info.ModEntry.Logger.LogException(e);
            info.ModEntry.SetValue("mErrorOnLoading", true);
            info.ModEntry.SetValue("mActive", false);
            return;
        }
        mod = JAMod.GetMods(name);
        MainThread.Run(mod, Enable);
        if(apiModInfo != null) mod.ModInfo(apiModInfo);
        state |= State.Applied;
    }

    private void Enable() {
        try {
            mod.OnToggle(null, true);
        } catch (Exception e) {
            mod.LogException(e);
            mod.ModEntry.SetValue("mActive", false);
        }
    }

    private void CheckAndApply() {
        if(CheckWaiting()) Apply();
        else Task.Yield().GetAwaiter().OnCompleted(CheckAndApply);
    }

    [Flags]
    private enum State {
        Info = 1,
        Dependency = 2,
        DependencyLoaded = 4,
        Installing = 8,
        Installed = 16,
        Applied = 32
    }
}