using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch.ILTools.Calculate;

public class ILAnd : ILCalculate {

    public ILAnd(ILCode left, ILCode right) : base(left, right) {
        if(!SimpleReflect.IsInteger(left.ReturnType)) throw new InvalidProgramException("left Type is not integer");
        if(!SimpleReflect.IsInteger(right.ReturnType)) throw new InvalidProgramException("right Type is not integer");
    }

    public override Type ReturnType => typeof(int);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Left.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Right.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.And);
    }

    public override string ToString() => $"{Left} & {Right}";
}