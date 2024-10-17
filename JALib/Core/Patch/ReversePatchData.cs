using System.Reflection;

namespace JALib.Core.Patch;

public class ReversePatchData {
    public MethodBase original;
    public MethodInfo patchMethod;
    public bool debug;
    public JAReversePatchAttribute attribute;
    public JAMod mod;
}