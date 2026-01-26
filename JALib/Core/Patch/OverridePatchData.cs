using System.Reflection;

namespace JALib.Core.Patch;

public class OverridePatchData {
    public readonly Type TargetType;
    public readonly MethodInfo PatchMethod;
    public readonly bool IgnoreBasePatch;
    public readonly bool Debug;
    public readonly bool TryCatch;
    public readonly string ID;
    public readonly JAMod Mod;

    internal OverridePatchData(Type targetType, MethodInfo patchMethod, bool ignoreBasePatch, bool debug, bool tryCatch, string id, JAMod mod) {
        TargetType = targetType;
        PatchMethod = patchMethod;
        IgnoreBasePatch = ignoreBasePatch;
        Debug = debug;
        TryCatch = tryCatch;
        ID = id;
        Mod = mod;
    }

    internal OverridePatchData(MethodInfo patchMethod, JAOverridePatchAttribute attribute, JAMod mod) {
        TargetType = attribute.targetType;
        PatchMethod = patchMethod;
        IgnoreBasePatch = attribute.IgnoreBasePatch;
        Debug = attribute.Debug;
        TryCatch = attribute.TryingCatch;
        ID = attribute.PatchId;
        Mod = mod;
    }
}