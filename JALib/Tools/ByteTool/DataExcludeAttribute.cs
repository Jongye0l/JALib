namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class DataExcludeAttribute : DataAttribute {
    public DataExcludeAttribute() {
    }

    public DataExcludeAttribute(uint version) {
        MinimumVersion = version;
        MaximumVersion = version;
    }

    public DataExcludeAttribute(uint minVersion, uint maxVersion) {
        MinimumVersion = minVersion;
        MaximumVersion = maxVersion;
    }
}