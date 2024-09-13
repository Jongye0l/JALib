using System;

namespace JALib.Tools.ByteTool;

public abstract class DataAttribute : Attribute {
    public uint MinimumVersion = uint.MinValue;
    public uint MaximumVersion = uint.MaxValue;
    public Func<uint?, bool> Condition;

    public bool CheckCondition(uint? version) {
        return !(version < MinimumVersion || version > MaximumVersion) && (Condition == null || Condition(version));
    }
}