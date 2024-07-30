﻿using System;

namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class DataExcludeAttribute : DataAttribute {
    public DataExcludeAttribute() {
    }

    public DataExcludeAttribute(int version) {
        MinimumVersion = version;
        MaximumVersion = version;
    }

    public DataExcludeAttribute(int minVersion, int maxVersion) {
        MinimumVersion = minVersion;
        MaximumVersion = maxVersion;
    }
}