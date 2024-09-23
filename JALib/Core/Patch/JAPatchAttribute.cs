using System;
using System.Reflection;
using HarmonyLib;

namespace JALib.Core.Patch;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class JAPatchAttribute : Attribute {
    internal string PatchId => Method.DeclaringType.FullName + "." + Method.Name;
    internal string Class;
    internal Type ClassType;
    internal string MethodName;
    internal MethodBase MethodBase;
    internal PatchType PatchType;
    public int MinVersion = GCNS.releaseNumber;
    public int MaxVersion = GCNS.releaseNumber;
    internal bool Disable;
    public string[] ArgumentTypes;
    public Type[] ArgumentTypesType;
    internal MethodInfo Method;
    internal HarmonyMethod HarmonyMethod;
    internal MethodInfo Patch;
    public string GenericName;
    public Type GenericType;
    public bool TryingCatch = true;

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