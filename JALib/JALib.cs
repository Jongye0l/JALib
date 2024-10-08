using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HarmonyLib;
using JALib.API;
using JALib.API.Packets;
using JALib.Bootstrap;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Tools;
using TinyJson;
using UnityModManagerNet;

namespace JALib;

class JALib : JAMod {
    internal static JALib Instance;
    internal static Harmony Harmony;
    internal new JALibSetting Setting;
    private static Task<Type> loadTask;
    private static Dictionary<string, Task> loadTasks = new();
    private static Dictionary<string, Version> updateQueue = new();
    internal static JAPatcher Patcher;

    private JALib(UnityModManager.ModEntry modEntry) : base(modEntry, true, typeof(JALibSetting), gid: 1716850936) {
        Instance = this;
        Setting = (JALibSetting) base.Setting;
        loadTask = LoadInfo();
        Harmony = typeof(JABootstrap).GetValue<Harmony>("harmony") ?? new Harmony(ModEntry.Info.Id);
        Patcher = new JAPatcher(this);
        Patcher.Patch();
        OnEnable();
    }

    private static async void LoadModInfo(JAModInfo modInfo) {
        SetupModInfo(modInfo);
        Type type = await loadTask;
        if(type == null) loadTasks[modInfo.ModEntry.Info.Id] = SetupMod(modInfo);
        else type.Invoke("SetupMod", null, modInfo);
    }

    private static void SetupModInfo(JAModInfo modInfo) {
        string modName = modInfo.ModEntry.Info.Id;
        bool beta = Instance.Setting.Beta[modName]?.ToObject<bool>() ?? false;
        if(beta != modInfo.IsBetaBranch) {
            Instance.Setting.Beta[modName] = modInfo.IsBetaBranch;
            Instance.SaveSetting();
        }
        modInfo.ModEntry.Info.DisplayName = modName + " <color=gray>[Waiting...]</color>";
    }

    private static async Task SetupMod(JAModInfo modInfo) {
        string modName = modInfo.ModEntry.Info.Id;
        GetModInfo getModInfo = null;
        if(JApi.Connected) {
            getModInfo = new GetModInfo(modInfo);
            modInfo.ModEntry.Info.DisplayName =  modName + " <color=gray>[Loading Info...]</color>";
            await JApi.Send(getModInfo);
            if(getModInfo.Success && getModInfo.ForceUpdate && getModInfo.LatestVersion > modInfo.ModEntry.Version) AddDownload( modName, getModInfo.LatestVersion);
        }
        await LoadDependencies(modInfo);
        if(JApi.Connected) {
            modInfo.ModEntry.Info.DisplayName =  modName + " <color=gray>[Waiting ConnectInfo...]</color>";
            try {
                await JApi.ConnectInfoTask;
            } catch (Exception e) {
                modInfo.ModEntry.Logger.Log("Failed to ConnectInfo");
                modInfo.ModEntry.Logger.LogException(e);
            }
        }
        if(updateQueue.TryGetValue(modName, out Version version) && version > modInfo.ModEntry.Version) {
            Instance.Log("Update JAMod " + modName);
            modInfo.ModEntry.Info.DisplayName = modName + " <color=aqua>[Updating...]</color>";
            await JApi.Send(new DownloadMod(modName, version, modInfo.ModEntry.Path));
            string path = System.IO.Path.Combine(modInfo.ModEntry.Path, "Info.json");
            if(!File.Exists(path)) path = System.IO.Path.Combine(modInfo.ModEntry.Path, "info.json");
            UnityModManager.ModInfo info = (await File.ReadAllTextAsync(path)).FromJson<UnityModManager.ModInfo>();
            modInfo.ModEntry.SetValue("Info", info);
            bool beta = typeof(JABootstrap).Invoke<bool>("InitializeVersion", [modInfo.ModEntry]);
            modInfo = typeof(JABootstrap).Invoke<JAModInfo>("LoadModInfo", modInfo.ModEntry, beta);
            SetupModInfo(modInfo);
            await LoadDependencies(modInfo);
        }
        modInfo.ModEntry.Info.DisplayName = modName;
        try {
            typeof(JABootstrap).Invoke("LoadMod", [modInfo]);
        } catch (Exception e) {
            modInfo.ModEntry.Logger.Log("Failed to Load JAMod " + modName);
            modInfo.ModEntry.Logger.LogException(e);
            modInfo.ModEntry.SetValue("mErrorOnLoading", true);
            modInfo.ModEntry.SetValue("mActive", false);
            return;
        }
        JAMod mod = GetMods(modName);
        try {
            mod.OnToggle(null, true);
        } catch (Exception e) {
            mod.LogException(e);
            mod.ModEntry.SetValue("mActive", false);
        }
        if(getModInfo != null) mod.ModInfo(getModInfo);
    }

    internal static void AddDownload(string modName, Version version) {
        if(updateQueue.TryGetValue(modName, out Version value)) {
            if(value < version) updateQueue[modName] = version;
        } else updateQueue.Add(modName, version);
    }

    internal static async void DownloadMod(string modName, Version version) {
        await Task.Yield();
        UnityModManager.ModEntry modEntry = UnityModManager.modEntries.Find(entry => entry.Info.Id == modName);
        if(modEntry.Version == version) return;
        AddDownload(modName, version);
        if(!loadTasks.ContainsKey(modName)) loadTasks.Add(modName, DownloadDependency(modName, modEntry));
    }

    private static async Task LoadDependencies(JAModInfo modInfo) {
        if(modInfo.Dependencies != null) {
            List<Task> tasks = [];
            string modName = modInfo.ModEntry.Info.Id;
            modInfo.ModEntry.Info.DisplayName = modName + " <color=gray>[Loading Dependencies...]</color>";
            foreach(KeyValuePair<string, string> dependency in modInfo.Dependencies) {
                try {
                    Version version = new(dependency.Value);
                    UnityModManager.ModEntry modEntry = UnityModManager.modEntries.Find(entry => entry.Info.Id == dependency.Key);
                    if(modEntry != null && modEntry.Version >= version) {
                        if(loadTasks.TryGetValue(dependency.Key, out Task task)) tasks.Add(task);
                        continue;
                    }
                    tasks.Add(SetupDependency(dependency.Key, version, modEntry));
                } catch (Exception e) {
                    modInfo.ModEntry.Logger.Log($"Failed to Load Dependency {dependency.Key}({dependency.Value})");
                    modInfo.ModEntry.Logger.LogException(e);
                }
            }
            modInfo.ModEntry.Info.DisplayName = modName + " <color=aqua>[Waiting Dependencies...]</color>";
            await Task.WhenAll(tasks);
        }
    }

    private static async Task SetupDependency(string name, Version version, UnityModManager.ModEntry modEntry) {
        AddDownload(name, version);
        await Task.Yield();
        if(loadTasks.TryGetValue(name, out Task task)) {
            await task;
            return;
        }
        task = DownloadDependency(name, modEntry);
        loadTasks.Add(name, task);
        await task;
    }

    private static async Task DownloadDependency(string name, UnityModManager.ModEntry modEntry) {
        string path = modEntry?.Path ?? System.IO.Path.Combine(UnityModManager.modsPath, name);
        await JApi.Send(new DownloadMod(name, updateQueue[name], path));
        if(modEntry != null) {
            modEntry.Enabled = false;
            modEntry.Active = false;
            modEntry.OnUnload(modEntry);
            UnityModManager.modEntries.Remove(modEntry);
        }
        ForceApplyMod.ApplyMod(path);
    }

    private async Task<Type> LoadInfo() {
        bool success = await JApi.CompleteLoadTask();
        if(!success) return null;
        if(JaModInfo == null) await Task.Yield();
        GetModInfo getModInfo = new(JaModInfo);
        await JApi.Send(getModInfo);
        ModInfo(getModInfo);
        OnEnable();
        if(!getModInfo.Success || !getModInfo.ForceUpdate || getModInfo.LatestVersion <= Version) return null;
        Log("Update is required. Updating the mod.");
        ModEntry.Info.DisplayName = Name + " <color=blue>[Updating...]</color>";
        await JApi.Send(new DownloadMod(Name, getModInfo.LatestVersion, ModEntry.Path));
        Type accessCacheType = typeof(Traverse).Assembly.GetType("HarmonyLib.AccessCache");
        object accessCache = typeof(Traverse).GetValue("Cache");
        string[] fields = ["declaredFields", "declaredProperties", "declaredMethods", "inheritedFields", "inheritedProperties", "inheritedMethods"];
        foreach (string field in fields) accessCacheType.GetValue<IDictionary>(field, accessCache).Clear();
        string path = System.IO.Path.Combine(ModEntry.Path, "Info.json");
        if(!File.Exists(path)) path = System.IO.Path.Combine(ModEntry.Path, "info.json");
        UnityModManager.ModInfo info = (await File.ReadAllTextAsync(path)).FromJson<UnityModManager.ModInfo>();
        ModEntry.SetValue("Info", info);
        ModEntry.Info.DisplayName = Name;
        Type type;
        try {
            if(JaModInfo == null) await Task.Yield();
            type = typeof(JABootstrap).Invoke<Type>("SetupJALib", JaModInfo);
        } catch (Exception e) {
            Log("Failed to Load JAMod " + Name);
            LogException(e);
            ModEntry.SetValue("mErrorOnLoading", true);
            ModEntry.SetValue("mActive", false);
            throw;
        }
        ForceReloadMod(type.Assembly);
        return type;
    }

    protected override void OnEnable() {
        MainThread.Initialize();
        JApi.Initialize();
        EnableInit();
    }

    protected override void OnDisable() {
        DisableInit();
        JApi.Instance.Dispose();
        MainThread.Dispose();
        GC.Collect();
    }

    protected override void OnUnload() {
        loadTask = null;
        loadTasks.Clear();
        updateQueue.Clear();
        loadTasks = null;
        updateQueue = null;
        Dispose();
    }

    protected override void OnUpdate(float deltaTime) {
        MainThread.OnUpdate();
    }
}