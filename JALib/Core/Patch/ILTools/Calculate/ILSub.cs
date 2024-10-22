using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Calculate;

public class ILSub(ILCode left, ILCode right) : ILCalculate(left, right) {

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Left.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Right.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Sub);
    }

    public override string ToString() => $"{Left} - {Right}";
}