using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Set;

public class ILArraySet(ILCode array, ILCode index, ILCode value) : ILCode {
    public readonly ILCode Array = array;
    public readonly ILCode Index = index;
    public readonly ILCode Value = value;

    public override Type ReturnType => Value.ReturnType;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Array.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Index.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Value.Load(generator)) yield return instruction;
        if(ReturnType == typeof(sbyte)) yield return new CodeInstruction(OpCodes.Stelem_I1);
        else if(ReturnType == typeof(byte)) yield return new CodeInstruction(OpCodes.Stelem_I1);
        else if(ReturnType == typeof(short)) yield return new CodeInstruction(OpCodes.Stelem_I2);
        else if(ReturnType == typeof(ushort)) yield return new CodeInstruction(OpCodes.Stelem_I2);
        else if(ReturnType == typeof(int)) yield return new CodeInstruction(OpCodes.Stelem_I4);
        else if(ReturnType == typeof(uint)) yield return new CodeInstruction(OpCodes.Stelem_I4);
        else if(ReturnType == typeof(long)) yield return new CodeInstruction(OpCodes.Stelem_I8);
        else if(ReturnType == typeof(ulong)) yield return new CodeInstruction(OpCodes.Stelem_I8);
        else if(ReturnType == typeof(float)) yield return new CodeInstruction(OpCodes.Stelem_R4);
        else if(ReturnType == typeof(double)) yield return new CodeInstruction(OpCodes.Stelem_R8);
        else if(ReturnType.IsValueType) yield return new CodeInstruction(OpCodes.Stelem, ReturnType);
        else yield return new CodeInstruction(OpCodes.Stelem_Ref);
    }

    public override string ToString() => $"{Array}[{Index}] = {Value}";
}