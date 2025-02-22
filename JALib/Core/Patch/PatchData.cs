using System.Reflection;

namespace JALib.Core.Patch;

public class PatchData {
    public MethodBase[] Prefixes;
    public MethodBase[] Postfixes;
    public MethodBase[] TryPrefixes;
    public MethodBase[] TryPostfixes;
    public MethodBase[] Transpilers;
    public MethodBase[] Finalizers;
    public MethodBase[] Replaces;
    public MethodBase[] Removes;
    public MethodBase ReversePatch;

    internal PatchData() {}
}