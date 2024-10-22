using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Value;

public class ILDouble(double value) : ILCode {
    public readonly double Value = value;

    public override Type ReturnType => typeof(double);

    public static implicit operator double(ILDouble ilDouble) => ilDouble.Value;

    public static implicit operator ILDouble(double value) => new(value);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return new CodeInstruction(OpCodes.Ldc_R8, Value);
    }

    public override string ToString() => Value.ToString();
}