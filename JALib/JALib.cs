using System;
using System.Reflection;
using HarmonyLib;
using JALib.API;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Tools;
using UnityModManagerNet;

namespace JALib;

public class JALib : JAMod {
    internal static JALib Instance { get; private set; }
    internal static Harmony Harmony;
    private readonly Assembly _assembly;
    internal static bool Active => Instance.ModEntry.Active;
    private static JAPatcher patcher;

    private JALib(UnityModManager.ModEntry modEntry) : base(modEntry, true) {
        Instance = this;
        _assembly = Assembly.GetExecutingAssembly();
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
        JApi.Dispose();
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