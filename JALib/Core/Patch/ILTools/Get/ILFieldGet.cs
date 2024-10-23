using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Get;

public class ILFieldGet(FieldInfo field, ILCode obj = null) : ILCode {
    public readonly FieldInfo Field = field;
    public readonly ILCode Obj = obj;

    public override Type ReturnType => Field.FieldType;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        if(Obj != null) foreach(CodeInstruction instruction in Obj.Load(generator)) yield return instruction;
        yield return new CodeInstruction(Obj == null ? OpCodes.Ldsfld : OpCodes.Ldfld, Field);
    }

    public override string ToString() => Field.IsStatic ? $"{Field.DeclaringType}.{Field.Name}" : $"{Obj}.{Field.Name}";
}