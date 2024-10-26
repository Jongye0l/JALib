using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Win32;
using TinyJson;
using UnityModManagerNet;

namespace JALib;

class JALib : JAMod {
    internal static JALib Instance;
    internal static Harmony Harmony;
    internal new JALibSetting Setting;
    private static Dictionary<string, Task> loadTasks = new();
    private static Dictionary<string, Version> updateQueue = new();
    internal static JAPatcher Patcher;

    private JALib(UnityModManager.ModEntry modEntry) : base(modEntry, true, typeof(JALibSetting), gid: 1716850936) {
        Instance = this;
        Setting = (JALibSetting) base.Setting;
        JApi.Initialize();
        JATask.Run(Instance, Init);
        OnEnable();
    }

    private void Init() {
        LoadInfo();
        Harmony = typeof(JABootstrap).GetValue<Harmony>("harmony") ?? new Harmony(ModEntry.Info.Id);
        Patcher = new JAPatcher(this);
        Patcher.Patch();
        try {
            JaModInfo = typeof(JABootstrap).GetValue<JAModInfo>("jalibModInfo");
        } catch (Exception) {
            // ignored
        }
        SetupModApplicator();
    }

    private static void SetupModApplicator() {
        if(ADOBase.platform == Platform.None) {
            MainThread.WaitForMainThread().GetAwaiter().OnCompleted(SetupModApplicator);
            return;
        }
        if(ADOBase.platform != Platform.Windows) {
            Instance.Log("ModApplicator is only available on Windows. Current: " + ADOBase.platform);
            return;
        }
        Task<int> portTask = JATask.Run(Instance, ApplicatorAPI.Connect);
        string applicationFolderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JALib", "ModApplicator");
        string applicationPath = System.IO.Path.Combine(applicationFolderPath, "JALib ModApplicator.exe");
        using(RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\JALib")) {
            if(key.GetValue("URL Protocol") == null) {
                key.SetValue("", "URL Protocol");
                key.SetValue("URL Protocol", "");
                key.SetValue("AdofaiPath", Environment.CurrentDirectory);
                using RegistryKey key2 = Registry.CurrentUser.CreateSubKey(@"Software\Classes\JALib\shell\open\command");
                key2.SetValue("", $"\"{applicationPath}\" \"%1\"");
            }
            key.SetValue("Port", portTask.Result);
        }
        if(File.Exists(applicationPath)) return;
        Directory.CreateDirectory(applicationFolderPath);
        Process[] processes = Process.GetProcessesByName("JALib ModApplicator.exe");
        if(processes.Length > 0) {
            foreach(Process process in processes) {
                process.WaitForExit(3000);
                if(!process.HasExited) process.Kill();
            }
        }
        Zipper.Unzip(System.IO.Path.Combine(Instance.Path, "ModApplicator.zip"), applicationFolderPath);
    }

    private static void LoadModInfo(JAModInfo modInfo) {
        SetupModInfo(modInfo);
        loadTasks[modInfo.ModEntry.Info.Id] = SetupMod(modInfo);
    }

    private static async Task SetupMod(JAModInfo modInfo) {
        string modName = modInfo.ModEntry.Info.Id;
        GetModInfo getModInfo = null;
        try {
            if(JApi.Instance != null) {
                getModInfo = new GetModInfo(modInfo);
                modInfo.ModEntry.Info.DisplayName = modName + " <color=gray>[Loading Info...]</color>";
                await JApi.Send(getModInfo, false);
                if(getModInfo.Success && getModInfo.ForceUpdate && getModInfo.LatestVersion > modInfo.ModEntry.Version) AddDownload(modName, getModInfo.LatestVersion);
            }
        } catch (Exception e) {
            modInfo.ModEntry.Logger.Log("Failed to Load ModInfo " + modName);
            modInfo.ModEntry.Logger.LogException(e);
        }
        await LoadDependencies(modInfo);
        if(updateQueue.TryGetValue(modName, out Version version) && version > modInfo.ModEntry.Version) {
            Instance.Log("Update JAMod " + modName);
            modInfo.ModEntry.Info.DisplayName = modName + " <color=aqua>[Updating...]</color>";
            try {
                await JApi.Send(new DownloadMod(modName, version, modInfo.ModEntry.Path), false);
                string path = System.IO.Path.Combine(modInfo.ModEntry.Path, "Info.json");
                if(!File.Exists(path)) path = System.IO.Path.Combine(modInfo.ModEntry.Path, "info.json");
                UnityModManager.ModInfo info = (await File.ReadAllTextAsync(path)).FromJson<UnityModManager.ModInfo>();
                modInfo.ModEntry.SetValue("Info", info);
                bool beta = typeof(JABootstrap).Invoke<bool>("InitializeVersion", [modInfo.ModEntry]);
                modInfo = typeof(JABootstrap).Invoke<JAModInfo>("LoadModInfo", modInfo.ModEntry, beta);
                SetupModInfo(modInfo);
                await LoadDependencies(modInfo);
            } catch (Exception e) {
                modInfo.ModEntry.Logger.Log("Failed to Update JAMod " + modName);
                modInfo.ModEntry.Logger.LogException(e);
            }
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
        MainThread.Run(new JAction(mod, () => {
            try {
                mod.OnToggle(null, true);
            } catch (Exception e) {
                mod.LogException(e);
                mod.ModEntry.SetValue("mActive", false);
            }
        }));
        if(getModInfo != null) mod.ModInfo(getModInfo);
    }

    internal static void AddDownload(string modName, Version version) {
        if(updateQueue.TryGetValue(modName, out Version value)) {
            if(value < version) updateQueue[modName] = version;
        } else updateQueue.Add(modName, version);
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
            foreach(Task task in tasks) {
                try {
                    await task;
                } catch (Exception e) {
                    modInfo.ModEntry.Logger.Log("Failed to Load 1 Dependencies");
                    modInfo.ModEntry.Logger.LogException(e);
                }
            }
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
        await JApi.Send(new DownloadMod(name, updateQueue[name], path), true);
        if(modEntry != null) {
            modEntry.Enabled = false;
            modEntry.Active = false;
            modEntry.OnUnload(modEntry);
            UnityModManager.modEntries.Remove(modEntry);
        }
        ForceApplyMod.ApplyMod(path);
    }

    private void LoadInfo() {
        try {
            Task<bool> task = JApi.CompleteLoadTask();
            if(!task.IsCompleted) {
                task.GetAwaiter().OnCompleted(LoadInfo);
                return;
            }
            if(!task.Result) return;
            if(JaModInfo == null) {
                Task.Yield().GetAwaiter().OnCompleted(LoadInfo);
                return;
            }
            JApi.Send(new GetModInfo(JaModInfo), false).ContinueWith(ModInfo);
        } catch (Exception e) {
            LogException(e);
        }
    }

    private void ModInfo(Task<GetModInfo> task) => ModInfo(task.Result);

    protected override void OnEnable() {
        MainThread.Initialize();
        EnableInit();
    }

    protected override void OnDisable() {
        DisableInit();
        JApi.Instance.Dispose();
        MainThread.Dispose();
        GC.Collect();
    }

    protected override void OnUnload() {
        loadTasks.Clear();
        updateQueue.Clear();
        loadTasks = null;
        updateQueue = null;
        ApplicatorAPI.Dispose();
        Dispose();
    }

    protected override void OnUpdate(float deltaTime) {
        MainThread.OnUpdate();
    }
}