using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Get;

public class ILLocalPointerGet(ILLocal local) : ILLocalGet(local) {
    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        Local.Setup(generator);
        yield return new CodeInstruction(OpCodes.Ldloca, Local.LocalBuilder);
    }

    public override string ToString() => $"&v{Local.Index}";
}