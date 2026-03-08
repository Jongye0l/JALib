using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using HarmonyLib;
using JALib.API;
using JALib.API.Packets;
using JALib.Bootstrap;
using JALib.Core;
using JALib.Core.ModLoader;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Tools;
using Microsoft.Win32;
using UnityEngine;
using UnityModManagerNet;

namespace JALib;

#pragma warning disable CS0649
sealed class JALib : JAMod {
    internal const string ModId = nameof(JALib);
    internal static JALib Instance;
    internal static Harmony Harmony;
    internal new JALibSetting Setting;
    internal static bool Quitting;
    private static SettingGUI _settingGUI;
    private static bool enableInit;

    private JALib(UnityModManager.ModEntry modEntry) : base(typeof(JALibSetting)) {
        Instance = this;
        Type bootstrapType = typeof(JABootstrap);
        try {
            JaModInfo = typeof(JABootstrap).GetValue<JAModInfo>("jalibModInfo") ?? 
                        MigrateModInfo((bootstrapType = modEntry.Assembly.GetType("JALib.Bootstrap.JABootstrap"))?.GetValue("jalibModInfo"));
        } catch (Exception e) {
            LogReportException("Fail to get mod info from JABootstrap.", e);
        }
        if((object) bootstrapType != null && bootstrapType.Assembly.GetName().Version < new Version(1, 0, 0, 8)) {
            JAPatcher patcher = new(this);
            foreach(Type nestedType in bootstrapType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)) {
                foreach(Type nestedType2 in nestedType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)) {
                    if(!nestedType2.Name.Contains("<Load>")) continue;
                    patcher.AddPatch(BootstrapLoaderTranspiler, new JAPatchAttribute(nestedType2.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), PatchType.Transpiler, false));
                    break;
                }
            }
            patcher.Patch();
        }
        
        Setup(modEntry, JaModInfo, null, new JAModSetting(System.IO.Path.Combine(modEntry.Path, "Settings.json")));
        if(JaModInfo.IsBetaBranch) ModSetting.UnlockBeta = ModSetting.Beta = true;
        Setting = (JALibSetting) base.Setting;
        _settingGUI = new SettingGUI(this);
        Patcher.AddPatch(JALocalization.RDStringPatch).AddPatch(JALocalization.RDStringSetup).AddPatch(ModTools.Load);
        JApi.Initialize();
        JATask.Run(Instance, Init);
        OnEnable();
        SetupEvent();
        MainThread.Run(Instance, SetupEventMain);
        Application.quitting += () => Quitting = true;
    }

    private static IEnumerable<CodeInstruction> BootstrapLoaderTranspiler(IEnumerable<CodeInstruction> instructions) {
        List<CodeInstruction> codes = instructions.ToList();
        for(int i = 0; i < codes.Count; i++) {
            if(codes[i].opcode == OpCodes.Ldstr && " <color=red>[Error Loading JALib]</color>" == codes[i].operand.AsUnsafe<string>()) {
                int start = 0;
                for(int j = i; j >= 0; j--) {
                    if(codes[j].opcode == OpCodes.Pop) {
                        start = j + 1;
                        break;
                    }
                }
                while(codes[++i].opcode != OpCodes.Leave);
                Label label = (Label) codes[i].operand;
                codes[i + 1].labels.Add(label);
                codes.RemoveRange(start, i - start + 1);
                i = start;
                while(++i < codes.Count) {
                    CodeInstruction code = codes[i];
                    if(code.labels.Remove(label)) break;
                    if(code.opcode == OpCodes.Ldsfld && code.operand is FieldInfo { Name: "LoadJAMod" }) {
                        codes.RemoveAt(i);
                        codes[i + 2] = new CodeInstruction(OpCodes.Call, ((Delegate) LoadModInfo2).Method);
                    }
                }
                break;
            }
        }
        return codes;
    }

    private void Init() {
        LoadInfo();
        Patcher.Patch();
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
        string applicationFolderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ModId, "ModApplicator");
        string applicationPath = System.IO.Path.Combine(applicationFolderPath, "JALib ModApplicator.exe");
        using(RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\JALib")) {
            if(key.GetValue("URL Protocol") == null) {
                key.SetValue("", "URL Protocol");
                key.SetValue("URL Protocol", "");
                using RegistryKey key2 = Registry.CurrentUser.CreateSubKey(@"Software\Classes\JALib\shell\open\command");
                key2.SetValue("", $"\"{applicationPath}\" \"%1\"");
            }
            key.SetValue("AdofaiPath", Environment.CurrentDirectory);
            key.SetValue("Port", portTask.Result);
        }
        if(File.Exists(applicationPath)) {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(applicationPath);
            if(Version.Parse(versionInfo.FileVersion) >= new Version(1, 0, 0, 3)) return;
        }
        Directory.CreateDirectory(applicationFolderPath);
        Process[] processes = Process.GetProcessesByName("JALib ModApplicator.exe");
        if(processes.Length > 0) {
            foreach(Process process in processes) {
                process.WaitForExit(3000);
                if(!process.HasExited) process.Kill();
            }
        }
        Instance.Log("Unzip ModApplicator...");
        Zipper.Unzip(System.IO.Path.Combine(Instance.Path, "ModApplicator.zip"), applicationFolderPath);
    }

    private static JAModInfo MigrateModInfo(object modInfo) {
        try {
            if(modInfo == null) return null;
            if(modInfo is JAModInfo info) return info;
            info = new JAModInfo();
            foreach(FieldInfo field in modInfo.GetType().Fields()) 
                info.SetValue(field.Name, field.GetValue(modInfo));
            return info;
        } catch (Exception e) {
            Instance.LogReportException("Fail to migrate mod info.", e);
            return null;
        }
    }

    private static void LoadModInfo(JAModInfo modInfo) {
        try {
            JAModLoader.AddMod(modInfo, 0);
        } catch (Exception e) {
            modInfo.ModEntry.Logger.LogException(e);
        }
    }

    private static void LoadModInfo2(object modInfo) {
        LoadModInfo(MigrateModInfo(modInfo));
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
                Task.Yield().OnCompleted(LoadInfo);
                return;
            }
            JApi.Send(new GetModInfo(JaModInfo, ModSetting.Beta), false).OnCompleted(Instance, ModInfo, JATask.CompleteFlag.None);
        } catch (Exception e) {
            LogReportException("Fail to load mod info.", e);
        }
    }

    private void ModInfo(Task<GetModInfo> task) {
        try {
            if(task.Exception != null) throw task.Exception.InnerExceptions.Count == 1 ? task.Exception.InnerExceptions[0] : task.Exception;
            GetModInfo apiInfo = task.Result;
            ModInfo(apiInfo);
            ModEntry.Info.Version = (apiInfo.LatestVersion > ModEntry.Version ? "<color=red>" : "<color=cyan>") + ModEntry.Info.Version + "</color>";
            SaveSetting();
        } catch (Exception e) {
            LogReportException("Fail to load mod info.", e);
        }
    }

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
        ApplicatorAPI.Dispose();
        Dispose();
    }

    protected override void OnUpdate(float deltaTime) {
        MainThread.OnUpdate();
    }

    protected override void OnGUI() {
        SettingGUI settingGUI = _settingGUI;
        JALocalization localization = Instance.Localization;
        settingGUI.AddSettingToggle(ref Setting.logPatches, localization["Setting.LogPatches"]);
        settingGUI.AddSettingToggle(ref Setting.logPrefixWarn, localization["Setting.LogPrefixWarn"]);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        int current = Setting.loggerLogDetail;
        if(GUILayout.Button(Bold(localization["Setting.LogDetail.Thread"], (current & 1) == 1))) {
            Setting.loggerLogDetail ^= 1;
            SaveSetting();
        }
        if(GUILayout.Button(Bold(localization["Setting.LogDetail.StackFrame"], (current & 2) == 2))) {
            Setting.loggerLogDetail ^= 2;
            SaveSetting();
        }
        if(GUILayout.Button(Bold(localization["Setting.LogDetail.FileInfo"], (current & 4) == 4))) {
            Setting.loggerLogDetail ^= 4;
            SaveSetting();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    
    private static string Bold(string text, bool bold) => bold ? "<b>" + text + "</b>" : text;
}