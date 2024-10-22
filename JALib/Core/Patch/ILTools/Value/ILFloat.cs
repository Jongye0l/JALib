using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Value;

public class ILFloat(float value) : ILCode {
    public readonly float Value = value;

    public override Type ReturnType => typeof(float);

    public static implicit operator float(ILFloat ilFloat) => ilFloat.Value;

    public static implicit operator ILFloat(float value) => new(value);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return new CodeInstruction(OpCodes.Ldc_R4, Value);
    }

    public override string ToString() => Value + "f";
}