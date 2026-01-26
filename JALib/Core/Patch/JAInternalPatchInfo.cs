using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace JALib.Core.Patch;

class JAInternalPatchInfo {
    public TriedPatchData[] tryPrefixes = [];
    public TriedPatchData[] tryPostfixes = [];
    public HarmonyLib.Patch[] replaces = [];
    public HarmonyLib.Patch[] removes = [];
    public ReversePatchData[] reversePatches = [];
    public OverridePatchData[] overridePatches = [];

    public void AddReplaces(string owner, HarmonyMethod methods) => replaces = Add(owner, methods, replaces);
    public void AddRemoves(string owner, HarmonyMethod methods) => removes = Add(owner, methods, removes);
    public void AddTryPrefixes(string owner, HarmonyMethod methods, JAMod mod) => tryPrefixes = Add(owner, methods, tryPrefixes, mod);
    public void AddTryPostfixes(string owner, HarmonyMethod methods, JAMod mod) => tryPostfixes = Add(owner, methods, tryPostfixes, mod);
    public void AddReversePatches(ReversePatchData data) => reversePatches = Add(reversePatches, data);
    public void AddOverridePatches(OverridePatchData data) => overridePatches = Add(overridePatches, data);
    public static HarmonyLib.Patch[] Add(string owner, HarmonyMethod add, HarmonyLib.Patch[] current) =>
        Add(current, new HarmonyLib.Patch(add.method, current.Length, owner, add.priority, add.before, add.after, add.debug.GetValueOrDefault()));
    public static TriedPatchData[] Add(string owner, HarmonyMethod add, TriedPatchData[] current, JAMod mod) => Add(current, new TriedPatchData(add, current.Length, owner, mod));

    private static T[] Add<T>(T[] current, T add) {
        T[] result = new T[current.Length + 1];
        current.CopyTo(result, 0);
        result[current.Length] = add;
        return result;
    }

    public void RemoveTryPrefix(string id) {
        tryPrefixes = id == "*" ? [] : tryPrefixes.Where(p => p.owner != id).ToArray();
    }

    public void RemoveTryPostfix(string id) {
        tryPostfixes = id == "*" ? [] : tryPostfixes.Where(p => p.owner != id).ToArray();
    }

    public void RemoveReplace(string id) {
        replaces = id == "*" ? [] : replaces.Where(p => p.owner != id).ToArray();
    }

    public void RemoveRemove(string id) {
        removes = id == "*" ? [] : removes.Where(p => p.owner != id).ToArray();
    }

    public void RemoveOverridePatch(string id) {
        overridePatches = id == "*" ? [] : overridePatches.Where(p => p.ID != id).ToArray();
    }

    public void RemovePatch(MethodInfo patch) {
        replaces = replaces.Where(p => p.PatchMethod != patch).ToArray();
        removes = removes.Where(p => p.PatchMethod != patch).ToArray();
        tryPrefixes = tryPrefixes.Where(p => p.PatchMethod != patch).ToArray();
        tryPostfixes = tryPostfixes.Where(p => p.PatchMethod != patch).ToArray();
        reversePatches = reversePatches.Where(p => p.PatchMethod != patch).ToArray();
        overridePatches = overridePatches.Where(p => p.PatchMethod != patch).ToArray();
    }

    public bool IsDebug() => tryPrefixes.Any(IsDebug) || tryPostfixes.Any(IsDebug) || replaces.Any(IsDebug) || removes.Any(IsDebug) || reversePatches.Any(patch => patch.Debug) || overridePatches.Any(patch => patch.Debug);
    private static bool IsDebug<T>(T patch) where T : HarmonyLib.Patch => patch.debug;
}