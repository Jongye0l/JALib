using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILThrow(ILCode value) : ILCode {
    public ILCode Value = value;

    public override Type ReturnType => typeof(void);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Value.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Throw);
    }

    public override string ToString() => $"throw {Value}";
}
