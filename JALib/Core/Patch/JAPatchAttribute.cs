using System.Reflection;

namespace JALib.Core.Patch;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class JAPatchAttribute : JAPatchBaseAttribute {
    internal bool Disable;
    internal PatchType PatchType;

    public JAPatchAttribute(string @class, string methodName, PatchType patchType, bool disable) {
        Class = @class;
        MethodName = methodName;
        PatchType = patchType;
        Disable = disable;
    }

    public JAPatchAttribute(Type @class, string methodName, PatchType patchType, bool disable) {
        ClassType = @class;
        MethodName = methodName;
        PatchType = patchType;
        Disable = disable;
    }

    public JAPatchAttribute(MethodBase method, PatchType patchType, bool disable) {
        MethodBase = method;
        PatchType = patchType;
        Disable = disable;
    }

    public JAPatchAttribute(Delegate @delegate, PatchType patchType, bool disable) : this(@delegate.Method, patchType, disable) {
    }
}