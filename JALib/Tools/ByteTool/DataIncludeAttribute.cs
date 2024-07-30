using System;

namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class DataIncludeAttribute : DataAttribute {
    public DataIncludeAttribute() {
    }

    public DataIncludeAttribute(int version) {
        MinimumVersion = version;
        MaximumVersion = version;
    }

    public DataIncludeAttribute(int minVersion, int maxVersion) {
        MinimumVersion = minVersion;
        MaximumVersion = maxVersion;
    }
}