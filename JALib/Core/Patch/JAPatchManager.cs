using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch;

public static class JAPatchManager {
    private static Dictionary<MethodBase, byte[]> _harmonyState;
    
    public static PatchData GetPatchData(MethodBase method) {
        PatchData patchData = new();
        lock(JAPatcher.HarmonyLocker) {
            PatchInfo patchInfo = JAPatcher.GetPatchInfo(method);
            if(patchInfo != null) {
                MethodBase[] methodBases = new MethodBase[patchInfo.prefixes.Length];
                for(int i = 0; i < patchInfo.prefixes.Length; i++) methodBases[i] = patchInfo.prefixes[i].PatchMethod;
                patchData.Prefixes = methodBases;
                methodBases = new MethodBase[patchInfo.postfixes.Length];
                for(int i = 0; i < patchInfo.postfixes.Length; i++) methodBases[i] = patchInfo.postfixes[i].PatchMethod;
                patchData.Postfixes = methodBases;
                methodBases = new MethodBase[patchInfo.transpilers.Length];
                for(int i = 0; i < patchInfo.transpilers.Length; i++) methodBases[i] = patchInfo.transpilers[i].PatchMethod;
                patchData.Transpilers = methodBases;
                methodBases = new MethodBase[patchInfo.finalizers.Length];
                for(int i = 0; i < patchInfo.finalizers.Length; i++) methodBases[i] = patchInfo.finalizers[i].PatchMethod;
                patchData.Finalizers = methodBases;
            } else patchData.Prefixes = patchData.Postfixes = patchData.Transpilers = patchData.Finalizers = [];
            if(JAPatcher.JaPatches.TryGetValue(method, out JAInternalPatchInfo jaPatchInfo)) {
                MethodBase[] methodBases = new MethodBase[jaPatchInfo.tryPrefixes.Length];
                for(int i = 0; i < jaPatchInfo.tryPrefixes.Length; i++) methodBases[i] = jaPatchInfo.tryPrefixes[i].PatchMethod;
                patchData.TryPrefixes = methodBases;
                methodBases = new MethodBase[jaPatchInfo.tryPostfixes.Length];
                for(int i = 0; i < jaPatchInfo.tryPostfixes.Length; i++) methodBases[i] = jaPatchInfo.tryPostfixes[i].PatchMethod;
                patchData.TryPostfixes = methodBases;
                methodBases = new MethodBase[jaPatchInfo.replaces.Length];
                for(int i = 0; i < jaPatchInfo.replaces.Length; i++) methodBases[i] = jaPatchInfo.replaces[i].PatchMethod;
                patchData.Replaces = methodBases;
                methodBases = new MethodBase[jaPatchInfo.removes.Length];
                for(int i = 0; i < jaPatchInfo.removes.Length; i++) methodBases[i] = jaPatchInfo.removes[i].PatchMethod;
                patchData.Removes = methodBases;
                methodBases = new MethodBase[jaPatchInfo.overridePatches.Length];
                for(int i = 0; i < jaPatchInfo.overridePatches.Length; i++) methodBases[i] = jaPatchInfo.overridePatches[i].PatchMethod;
                patchData.Overrides = methodBases;
            } else patchData.TryPrefixes = patchData.TryPostfixes = patchData.Replaces = patchData.Removes = patchData.Overrides = [];
        }
        return patchData;
    }
    
    public static JAPatchInfo GetPatchInfo(MethodBase method) {
        JAPatchInfo result = new();
        lock(JAPatcher.HarmonyLocker) {
            PatchInfo patchInfo = JAPatcher.GetPatchInfo(method);
            if(patchInfo != null) {
                result.Prefixes = patchInfo.prefixes;
                result.Postfixes = patchInfo.postfixes;
                result.Transpilers = patchInfo.transpilers;
                result.Finalizers = patchInfo.finalizers;
            } else result.Prefixes = result.Postfixes = result.Transpilers = result.Finalizers = [];
            if(JAPatcher.JaPatches.TryGetValue(method, out JAInternalPatchInfo jaPatchInfo)) {
                result.TryPrefixes = jaPatchInfo.tryPrefixes;
                result.TryPostfixes = jaPatchInfo.tryPostfixes;
                result.Replaces = jaPatchInfo.replaces;
                result.Removes = jaPatchInfo.removes;
                result.OverridePatches = jaPatchInfo.overridePatches;
            } else {
                result.TryPrefixes = result.TryPostfixes = [];
                result.Replaces = [];
                result.Removes = [];
                result.OverridePatches = [];
            }
        }
        return result;
    }

    private static Dictionary<MethodBase, byte[]> GetHarmonyPatchState() =>
        _harmonyState ??= typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").GetValue<Dictionary<MethodBase, byte[]>>("state");

    public static IEnumerable<JAPatchInfo> GetPatchInfos() {
        Dictionary<MethodBase, byte[]> harmonyPatch = GetHarmonyPatchState();
        Dictionary<MethodBase, JAInternalPatchInfo> internalPatchInfos = JAPatcher.JaPatches;
        HashSet<MethodBase> methods = new(Math.Max(harmonyPatch.Count, internalPatchInfos.Count));
        foreach(MethodBase methodBase in harmonyPatch.Keys.Concat(internalPatchInfos.Keys)) methods.Add(methodBase);
        return methods.Select(GetPatchInfo);
    }
}