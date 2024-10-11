using System.IO;
using System.Linq;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Tools;

public static class ForceApplyMod {
    public static void ApplyMod(string path) {
        UnityModManager.GameInfo config = typeof(UnityModManager).GetValue<UnityModManager.GameInfo>("Config");
        string path1 = Path.Combine(path, config.ModInfo);
        if(!File.Exists(path1)) path1 = Path.Combine(path, config.ModInfo.ToLower());
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
            foreach(UnityModManager.Param.Mod mod in typeof(UnityModManager).GetValue<UnityModManager.Param>("Params").ModParams.Where(mod => mod.Id == info.Id)) modEntry.Enabled = mod.Enabled;
        } catch (Exception ex) {
            UnityModManager.Logger.Error("Error parsing file '" + path1 + "'.");
            UnityEngine.Debug.LogException(ex);
        }
    }
}