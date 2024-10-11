namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Class)]
public class VersionAttribute : Attribute {
    public uint Version;

    public VersionAttribute(uint version) {
        Version = version;
    }
}