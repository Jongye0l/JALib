using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using HarmonyLib;
using JALib.API;
using JALib.API.Packets;
using JALib.Bootstrap;
using JALib.Core;
using JALib.Core.ModLoader;
using JALib.Core.Setting;
using JALib.Tools;
using Microsoft.Win32;
using UnityModManagerNet;

namespace JALib;

#pragma warning disable CS0649
class JALib : JAMod {
    internal static JALib Instance;
    internal static Harmony Harmony;
    internal new JALibSetting Setting;
    private static Dictionary<string, Task> loadTasks = new();
    private static Dictionary<string, Version> updateQueue = new();
    private static bool enableInit;

    private JALib(UnityModManager.ModEntry modEntry) : base(modEntry, true, typeof(JALibSetting), gid: 1716850936) {
        Setting = (JALibSetting) base.Setting;
        JApi.Initialize();
        JATask.Run(Instance, Init);
        OnEnable();
        SetupEvent();
        MainThread.Run(Instance, SetupEventMain);
    }

    private void Init() {
        LoadInfo();
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
        try {
            SetupModInfo(modInfo);
            JAModLoader.AddMod(modInfo);
        } catch (Exception e) {
            modInfo.ModEntry.Logger.LogException(e);
        }
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
        if(enableInit) return;
        MainThread.Initialize();
        EnableInit();
        enableInit = true;
    }

    protected override void OnDisable() {
        enableInit = false;
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