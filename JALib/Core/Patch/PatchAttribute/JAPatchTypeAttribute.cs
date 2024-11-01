namespace JALib.Core.Patch.PatchAttribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
public class JAPatchTypeAttribute : JAPatchBase {
    internal Type type;
    public string name;
    public string[] genericArguments;
    public Type[] genericArgumentTypes;

    public JAPatchTypeAttribute() {
    }

    public JAPatchTypeAttribute(Type type) {
        this.type = type;
    }

    public JAPatchTypeAttribute(string name) {
        this.name = name;
    }
}