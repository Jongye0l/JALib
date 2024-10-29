using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch;

class JAEmitter {
    public void Emit(OpCode opcode) => throw new NotSupportedException();
    public void Emit(OpCode opcode, string arg) => throw new NotSupportedException();
    public void Emit(OpCode opcode, MethodInfo arg) => throw new NotSupportedException();
    public void Emit(OpCode opcode, FieldInfo arg) => throw new NotSupportedException();
    public void Emit(OpCode opcode, LocalBuilder arg) => throw new NotSupportedException();
    public void MarkBlockBefore(ExceptionBlock block, out Label? label) => throw new NotSupportedException();
    public void MarkBlockAfter(ExceptionBlock block) => throw new NotSupportedException();
}