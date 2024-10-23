using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Core.Patch.ILTools.Value;

namespace JALib.Core.Patch.ILTools;

public abstract class ILCode : ILTool {
    public List<ExceptionBlock> Blocks = [];
    public List<Label> Labels = [];

    public abstract Type ReturnType { get; }

    public abstract IEnumerable<CodeInstruction> Load(ILGenerator generator);

    public abstract override string ToString();

    public static implicit operator ILCode(int value) => new ILInt(value);
    public static implicit operator ILCode(long value) => new ILLong(value);
    public static implicit operator ILCode(float value) => new ILFloat(value);
    public static implicit operator ILCode(double value) => new ILDouble(value);
    public static implicit operator ILCode(string value) => new ILString(value);
}