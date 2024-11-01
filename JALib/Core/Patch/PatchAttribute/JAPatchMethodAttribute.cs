using System.Reflection;

namespace JALib.Core.Patch.PatchAttribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
public class JAPatchMethodAttribute : JAPatchBase {
    internal MethodBase method;
    public string MethodName;
    public string[] ArgumentTypes;
    public Type[] ArgumentTypesType;
    public string[] GenericName;
    public Type[] GenericType;

    public JAPatchMethodAttribute() {
    }

    public JAPatchMethodAttribute(string methodName) {
        MethodName = methodName;
    }
}