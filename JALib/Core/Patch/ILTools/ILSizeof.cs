using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILSizeof : ILCode {
    public readonly Type Type;

    public override Type ReturnType => typeof(int);

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return new CodeInstruction(OpCodes.Sizeof, Type);
    }

    public override string ToString() => $"sizeof({Type.Name})";
}