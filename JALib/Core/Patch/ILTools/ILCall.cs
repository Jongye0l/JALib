using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILCall : ILCode {
    public readonly MethodInfo MethodInfo;
    public readonly ILCode[] Variables;

    public ILCall(string method, params ILCode[] variables) {

    }

    public ILCall(MethodInfo method, params ILCode[] variables) {
        MethodInfo = method;
        Variables = variables;
    }

    public ILCall(MethodInfo method, ConcurrentStack<ILCode> stack) {
        MethodInfo = method;
        Variables = new ILCode[MethodInfo.GetParameters().Length + (MethodInfo.IsStatic ? 0 : 1)];
        for(int i = Variables.Length - 1; i >= 0; i--) {
            if(!stack.TryPop(out Variables[i]))
                throw new InvalidProgramException("Stack is empty");
        }
    }

    public ILCall(CodeInstruction instruction, ConcurrentStack<ILCode> stack) : this(instruction.operand as MethodInfo, stack) {
        Labels = instruction.labels;
        Blocks = instruction.blocks;
    }

    public override Type ReturnType => MethodInfo.ReturnType;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(ILCode tool in Variables)
        foreach(CodeInstruction instruction in tool.Load(generator))
            yield return instruction;
        CodeInstruction code = MethodInfo.IsStatic || !MethodInfo.IsVirtual ? new CodeInstruction(OpCodes.Call, MethodInfo) : new CodeInstruction(OpCodes.Callvirt, MethodInfo);
        code.labels = Labels;
        code.blocks = Blocks;
        yield return code;
    }

    public override string ToString() {
        StringBuilder builder = new();
        builder.Append(MethodInfo.IsStatic ? MethodInfo.DeclaringType.Name : Variables[0]).Append('.').Append(MethodInfo.Name).Append('(');
        for(int i = MethodInfo.IsStatic ? 0 : 1; i < Variables.Length; i++) {
            builder.Append(Variables[i]);
            builder.Append(", ");
        }
        if(Variables.Length > (MethodInfo.IsStatic ? 0 : 1)) builder.Length -= 2;
        builder.Append(')');
        return builder.ToString();
    }
}