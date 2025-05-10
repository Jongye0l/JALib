using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using UnityModManagerNet;

namespace JAMod.Bootstrap;

struct BootModData {
    public static ConstructorInfo constructorInfo;
    public static List<BootModData> bootModDataList;
    public static byte action;
    public UnityModManager.ModEntry modEntry;

    public BootModData(UnityModManager.ModEntry modEntry) {
        this.modEntry = modEntry;
        if(bootModDataList == null) {
            bootModDataList = [];
            Checker();
        }
        bootModDataList.Add(this);
        SetPostfix(action switch {
            0 => "<color=red> [JALib Error]</color>",
            1 => "<color=red> [Need Enable JALib]</color>",
            2 => "<color=red> [Waiting Install JALib]</color>",
            _ => ""
        });
    }

    public static void Checker() {
        foreach(UnityModManager.ModEntry modEntry in UnityModManager.modEntries) {
            if(modEntry.Info.Id != "JALib") continue;
            if(modEntry.Enabled) {
                UnityModManager.Logger.Error(modEntry.Active ? "JALib is Active, but cannot found JALib Bootstrap Assembly." : "JALib Failed to load", "[JAMod] ");
                return;
            }
            action = 1;
            UnityModManager.Logger.Error("JALib is Disabled", "[JAMod] ");
            new Harmony("JAMod.LoadChecker").Patch(typeof(UnityModManager.ModEntry).GetMethod("Load", BindingFlags.Public | BindingFlags.Instance), postfix: new HarmonyMethod(((Delegate) OnLoad).Method));
            return;
        }
        action = 2;
        UnityModManager.Logger.Error("JALib is not found", "[JAMod] ");
        Task.Run(Installer.InstallMod);
    }

    private static void OnLoad(UnityModManager.ModEntry modEntry, bool __result) {
        if(modEntry.Info.Id != "JALib") return;
        if(__result) {
            Action<UnityModManager.ModEntry> action = CreateSetupAction(modEntry);
            foreach(BootModData modData in bootModDataList) modData.Run(action);
        } else {
            UnityModManager.Logger.Error("JALib Failed to load", "[JAMod] ");
            action = 0;
            foreach(BootModData modData in bootModDataList) {
                if(modData.modEntry == null) continue;
                modData.SetPostfix("<color=red> [JALib Error]</color>");
            }
        }
        new Harmony("JAMod.LoadChecker").UnpatchAll("JAMod.LoadChecker");
    }

    public static Action<UnityModManager.ModEntry> CreateSetupAction(UnityModManager.ModEntry modEntry) => 
        (Action<UnityModManager.ModEntry>) Delegate.CreateDelegate(typeof(Action<UnityModManager.ModEntry>), modEntry.Assembly.GetType("JALib.Bootstrap.JABootstrap").GetMethod("Setup"));
    
    public void SetPostfix(string postfix) {
        if(modEntry == null) return;
        modEntry.Info.DisplayName = modEntry.Info.Id + postfix;
    }

    public void Run(Action<UnityModManager.ModEntry> action) {
        BootModData.action = 3;
        modEntry.Info.DisplayName = modEntry.Info.Id;
        action(modEntry);
    }
}