using System.Reflection;

namespace JALib.Core.Patch;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class JAPatchAttribute : JAPatchBaseAttribute {
    public bool? Disable;
    public PatchType? PatchType;
    public int Priority = -1;
    public string[] Before;
    public string[] After;

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

    public JAPatchAttribute() {
    }

    public JAPatchAttribute(Type @class) {
        ClassType = @class;
    }

    public JAPatchAttribute(string methodName) {
        MethodName = methodName;
    }

    public JAPatchAttribute(PatchType patchType) {
        PatchType = patchType;
    }

    public JAPatchAttribute(bool disable) {
        Disable = disable;
    }
}