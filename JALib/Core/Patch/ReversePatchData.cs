using System.Reflection;

namespace JALib.Core.Patch;

public class ReversePatchData {
    public MethodBase Original;
    public MethodInfo PatchMethod;
    public bool Debug;
    public JAReversePatchAttribute Attribute;
    public JAMod Mod;

    internal ReversePatchData() {
    }
}