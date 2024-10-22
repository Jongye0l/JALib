using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILReturn(ILCode value) : ILCode {
    public ILCode Value = value;

    public override Type ReturnType => typeof(void);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        if(Value != null)
            foreach(CodeInstruction instruction in Value.Load(generator))
                yield return instruction;
        yield return new CodeInstruction(OpCodes.Ret);
    }

    public override string ToString() => $"return {Value}";
}