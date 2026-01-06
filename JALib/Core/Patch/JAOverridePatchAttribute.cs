namespace JALib.Core.Patch;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class JAOverridePatchAttribute : JAPatchBaseAttribute {
    public bool IgnoreBasePatch = true;
    public Type targetType;
    public string targetTypeName;
    public bool checkType = true;

    public JAOverridePatchAttribute(string @class, string methodName) {
        Class = @class;
        MethodName = methodName;
    }

    public JAOverridePatchAttribute(Type @class, string methodName) {
        ClassType = @class;
        MethodName = methodName;
    }

    public JAOverridePatchAttribute(string @class) => Class = @class;
    public JAOverridePatchAttribute(Type @class) => ClassType = @class;
    public JAOverridePatchAttribute() {
    }
    
    protected override string GetPatchTypeString() => "Override";
}