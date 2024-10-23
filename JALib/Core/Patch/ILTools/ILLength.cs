using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILLength(ILCode array) : ILCode {
    public readonly ILCode Array = array;

    public override Type ReturnType => typeof(int);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Array.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Ldlen);
    }

    public override string ToString() => $"{Array}.Length";
}