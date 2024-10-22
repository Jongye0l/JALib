using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Get;

public class ILParameterGet(ILParameter parameter) : ILCode {
    public readonly ILParameter Parameter = parameter;

    public override Type ReturnType => throw new NotSupportedException(); // TODO: Support This

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return Parameter.Index switch {
            0 => new CodeInstruction(OpCodes.Ldarg_0),
            1 => new CodeInstruction(OpCodes.Ldarg_1),
            2 => new CodeInstruction(OpCodes.Ldarg_2),
            3 => new CodeInstruction(OpCodes.Ldarg_3),
            < 256 => new CodeInstruction(OpCodes.Ldarg_S, (byte) Parameter.Index),
            _ => new CodeInstruction(OpCodes.Ldarg, Parameter.Index)
        };
    }
    public override string ToString() => parameter.Name;
}