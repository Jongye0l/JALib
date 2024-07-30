using System;

namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
public class DeclearingAttribute : DataAttribute {
    public bool Declearing;

    public DeclearingAttribute(bool declearing) {
        Declearing = declearing;
    }
}