using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools.Get;

public class ILArrayGet(ILCode array, ILCode index) : ILCode {
    public readonly ILCode Array = array;
    public readonly ILCode Index = index;

    public override Type ReturnType => Array.ReturnType.GetElementType();

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Array.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Index.Load(generator)) yield return instruction;
        if(ReturnType == typeof(sbyte)) yield return new CodeInstruction(OpCodes.Ldelem_I1);
        else if(ReturnType == typeof(byte)) yield return new CodeInstruction(OpCodes.Ldelem_U1);
        else if(ReturnType == typeof(short)) yield return new CodeInstruction(OpCodes.Ldelem_I2);
        else if(ReturnType == typeof(ushort)) yield return new CodeInstruction(OpCodes.Ldelem_U2);
        else if(ReturnType == typeof(int)) yield return new CodeInstruction(OpCodes.Ldelem_I4);
        else if(ReturnType == typeof(uint)) yield return new CodeInstruction(OpCodes.Ldelem_U4);
        else if(ReturnType == typeof(long) || ReturnType == typeof(ulong)) yield return new CodeInstruction(OpCodes.Ldelem_I8);
        else if(ReturnType == typeof(float)) yield return new CodeInstruction(OpCodes.Ldelem_R4);
        else if(ReturnType == typeof(double)) yield return new CodeInstruction(OpCodes.Ldelem_R8);
        else if(ReturnType.IsValueType) yield return new CodeInstruction(OpCodes.Ldelem, ReturnType);
        else yield return new CodeInstruction(OpCodes.Ldelem_Ref);
    }

    public override string ToString() => $"{Array}[{Index}]";
}