using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
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
    internal static JAPatcher Patcher;
    internal new JALibSetting Setting;
    private static Task<Type> loadTask;
    private static Dictionary<string, Task> loadTasks = new();
    private static Dictionary<string, Version> updateQueue = new();

    private JALib(UnityModManager.ModEntry modEntry) : base(modEntry, true, typeof(JALibSetting)) {
        Instance = this;
        Setting = (JALibSetting) base.Setting;
        Patcher = new JAPatcher(this).AddPatch(OnAdofaiStart);
        loadTask = LoadInfo();
    }

    private static async void LoadModInfo(JAModInfo modInfo) {
        SetupModInfo(modInfo);
        Type type = await loadTask;
        if(type == null) loadTasks[modInfo.ModEntry.Info.Id] = SetupMod(modInfo);
        else type.Invoke("SetupMod", null, modInfo);
    }

    private static void SetupModInfo(JAModInfo modInfo) {
        modInfo.ModName = modInfo.ModEntry.Info.DisplayName;
        bool beta = modInfo.IsBetaBranch = Instance.Setting.Beta[modInfo.ModName]?.ToObject<bool>() ?? false;
        modInfo.ModVersion = ParseVersion(modInfo.ModEntry, ref modInfo.IsBetaBranch);
        if(beta != modInfo.IsBetaBranch) {
            Instance.Setting.Beta[modInfo.ModName] = modInfo.IsBetaBranch;
            Instance.SaveSetting();
        }
        modInfo.ModEntry.Info.DisplayName = modInfo.ModName + " <color=gray>[Waiting...]</color>";
    }

    private static async Task SetupMod(JAModInfo modInfo) {
        GetModInfo getModInfo = null;
        if(JApi.Connected) {
            getModInfo = new GetModInfo(modInfo);
            modInfo.ModEntry.Info.DisplayName = modInfo.ModName + " <color=gray>[Loading Info...]</color>";
            await JApi.Send(getModInfo);
            if(getModInfo.Success && getModInfo.ForceUpdate && getModInfo.LatestVersion > modInfo.ModVersion) {
                Instance.Log("JAMod " + modInfo.ModName + " is Forced to Update");
                modInfo.ModEntry.Info.DisplayName = modInfo.ModName + " <color=aqua>[Updating...]</color>";
                await JApi.Send(new DownloadMod(modInfo.ModName, getModInfo.LatestVersion, modInfo.ModEntry.Path));
                string path = System.IO.Path.Combine(modInfo.ModEntry.Path, "Info.json");
                if(!File.Exists(path)) path = System.IO.Path.Combine(modInfo.ModEntry.Path, "info.json");
                UnityModManager.ModInfo info = (await File.ReadAllTextAsync(path)).FromJson<UnityModManager.ModInfo>();
                modInfo.ModEntry.SetValue("Info", info);
                modInfo = typeof(JABootstrap).Invoke<JAModInfo>("LoadModInfo", modInfo.ModEntry);
                SetupModInfo(modInfo);
            }
        }
        if(modInfo.Dependencies != null) {
            List<Task> tasks = new();
            modInfo.ModEntry.Info.DisplayName = modInfo.ModName + " <color=gray>[Loading Dependencies...]</color>";
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
            modInfo.ModEntry.Info.DisplayName = modInfo.ModName + " <color=aqua>[Waiting Dependencies...]</color>";
            await Task.WhenAll(tasks);
        }
        modInfo.ModEntry.Info.DisplayName = modInfo.ModName;
        try {
            typeof(JABootstrap).Invoke("LoadMod", [modInfo]);
        } catch (Exception e) {
            modInfo.ModEntry.Logger.Log("Failed to Load JAMod " + modInfo.ModName);
            modInfo.ModEntry.Logger.LogException(e);
            modInfo.ModEntry.SetValue("mErrorOnLoading", true);
            modInfo.ModEntry.SetValue("mActive", false);
            return;
        }
        JAMod mod = GetMods(modInfo.ModName);
        try {
            mod.OnToggle(null, true);
        } catch (Exception e) {
            mod.LogException(e);
            mod.ModEntry.SetValue("mActive", false);
        }
        if(getModInfo != null) mod.ModInfo(getModInfo);
    }

    private static async Task SetupDependency(string name, Version version, UnityModManager.ModEntry modEntry) {
        bool needUpdate = false;
        if(updateQueue.TryGetValue(name, out Version value) && value < version) {
            updateQueue[name] = version;
            needUpdate = true;
        }
        await Task.Yield();
        if(loadTasks.TryGetValue(name, out Task task)) await task;
        if(needUpdate && updateQueue.TryGetValue(name, out value) && value == version) {
            updateQueue.Remove(name);
            if(modEntry != null && modEntry.Version >= version) return;
            task = DownloadDependency(name, version, modEntry);
            loadTasks[name] = task;
            await task;
            return;
        }
        if(modEntry != null && modEntry.Version >= version) return;
        if(loadTasks.TryGetValue(name, out task)) await task;
    }

    private static async Task DownloadDependency(string name, Version version, UnityModManager.ModEntry modEntry) {
        string path = modEntry?.Path ?? System.IO.Path.Combine(UnityModManager.modsPath, name);
        await JApi.Send(new DownloadMod(name, version, path));
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
        GetModInfo getModInfo = new(new JAModInfo {
            ModName = Name,
            ModVersion = Version,
            IsBetaBranch = Setting.Beta[Name]?.ToObject<bool>() ?? false
        });
        await JApi.Send(getModInfo);
        ModInfo(getModInfo);
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
        bool beta = false;
        ParseVersion(ModEntry, ref beta);
        ModEntry.Info.DisplayName = Name;
        Type type;
        try {
            type = typeof(JABootstrap).Invoke<Type>("SetupJALib", ModEntry);
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
        Harmony = new Harmony(ModEntry.Info.Id);
        Patcher.Patch();
    }

    protected override void OnDisable() {
        Harmony.UnpatchAll(ModEntry.Info.Id);
        Patcher.Unpatch();
        DisableInit();
        JApi.Instance.Dispose();
        MainThread.Dispose();
        GC.Collect();
    }

    protected override void OnUnload() {
        Patcher.Dispose();
        Patcher = null;
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


    [JAPatch(typeof(scnSplash), "GoToMenu", PatchType.Postfix, false)]
    private static void OnAdofaiStart() {
        JApi.OnAdofaiStart();
    }
}