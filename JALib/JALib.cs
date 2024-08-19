using System;
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

public class JALib : JAMod {
    internal static JALib Instance;
    internal static Harmony Harmony;
    private static JAPatcher patcher;
    internal new JALibSetting Setting;
    private static Task<Type> loadTask;

    private JALib(UnityModManager.ModEntry modEntry) : base(modEntry, true, null, typeof(JALibSetting)) {
        Instance = this;
        Setting = (JALibSetting) base.Setting;
        patcher = new JAPatcher(this).AddPatch(OnAdofaiStart);
        loadTask = LoadInfo();
    }

    private static async void LoadModInfo(JAModInfo modInfo) {
        modInfo.ModName = modInfo.ModEntry.Info.DisplayName;
        bool beta = modInfo.IsBetaBranch = Instance.Setting.Beta[modInfo.ModName]?.ToObject<bool>() ?? false;
        modInfo.ModVersion = ParseVersion(modInfo.ModEntry, ref modInfo.IsBetaBranch);
        if(beta != modInfo.IsBetaBranch) Instance.Setting.Beta[modInfo.ModName] = modInfo.IsBetaBranch;
        modInfo.ModEntry.Info.DisplayName = modInfo.ModName + " <color=blue>[Waiting...]</color>";
        Type type = await loadTask;
        if(type == null) SetupMod(modInfo);
        else type.Invoke("SetupMod", modInfo);
    }

    private static async void SetupMod(JAModInfo modInfo) {
        bool success = await JApi.CompleteLoadTask();
        GetModInfo getModInfo = null;
        if(success) {
            getModInfo = new GetModInfo(modInfo);
            modInfo.ModEntry.Info.DisplayName = modInfo.ModName + " <color=blue>[Loading Info...]</color>";
            await JApi.Send(getModInfo);
            if(getModInfo.Success && getModInfo.ForceUpdate && getModInfo.LatestVersion > modInfo.ModVersion) {
                Instance.Log("JAMod " + modInfo.ModName + " is Forced to Update");
                modInfo.ModEntry.Info.DisplayName = modInfo.ModName + " <color=blue>[Updating...]</color>";
                await JApi.Send(new DownloadMod(modInfo.ModName, getModInfo.LatestVersion, modInfo.ModEntry.Path));
                string path = System.IO.Path.Combine(modInfo.ModEntry.Path, "Info.json");
                if(!File.Exists(path)) path = System.IO.Path.Combine(modInfo.ModEntry.Path, "info.json");
                UnityModManager.ModInfo info = (await File.ReadAllTextAsync(path)).FromJson<UnityModManager.ModInfo>();
                modInfo.ModEntry.SetValue("Info", info);
                bool beta = false;
                ParseVersion(modInfo.ModEntry, ref beta);
            }
        }
        modInfo.ModEntry.Info.DisplayName = modInfo.ModName;
        try {
            typeof(JABootstrap).Invoke("LoadMod", modInfo);
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
        try {
            type.Invoke("OnToggle", null, true);
        } catch (Exception e) {
            LogException(e);
            ModEntry.SetValue("mActive", false);
        }
        return type;
    }

    protected override void OnEnable() {
        MainThread.Initialize();
        JApi.Initialize();
        EnableInit();
        Harmony = new Harmony(ModEntry.Info.Id);
        patcher.Patch();
    }

    protected override void OnDisable() {
        Harmony.UnpatchAll(ModEntry.Info.Id);
        patcher.Unpatch();
        DisableInit();
        JApi.Instance.Dispose();
        MainThread.Dispose();
        GC.Collect();
    }

    protected override void OnUnload() {
        patcher.Dispose();
        patcher = null;
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