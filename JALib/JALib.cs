using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using JALib.API;
using JALib.Core;
using JALib.Core.GUI;
using JALib.Core.Patch;
using JALib.Core.Setting.GUI;
using JALib.Tools;
using UnityModManagerNet;

namespace JALib;

public class JALib : JAMod {
    internal static JALib Instance { get; private set; }
    internal static Harmony Harmony;
    private readonly Assembly _assembly;
    internal static bool Active => Instance.ModEntry.Active;
    private static JAPatcher patcher;

    private static void Setup(UnityModManager.ModEntry modEntry) {
        foreach(string file in Directory.GetFiles(System.IO.Path.Combine(modEntry.Path, "lib"), "*.dll")) {
            try {
                Assembly.LoadFile(file);
            } catch (Exception e) {
                modEntry.Logger.LogException(e);
            }
        }
        Instance = new JALib(modEntry);
    }

    private JALib(UnityModManager.ModEntry modEntry) : base(modEntry, true) {
        _assembly = Assembly.GetExecutingAssembly();
        patcher = new JAPatcher(this).AddPatch(OnAdofaiStart);
    }

    protected override void OnEnable() {
        MainThread.Initialize();
        JApi.Initialize();
        EnableInit();
        Harmony = new Harmony(ModEntry.Info.Id);
        patcher.Patch();
        JABundle.Initialize();
        SettingMenu.Initialize();
    }

    protected override void OnDisable() {
        Harmony.UnpatchAll(ModEntry.Info.Id);
        patcher.Unpatch();
        SettingMenu.Dispose();
        DisableInit();
        JApi.Dispose();
        MainThread.Dispose();
        ErrorUtils.Dispose();
        GC.Collect();
    }

    protected override void OnUnload() {
        patcher.Dispose();
        patcher = null;
        Dispose();
    }

    protected override void OnUpdate(float deltaTime) {
        MainThread.OnUpdate();
        ErrorUtils.OnUpdate();
        SettingMenu.OnUpdate();
    }
    
    
    [JAPatch("JALib.AdofaiStart", typeof(scnSplash), "GoToMenu", PatchType.Postfix, false)]
    private static void OnAdofaiStart() {
        ErrorUtils.OnAdofaiStart();
        JApi.OnAdofaiStart();
    }
}