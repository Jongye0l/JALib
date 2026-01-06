using System.Reflection;
using System.Text;
using HarmonyLib;

namespace JALib.Core.Patch;

class JAPatchException : Exception {
    private string patchInfoString;
    
    public JAPatchException(MethodBase targetMethod, PatchInfo patchInfo, JAPatchInfo jaPatchInfo, Exception inner) : base("An error occurred during the patching process", inner) {
        patchInfoString = PatchInfoText(targetMethod, patchInfo, jaPatchInfo);
    }

    public static string PatchInfoText(MethodBase targetMethod, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        StringBuilder sb = new();
        sb.Append(nameof(JAPatchException)).Append(": ").Append("An error occurred during the patching process\n");
        sb.Append("Patching method: ").Append(targetMethod.FullDescription()).Append('\n');
        AppendPatches("Prefix", sb, patchInfo.prefixes);
        AppendPatches("TryPrefixes", sb, jaPatchInfo.tryPrefixes);
        AppendPatches("Postfixes", sb, patchInfo.postfixes);
        AppendPatches("TryPostfixes", sb, jaPatchInfo.tryPostfixes);
        AppendPatches("Transpilers", sb, patchInfo.transpilers);
        AppendPatches("Finalizers", sb, patchInfo.finalizers);
        AppendPatches("Replaces", sb, jaPatchInfo.replaces);
        AppendPatches("Removes", sb, jaPatchInfo.removes);
        return sb.ToString();
    }

    private static void AppendPatches(string title, StringBuilder sb, HarmonyLib.Patch[] patches) {
        if(patches.Length == 0) return;
        sb.Append("  ").Append(title).Append(":\n");
        foreach(HarmonyLib.Patch prefix in patches) 
            sb.Append("    ").Append(prefix.PatchMethod.FullDescription()).Append("  [").Append(prefix.owner).Append("]\n");
    }

    private static void AppendPatches(string title, StringBuilder sb, TriedPatchData[] patches) {
        if(patches.Length == 0) return;
        sb.Append("  ").Append(title).Append(":\n");
        foreach(TriedPatchData prefix in patches) 
            sb.Append("    ").Append(prefix.PatchMethod.FullDescription()).Append("  [").Append(prefix.mod.Name).Append("]\n");
    }

    public override string ToString() => patchInfoString + "\n" + InnerException!;
}