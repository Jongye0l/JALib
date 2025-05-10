using System.IO;
using System.Linq;
using TinyJson;
using UnityEngine;
using UnityModManagerNet;

namespace JALib.Tools;

public static class ForceApplyMod {
    public static void ApplyMod(string path) {
        string path1 = Path.Combine(path, "Info.json");
        if(!File.Exists(path1)) path1 = Path.Combine(path, "info.json");
        if(!File.Exists(path1)) return;
        UnityModManager.Logger.Log("Reading file '" + path1 + "'.");
        try {
            UnityModManager.ModInfo info = File.ReadAllText(path1).FromJson<UnityModManager.ModInfo>();
            if(string.IsNullOrEmpty(info.Id)) {
                UnityModManager.Logger.Error("Id is null.");
                return;
            }
            if(string.IsNullOrEmpty(info.AssemblyName) && File.Exists(Path.Combine(path, info.Id + ".dll"))) info.AssemblyName = info.Id + ".dll";
            UnityModManager.ModEntry modEntry = new(info, path + Path.DirectorySeparatorChar);
            UnityModManager.modEntries.Add(modEntry);
            foreach(UnityModManager.Param.Mod mod in typeof(UnityModManager).InvokeUnsafe<UnityModManager.Param>("get_Params").ModParams.Where(mod => mod.Id == info.Id)) modEntry.Enabled = mod.Enabled;
            if(modEntry.Enabled) modEntry.Active = true;
        } catch (Exception ex) {
            UnityModManager.Logger.Error("Error parsing file '" + path1 + "'.");
            Debug.LogException(ex);
        }
    }
}