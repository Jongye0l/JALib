using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILIf : ILCode {
    public readonly ILCode Condition;
    public readonly ILCode IfTrue;
    public readonly ILCode IfFalse;

    public ILIf(ILCode condition, ILCode ifTrue, ILCode ifFalse) {
        Condition = condition;
        IfTrue = ifTrue;
        IfFalse = ifFalse;
    }

    public override Type ReturnType => IfTrue.ReturnType;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        Label end = generator.DefineLabel();
        foreach(CodeInstruction instruction in Condition.Load(generator)) yield return instruction;
        if(IfTrue == null) {
            yield return new CodeInstruction(OpCodes.Brtrue, end);
            foreach(CodeInstruction instruction in IfFalse.Load(generator)) yield return instruction;
        } else if(IfFalse == null) {
            yield return new CodeInstruction(OpCodes.Brfalse, end);
            foreach(CodeInstruction instruction in IfTrue.Load(generator)) yield return instruction;
        } else {
            Label label = generator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse, label);
            foreach(CodeInstruction instruction in IfTrue.Load(generator)) yield return instruction;
            yield return new CodeInstruction(OpCodes.Br, end);
            yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
            foreach(CodeInstruction instruction in IfFalse.Load(generator)) yield return instruction;
        }
        yield return new CodeInstruction(OpCodes.Nop).WithLabels(end);
    }

    public override string ToString() {
        if(IfTrue == null) return $"if(!{Condition}) {{\n\t{IfFalse}\n}}";
        if(IfFalse == null) return $"if({Condition}) {{\n\t{IfTrue}\n}}";
        return IfTrue.ReturnType == typeof(void) ? $"if({Condition}) {{\n\t{IfTrue}\n}} else {{\n\t{IfFalse}\n}}" : $"{Condition} ? {IfTrue} : {IfFalse}";
    }
}