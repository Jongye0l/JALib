using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILCast : ILCode {
    public readonly ILCode Code;
    public readonly Type Type;

    public ILCast(ILCode code, Type type) {
        Code = code;
        Type = type;
    }
    public override Type ReturnType => Type;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Code.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Castclass, Type);
    }

    public override string ToString() => $"({Type.Name}) {Code}";
}