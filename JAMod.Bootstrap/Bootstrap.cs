using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using JALib.Bootstrap;
using UnityModManagerNet;

namespace JAMod.Bootstrap;

public static class Bootstrap {
    public static void Setup(UnityModManager.ModEntry modEntry) {
        try {
            RunBootstrap(modEntry);
        } catch (Exception) {
            bool old = typeof(UnityModManager).Assembly.GetName().Version < new Version(0, 27, 13, 0);
            if(old) {
                try {
                    _ = typeof(BootModData).GetConstructor(null).Invoke([modEntry]);
                } catch (ArgumentNullException) {
                    _ = new BootModData(modEntry);
                    BootModData.constructorInfo = typeof(BootModData).GetConstructor([typeof(UnityModManager.ModEntry)]);
                    new Harmony("JAMod.Bootstrap").Patch(
                        typeof(Type).GetMethod("GetConstructor", BindingFlags.Public | BindingFlags.Instance, null, [typeof(Type[])], null), 
                        new HarmonyMethod(((Delegate) GetConstructorPatch).Method));
                }
            } else _ = new BootModData(modEntry);
        }
    }
    
    private static bool GetConstructorPatch(Type __instance, Type[] types, ref ConstructorInfo __result) {
        if(types != null || __instance.FullName != typeof(BootModData).FullName) return true;
        __result = BootModData.constructorInfo;
        return false;
    }

    private static void RunBootstrap(UnityModManager.ModEntry modEntry) {
        JABootstrap.Load(modEntry);
    }
}