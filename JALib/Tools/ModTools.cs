using System.Collections.Generic;
using System.IO;
using System.Linq;
using JALib.Core;
using JALib.Core.Patch;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Tools;

public static class ModTools {
    private static readonly List<(JAMod, Action<UnityModManager.ModEntry>)> _activeEvent = [];

    public static void RegisterModLoadEvent(JAMod mod, Action<UnityModManager.ModEntry> action) {
        _activeEvent.Add((mod, action));
    }
    
    public static void UnregisterModLoadEvent(JAMod mod, Action<UnityModManager.ModEntry> action) {
        _activeEvent.Remove((mod, action));
    }
    
    public static void ApplyMod(JAMod requestMod, string path) {
        const string prefix = "[ApplyMod] ";
        requestMod.Log(prefix + "Finding mod in path '" + path + "'.");
        string path1;
        if(path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
            path1 = path;
            path = Path.GetDirectoryName(path1) ?? string.Empty;
        } else {
            path1 = Path.Combine(path, "Info.json");
            if(!File.Exists(path1)) path1 = Path.Combine(path, "info.json");
            if(!File.Exists(path1)) throw new FileNotFoundException(Path.Combine(path, "Info.json"));
        }
        requestMod.Log(prefix + "Reading file '" + path1 + "'.");
        try {
            UnityModManager.ModInfo info = File.ReadAllText(path1).FromJson<UnityModManager.ModInfo>();
            if(string.IsNullOrEmpty(info.Id)) throw new InvalidDataException(prefix + "Id is null in file '" + path1 + "'.");
            if(string.IsNullOrEmpty(info.AssemblyName) && File.Exists(Path.Combine(path, info.Id + ".dll"))) info.AssemblyName = info.Id + ".dll";
            UnityModManager.ModEntry modEntry = new(info, path + Path.DirectorySeparatorChar);
            UnityModManager.modEntries.Add(modEntry);
            foreach(UnityModManager.Param.Mod mod in typeof(UnityModManager).InvokeUnsafe<UnityModManager.Param>("get_Params").ModParams.Where(mod => mod.Id == info.Id)) modEntry.Enabled = mod.Enabled;
            if(modEntry.Enabled) modEntry.Active = true;
            requestMod.Log(prefix + "Mod '" + info.Id + "' applied successfully from path '" + path + "'.");
        } catch (Exception ex) {
            requestMod.LogReportException(prefix + "Failed to apply mod from path '" + path + '\'', ex);
        }
    }

    [JAPatch(typeof(UnityModManager.ModEntry), nameof(Load), PatchType.Postfix, false, TryingCatch = false)]
    internal static void Load(UnityModManager.ModEntry __instance) {
        try {
            foreach((JAMod mod, Action<UnityModManager.ModEntry> action) in _activeEvent) {
                try {
                    action(__instance);
                } catch (Exception e) {
                    mod.LogReportException("Mod Load Event Error for mod '" + __instance.Info.Id + '\'', e);
                }
            }
        } catch (Exception e) {
            JALib.Instance.LogReportException("Mod Load Event Error", e);
        }
    }
}