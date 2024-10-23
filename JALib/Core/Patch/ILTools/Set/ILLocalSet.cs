using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Set;

public class ILLocalSet(ILLocal local, ILCode value) : ILCode {
    public readonly ILLocal Local = local;
    public readonly ILCode Value = value;

    public override Type ReturnType => Local.LocalBuilder.LocalType;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Value.Load(generator)) yield return instruction;
        Local.Setup(generator);
        yield return new CodeInstruction(OpCodes.Ldloc, Local.LocalBuilder);
    }

    public override string ToString() => $"v{Local.Index} = {Value}";
}