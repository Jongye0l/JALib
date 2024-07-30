using System;

namespace JALib.Tools.ByteTool;

public abstract class DataAttribute : Attribute {
    public int MinimumVersion = 0;
    public int MaximumVersion = int.MaxValue;
    public Func<int?, bool> Condition;

    public bool CheckCondition(int? version) {
        return (version == null || version < MinimumVersion || version > MaximumVersion) && (Condition == null || Condition(version));
    }
}