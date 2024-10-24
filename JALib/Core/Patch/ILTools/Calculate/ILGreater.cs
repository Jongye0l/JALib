using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch.ILTools.Calculate;

// TODO: Support other types
public class ILGreater : ILCalculate {

    public ILGreater(ILCode left, ILCode right) : base(left, right) {
        if(!left.ReturnType.IsNumeric()) throw new InvalidProgramException("left Type is not number");
        if(!right.ReturnType.IsNumeric()) throw new InvalidProgramException("right Type is not number");
    }

    public override Type ReturnType => typeof(int);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Left.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Right.Load(generator)) yield return instruction;
        Type returnType = Left.ReturnType;
        if(returnType == typeof(byte) || returnType == typeof(ushort) || returnType == typeof(uint) || returnType == typeof(ulong)) yield return new CodeInstruction(OpCodes.Cgt_Un);
        else yield return new CodeInstruction(OpCodes.Cgt);
    }

    public override string ToString() => $"{Left} > {Right}";
}