namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class CastAttribute : DataAttribute {
    public Type Type;
    public FirstCast FirstCast = FirstCast.Explicit;

    public CastAttribute(Type type) {
        Type = type;
    }

    public CastAttribute(Type type, FirstCast firstCast) {
        Type = type;
        FirstCast = firstCast;
    }
}