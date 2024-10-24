using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch.ILTools.Calculate;

public class ILNeg : ILCode {
    public readonly ILCode Code;

    public ILNeg(ILCode code) {
        if(!code.ReturnType.IsNumeric()) throw new InvalidProgramException("code Type is not numeric");
        Code = code;
    }

    public override Type ReturnType => Code.ReturnType;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Code.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Neg);
    }

    public override string ToString() => $"-{Code}";
}