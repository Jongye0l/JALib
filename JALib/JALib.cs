using System;
using HarmonyLib;
using JALib.API;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Tools;
using UnityModManagerNet;

namespace JALib;

public class JALib : JAMod {
    internal static JALib Instance;
    internal static Harmony Harmony;
    private static JAPatcher patcher;

    private JALib(UnityModManager.ModEntry modEntry) : base(modEntry, true) {
        Instance = this;
        patcher = new JAPatcher(this).AddPatch(OnAdofaiStart);
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