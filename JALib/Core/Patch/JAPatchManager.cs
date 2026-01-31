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
                // Directly extract PatchMethod to avoid array allocation overhead
                patchData.Prefixes = ExtractPatchMethods(patchInfo.prefixes);
                patchData.Postfixes = ExtractPatchMethods(patchInfo.postfixes);
                patchData.Transpilers = ExtractPatchMethods(patchInfo.transpilers);
                patchData.Finalizers = ExtractPatchMethods(patchInfo.finalizers);
            } else patchData.Prefixes = patchData.Postfixes = patchData.Transpilers = patchData.Finalizers = [];
            if(JAPatcher.JaPatches.TryGetValue(method, out JAInternalPatchInfo jaPatchInfo)) {
                patchData.TryPrefixes = ExtractPatchMethods(jaPatchInfo.tryPrefixes);
                patchData.TryPostfixes = ExtractPatchMethods(jaPatchInfo.tryPostfixes);
                patchData.Replaces = ExtractPatchMethods(jaPatchInfo.replaces);
                patchData.Removes = ExtractPatchMethods(jaPatchInfo.removes);
                patchData.Overrides = ExtractPatchMethods(jaPatchInfo.overridePatches);
            } else patchData.TryPrefixes = patchData.TryPostfixes = patchData.Replaces = patchData.Removes = patchData.Overrides = [];
        }
        return patchData;
    }
    
    // Helper method to extract PatchMethod from patch array
    private static MethodBase[] ExtractPatchMethods(Patch[] patches) {
        if(patches == null || patches.Length == 0) return [];
        MethodBase[] methods = new MethodBase[patches.Length];
        for(int i = 0; i < patches.Length; i++) methods[i] = patches[i].PatchMethod;
        return methods;
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