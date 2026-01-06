using System.Reflection;
using JALib.Tools;

namespace JALib.Core.Patch;

public abstract class JAPatchBaseAttribute : Attribute {
    internal string PatchId => "JAPatch: " + Method.DeclaringType.FullName + "." + Method.Name + "(" + GetPatchTypeString() + ")";
    internal string Class;
    internal Type ClassType;
    internal string MethodName;
    internal MethodBase MethodBase;
    public int MinVersion = VersionControl.releaseNumber;
    public int MaxVersion = VersionControl.releaseNumber;
    public string[] ArgumentTypes;
    public Type[] ArgumentTypesType;
    public string[] GenericName;
    public Type[] GenericType;
    public bool TryingCatch = true;
    internal MethodInfo Method;
    public bool Debug;

    protected abstract string GetPatchTypeString();
}