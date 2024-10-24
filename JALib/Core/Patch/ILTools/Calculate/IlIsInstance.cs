using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Calculate;

public class IlIsInstance(ILCode value, Type type) : ILCode {
    public readonly ILCode Value = value;
    public readonly Type Type = type;

    public override Type ReturnType => typeof(int);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Value.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Isinst, Type);
    }

    public override string ToString() => $"{Value} is {Type.Name}";
}