using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILConvert : ILCode {
    public readonly ILCode Code;
    public readonly Type Type;

    public ILConvert(ILCode code, Type type) {
        Code = code;
        Type = type;
    }
    public override Type ReturnType => Type;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Code.Load(generator)) yield return instruction;
        if(Type == typeof(sbyte)) yield return new CodeInstruction(OpCodes.Conv_I1);
        else if(Type == typeof(byte)) yield return new CodeInstruction(OpCodes.Conv_U1);
        else if(Type == typeof(short)) yield return new CodeInstruction(OpCodes.Conv_I2);
        else if(Type == typeof(ushort)) yield return new CodeInstruction(OpCodes.Conv_U2);
        else if(Type == typeof(int)) yield return new CodeInstruction(OpCodes.Conv_I4);
        else if(Type == typeof(uint)) yield return new CodeInstruction(OpCodes.Conv_U4);
        else if(Type == typeof(long)) yield return new CodeInstruction(OpCodes.Conv_I8);
        else if(Type == typeof(ulong)) yield return new CodeInstruction(OpCodes.Conv_U8);
        else if(Type == typeof(float)) yield return new CodeInstruction(OpCodes.Conv_R4);
        else if(Type == typeof(double)) yield return new CodeInstruction(OpCodes.Conv_R8);
        else if(Type == typeof(IntPtr)) yield return new CodeInstruction(OpCodes.Conv_I);
        else if(Type == typeof(UIntPtr)) yield return new CodeInstruction(OpCodes.Conv_U);
        else if(Type.IsValueType) yield return new CodeInstruction(OpCodes.Unbox_Any, Type);
        else yield return new CodeInstruction(OpCodes.Castclass, Type);
    }

    public override string ToString() => $"({Type.Name}) {Code}";
}