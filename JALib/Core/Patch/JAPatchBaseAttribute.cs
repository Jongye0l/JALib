using System.Reflection;
using JALib.Core.Patch.PatchAttribute;

namespace JALib.Core.Patch;

public abstract class JAPatchBaseAttribute : JAPatchBase {
    internal string PatchId => Method.DeclaringType.FullName + "." + Method.Name;
    public string Class;
    public Type ClassType;
    public string MethodName;
    public MethodBase MethodBase;
    public string[] ArgumentTypes;
    public Type[] ArgumentTypesType;
    public string[] GenericName;
    public Type[] GenericType;
    public bool TryingCatch = true;
    internal MethodInfo Method;
    public bool Debug;
}