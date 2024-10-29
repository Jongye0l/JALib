using System.Reflection;

namespace JALib.Core.Patch;

[AttributeUsage(AttributeTargets.Method)]
public class JAReversePatchAttribute : JAPatchBaseAttribute {
    internal ReversePatchType PatchType;
    public bool TryCatchChildren = true;
    internal ReversePatchData Data;

    public JAReversePatchAttribute(string @class, string methodName, ReversePatchType patchType) {
        Class = @class;
        MethodName = methodName;
        PatchType = patchType;
    }

    public JAReversePatchAttribute(Type @class, string methodName, ReversePatchType patchType) {
        ClassType = @class;
        MethodName = methodName;
        PatchType = patchType;
    }

    public JAReversePatchAttribute(MethodBase method, ReversePatchType patchType) {
        MethodBase = method;
        PatchType = patchType;
    }

    public JAReversePatchAttribute(Delegate @delegate, ReversePatchType patchType) : this(@delegate.Method, patchType) {
    }
}