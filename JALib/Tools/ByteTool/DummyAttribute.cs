using System;

namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DummyAttribute : DataAttribute {
    public int Count;

    public DummyAttribute(int count) {
        Count = count;
    }

    public DummyAttribute(int count, int version) {
        Count = count;
        MinimumVersion = version;
        MaximumVersion = version;
    }

    public DummyAttribute(int count, int minVersion, int maxVersion) {
        Count = count;
        MinimumVersion = minVersion;
        MaximumVersion = maxVersion;
    }
}