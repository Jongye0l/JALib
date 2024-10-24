using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch.ILTools.Calculate;

public class ILShr : ILCalculate {

    public ILShr(ILCode left, ILCode right) : base(left, right) {
        if(!SimpleReflect.IsInteger(left.ReturnType)) throw new InvalidProgramException("left Type is not integer");
        if(!SimpleReflect.IsInteger(right.ReturnType)) throw new InvalidProgramException("right Type is not integer");
    }

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Left.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Right.Load(generator)) yield return instruction;
        yield return new CodeInstruction(ReturnType.IsUnsigned() ? OpCodes.Shr_Un : OpCodes.Shr);
    }

    public override string ToString() => $"{Left} >> {Right}";
}