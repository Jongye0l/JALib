using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILNewArr(ILCode length, Type elementType) : ILCode {
    public readonly ILCode Length = length;
    public readonly Type ElementType = elementType;

    public override Type ReturnType => ElementType.MakeArrayType();

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Length.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Newarr, ElementType);
    }

    public override string ToString() => $"new {ElementType.Name}[{Length}]";
}