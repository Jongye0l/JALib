using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public class ILToken(MemberInfo member) : ILCode {
    public readonly MemberInfo Member = member;

    public override Type ReturnType => Member switch {
        FieldInfo => typeof(RuntimeFieldHandle),
        MethodInfo => typeof(RuntimeMethodHandle),
        ConstructorInfo => typeof(RuntimeMethodHandle),
        Type => typeof(RuntimeTypeHandle),
        _ => throw new NotImplementedException()
    };

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        yield return new CodeInstruction(OpCodes.Ldtoken, Member);
    }

    public override string ToString() => throw new NotImplementedException();
}