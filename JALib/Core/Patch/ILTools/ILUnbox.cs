using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILUnbox(ILCode code, Type type) : ILCode {
    public readonly ILCode Code = code;
    public readonly Type Type = type;

    public override Type ReturnType => Type;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Code.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Unbox_Any, Type);
    }

    public override string ToString() => $"({Type.Name}) {Code}";
}