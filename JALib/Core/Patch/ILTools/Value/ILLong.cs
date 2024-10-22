using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Value;

public class ILLong : ILCode {
    public readonly long Value;

    public ILLong(long value) {
        Value = value;
    }

    public override Type ReturnType => typeof(long);

    public static implicit operator long(ILLong ilInt) => ilInt.Value;

    public static implicit operator ILLong(long value) => new(value);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return new CodeInstruction(OpCodes.Ldc_I8, Value);
    }

    public override string ToString() => Value + "L";
}