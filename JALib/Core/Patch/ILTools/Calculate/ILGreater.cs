using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Calculate;

public class ILGreater(ILCode left, ILCode right) : ILCalculate(left, right) {

    public override Type ReturnType => typeof(bool);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Left.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Right.Load(generator)) yield return instruction;
        Type returnType = Left.ReturnType;
        if(returnType == typeof(byte) || returnType == typeof(ushort) || returnType == typeof(uint) || returnType == typeof(ulong)) yield return new CodeInstruction(OpCodes.Cgt_Un);
        else yield return new CodeInstruction(OpCodes.Cgt);
    }

    public override string ToString() => $"{Left} > {Right}";
}