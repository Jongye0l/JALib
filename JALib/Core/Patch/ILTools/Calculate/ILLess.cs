using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch.ILTools.Calculate;

public class ILLess : ILCalculate {

    public ILLess(ILCode left, ILCode right) : base(left, right) {
        if(!left.ReturnType.IsNumeric()) throw new InvalidProgramException("left Type is not number");
        if(!right.ReturnType.IsNumeric()) throw new InvalidProgramException("right Type is not number");
    }

    public override Type ReturnType => typeof(int);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Left.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Right.Load(generator)) yield return instruction;
        Type returnType = Left.ReturnType;
        if(returnType == typeof(byte) || returnType == typeof(ushort) || returnType == typeof(uint) || returnType == typeof(ulong)) yield return new CodeInstruction(OpCodes.Clt_Un);
        else yield return new CodeInstruction(OpCodes.Clt);
    }

    public override string ToString() => $"{Left} < {Right}";
    
}