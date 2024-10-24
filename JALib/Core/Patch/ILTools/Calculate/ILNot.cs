using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch.ILTools.Calculate;

public class ILNot : ILCode {
    public readonly ILCode Code;

    public ILNot(ILCode code) {
        if(!SimpleReflect.IsInteger(code.ReturnType)) throw new InvalidProgramException("code Type is not integer");
        Code = code;
    }

    public override Type ReturnType => Code.ReturnType;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Code.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Not);
    }

    public override string ToString() => $"~{Code}";
}