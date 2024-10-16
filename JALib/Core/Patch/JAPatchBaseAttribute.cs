using System.Reflection;

namespace JALib.Core.Patch;

public abstract class JAPatchBaseAttribute : Attribute {
    internal static int GetCurrentVersion => GCNS.releaseNumber;
    internal string PatchId => Method.DeclaringType.FullName + "." + Method.Name;
    internal string Class;
    internal Type ClassType;
    internal string MethodName;
    internal MethodBase MethodBase;
    public int MinVersion = GetCurrentVersion;
    public int MaxVersion = GetCurrentVersion;
    public string[] ArgumentTypes;
    public Type[] ArgumentTypesType;
    public string[] GenericName;
    public Type[] GenericType;
    public bool TryingCatch = true;
    internal MethodInfo Method;
    
}