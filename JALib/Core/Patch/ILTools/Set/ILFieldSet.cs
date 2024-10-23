using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Set;

public class ILFieldSet(FieldInfo field, ILCode obj, ILCode value) : ILCode {
    public readonly FieldInfo Field = field;
    public readonly ILCode Obj = obj;
    public readonly ILCode Value = value;

    public ILFieldSet(FieldInfo field, ILCode value) : this(field, null, value) {
    }

    public override Type ReturnType => Field.FieldType;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        if(Obj != null) foreach(CodeInstruction instruction in Obj.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Value.Load(generator)) yield return instruction;
        yield return new CodeInstruction(Obj == null ? OpCodes.Stsfld : OpCodes.Stfld, Field);
    }

    public override string ToString() => Field.IsStatic ? $"{Field.DeclaringType}.{Field.Name} = {Value}" : $"{Obj}.{Field.Name} = {Value}";
}