using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Value;

public class ILString(string value) : ILCode {
    public readonly string Value = value;

    public override Type ReturnType => typeof(string);

    public static implicit operator string(ILString ilString) => ilString.Value;

    public static implicit operator ILString(string value) => new(value);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return new CodeInstruction(OpCodes.Ldstr, Value);
    }

    public override string ToString() => $"\"{Value}\"";
}