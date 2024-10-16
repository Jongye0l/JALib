using HarmonyLib;

namespace JALib.Core.Patch;

public class TriedPatchData(HarmonyMethod method, int index, string owner, JAMod mod) : HarmonyLib.Patch(method, index, owner) {
    public JAMod mod = mod;
}