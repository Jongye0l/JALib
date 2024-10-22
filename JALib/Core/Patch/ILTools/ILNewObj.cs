using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILNewObj : ILCode {
    public readonly ConstructorInfo Constructor;
    public readonly ILCode[] Variables;

    public ILNewObj(ConstructorInfo constructor, ConcurrentStack<ILCode> stack) {
        Constructor = constructor;
        Variables = new ILCode[Constructor.GetParameters().Length];
        for(int i = Variables.Length - 1; i >= 0; i--) {
            if(!stack.TryPop(out Variables[i]))
                throw new InvalidProgramException("Stack is empty");
        }
    }

    public override Type ReturnType => Constructor.DeclaringType;

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(ILCode tool in Variables)
        foreach(CodeInstruction instruction in tool.Load(generator))
            yield return instruction;
        yield return new CodeInstruction(OpCodes.Newobj, Constructor);
    }

    public override string ToString() {
        StringBuilder builder = new();
        builder.Append("new ").Append(Constructor.DeclaringType.Name).Append('(');
        foreach(ILCode t in Variables) {
            builder.Append(t);
            builder.Append(", ");
        }
        if(Variables.Length > 0) builder.Length -= 2;
        builder.Append(')');
        return builder.ToString();
    }
}