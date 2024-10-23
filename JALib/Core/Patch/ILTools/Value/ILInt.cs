using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Value;

public class ILInt(int value) : ILCode {
    public readonly int Value = value;

    public override Type ReturnType => typeof(int);

    public static implicit operator int(ILInt ilInt) => ilInt.Value;

    public static implicit operator ILInt(sbyte value) => new(value);

    public static implicit operator ILInt(int value) => new(value);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return Value switch {
            -1 => new CodeInstruction(OpCodes.Ldc_I4_M1),
            0 => new CodeInstruction(OpCodes.Ldc_I4_0),
            1 => new CodeInstruction(OpCodes.Ldc_I4_1),
            2 => new CodeInstruction(OpCodes.Ldc_I4_2),
            3 => new CodeInstruction(OpCodes.Ldc_I4_3),
            4 => new CodeInstruction(OpCodes.Ldc_I4_4),
            5 => new CodeInstruction(OpCodes.Ldc_I4_5),
            6 => new CodeInstruction(OpCodes.Ldc_I4_6),
            7 => new CodeInstruction(OpCodes.Ldc_I4_7),
            8 => new CodeInstruction(OpCodes.Ldc_I4_8),
            < 128 and > -129 => new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte) Value),
            _ => new CodeInstruction(OpCodes.Ldc_I4, Value)
        };
    }

    public override string ToString() => Value.ToString();
}