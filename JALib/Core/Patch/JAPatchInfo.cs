using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace JALib.Core.Patch;

class JAPatchInfo {
    public TriedPatchData[] tryPrefixes = [];
    public TriedPatchData[] tryPostfixes = [];
    public HarmonyLib.Patch[] replaces = [];
    public HarmonyLib.Patch[] removes = [];
    public List<ReversePatchData> reversePatches = [];

    public void AddReplaces(string owner, HarmonyMethod methods) {
        replaces = Add(owner, methods, replaces);
    }

    public void AddRemoves(string owner, HarmonyMethod methods) {
        removes = Add(owner, methods, removes);
    }

    public void AddTryPrefixes(string owner, HarmonyMethod methods, JAMod mod) {
        tryPrefixes = Add(owner, methods, tryPrefixes, mod);
    }

    public void AddTryPostfixes(string owner, HarmonyMethod methods, JAMod mod) {
        tryPostfixes = Add(owner, methods, tryPostfixes, mod);
    }

    public static HarmonyLib.Patch[] Add(string owner, HarmonyMethod add, HarmonyLib.Patch[] current) {
        int initialIndex = current.Length;
        return current.Concat([new HarmonyLib.Patch(add.method, initialIndex, owner, add.priority, add.before, add.after, add.debug.GetValueOrDefault())]).ToArray();
    }

    public static TriedPatchData[] Add(string owner, HarmonyMethod add, TriedPatchData[] current, JAMod mod) {
        int initialIndex = current.Length;
        return current.Concat([new TriedPatchData(add, initialIndex, owner, mod)]).ToArray();
    }

    public bool IsDebug() => tryPrefixes.Any(x => x.debug) || tryPostfixes.Any(x => x.debug) || replaces.Any(x => x.debug) || removes.Any(x => x.debug);
}