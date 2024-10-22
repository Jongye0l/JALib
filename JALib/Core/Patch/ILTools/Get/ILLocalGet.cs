using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Get;

public class ILLocalGet(ILLocal local) : ILCode {
    public readonly ILLocal Local = local;

    public override Type ReturnType => Local.type;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        Local.Setup(generator);
        yield return new CodeInstruction(OpCodes.Ldloc, Local.LocalBuilder);
    }

    public override string ToString() => $"v{Local.Index}";
}