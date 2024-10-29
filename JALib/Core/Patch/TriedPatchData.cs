using System.Reflection;
using HarmonyLib;

namespace JALib.Core.Patch;

public class TriedPatchData : HarmonyLib.Patch {
    public JAMod mod;

    public TriedPatchData(MethodInfo patch, int index, string owner, int priority, string[] before, string[] after, bool debug, JAMod mod) : base(patch, index, owner, priority, before, after, debug) {
        this.mod = mod;
    }

    public TriedPatchData(HarmonyMethod method, int index, string owner, JAMod mod) : base(method.method, index, owner, method.priority, method.before, method.after, method.debug.GetValueOrDefault()) {
        this.mod = mod;
    }
}