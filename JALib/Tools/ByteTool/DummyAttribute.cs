using System;

namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DummyAttribute : DataAttribute {
    public int Count;

    public DummyAttribute(int count) {
        Count = count;
    }

    public DummyAttribute(int count, uint version) {
        Count = count;
        MinimumVersion = version;
        MaximumVersion = version;
    }

    public DummyAttribute(int count, uint minVersion, uint maxVersion) {
        Count = count;
        MinimumVersion = minVersion;
        MaximumVersion = maxVersion;
    }
}