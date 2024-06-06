using System;
using System.Reflection;
using HarmonyLib;

namespace JALib.Core.Patch;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class JAPatchAttribute : Attribute {
    internal string PatchId;
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

    public JAPatchAttribute(string patchId, string @class, string methodName, PatchType patchType, bool disable) {
        PatchId = patchId;
        Class = @class;
        MethodName = methodName;
        PatchType = patchType;
        Disable = disable;
    }

    public JAPatchAttribute(string patchId, Type @class, string methodName, PatchType patchType, bool disable) {
        PatchId = patchId;
        ClassType = @class;
        MethodName = methodName;
        PatchType = patchType;
        Disable = disable;
    }
    
    public JAPatchAttribute(string patchId, MethodBase method, PatchType patchType, bool disable) {
        PatchId = patchId;
        MethodBase = method;
        PatchType = patchType;
        Disable = disable;
    }
    
    public JAPatchAttribute(string patchId, Delegate @delegate, PatchType patchType, bool disable) : this(patchId, @delegate.Method, patchType, disable) {
    }
}