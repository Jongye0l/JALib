using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Get;

public class ILParameterPointerGet(ILParameter parameter) : ILParameterGet(parameter) {

    public override Type ReturnType => base.ReturnType.MakeByRefType();

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return Parameter.Index switch {
            < 256 => new CodeInstruction(OpCodes.Ldarga_S, (byte) Parameter.Index),
            _ => new CodeInstruction(OpCodes.Ldarga, Parameter.Index)
        };
    }

    public override string ToString() => $"&{Parameter.Name}";
}