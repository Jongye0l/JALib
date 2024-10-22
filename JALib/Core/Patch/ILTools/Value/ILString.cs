using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Value;

public class ILString(string value) : ILCode {
    public string Value = value;

    public override Type ReturnType => typeof(string);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return new CodeInstruction(OpCodes.Ldstr, Value);
    }

    public override string ToString() => $"\"{Value}\"";
}