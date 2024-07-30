using System;

namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class DataIncludeAttribute : DataAttribute {
    public DataIncludeAttribute() {
    }

    public DataIncludeAttribute(uint version) {
        MinimumVersion = version;
        MaximumVersion = version;
    }

    public DataIncludeAttribute(uint minVersion, uint maxVersion) {
        MinimumVersion = minVersion;
        MaximumVersion = maxVersion;
    }
}