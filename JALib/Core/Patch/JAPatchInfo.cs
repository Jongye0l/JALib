namespace JALib.Core.Patch;

public class JAPatchInfo {
    public HarmonyLib.Patch[] Prefixes;
    public HarmonyLib.Patch[] Postfixes;
    public HarmonyLib.Patch[] Transpilers;
    public HarmonyLib.Patch[] Finalizers;
    public TriedPatchData[] TryPrefixes;
    public TriedPatchData[] TryPostfixes;
    public HarmonyLib.Patch[] Replaces;
    public HarmonyLib.Patch[] Removes;
    public ReversePatchData[] ReversePatches;
    public OverridePatchData[] OverridePatches;

    internal JAPatchInfo() {
    }
}