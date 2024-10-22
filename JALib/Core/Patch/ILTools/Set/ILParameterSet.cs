using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Set;

public class ILParameterSet(ILParameter parameter) : ILCode {
    public readonly ILParameter Parameter = parameter;
    public readonly ILCode Value;

    public override Type ReturnType => typeof(void);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Value.Load(generator)) yield return instruction;
        yield return Parameter.Index switch {
            < 256 => new CodeInstruction(OpCodes.Starg_S, (byte) Parameter.Index),
            _ => new CodeInstruction(OpCodes.Starg, Parameter.Index)
        };
    }

    public override string ToString() => $"{parameter.Name} = {Value}";
}