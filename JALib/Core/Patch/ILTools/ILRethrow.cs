using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILRethrow : ILCode {
    public override Type ReturnType => typeof(void);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return new CodeInstruction(OpCodes.Rethrow);
    }

    public override string ToString() => "throw";
    
}