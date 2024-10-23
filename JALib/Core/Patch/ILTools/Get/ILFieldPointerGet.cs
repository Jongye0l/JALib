using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Get;

public class ILFieldPointerGet : ILFieldGet {

    public ILFieldPointerGet(FieldInfo field) : base(field) {
    }

    public ILFieldPointerGet(FieldInfo field, ILCode obj) : base(field, obj) {
    }

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        if(Obj != null) foreach(CodeInstruction instruction in Obj.Load(generator)) yield return instruction;
        yield return new CodeInstruction(OpCodes.Ldflda, Field);
    }

    public override string ToString() => Field.IsStatic ? $"&{Field.DeclaringType}.{Field.Name}" : $"&{Obj}.{Field.Name}";
}