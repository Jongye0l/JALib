using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Get;

public class ILArrayPointerGet(ILCode array, ILCode index) : ILCode {
    public readonly ILCode Array = array;
    public readonly ILCode Index = index;

    public override Type ReturnType => Array.ReturnType.GetElementType();

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Array.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Index.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Ldelem, ReturnType);
    }

    public override string ToString() => $"&{Array}[{Index}]";
}