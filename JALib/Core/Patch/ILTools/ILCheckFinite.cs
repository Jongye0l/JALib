using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILCheckFinite(ILCode value) : ILCode {
    public readonly ILCode Value = value;

    public override Type ReturnType => typeof(bool);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Value.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Ckfinite);
    }

    public override string ToString() => $"{Value.ReturnType.Name} checkFiniteVar = {Value};\n" +
                                         $"if({{Value.ReturnType.Name}}.IsInfinity(checkFiniteVar) || {{Value.ReturnType.Name}}.IsNaN(checkFiniteVar)) throw new OverflowException(\"Value is not finite.\");";
}