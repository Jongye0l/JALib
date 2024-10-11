using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch;

class JAEmitter(object original) {
    public void Emit(OpCode opcode) => original.Invoke("Emit", [typeof(OpCode)], opcode);
    public void Emit(OpCode opcode, int arg) => original.Invoke("Emit", [typeof(OpCode), typeof(int)], opcode, arg);
    public void Emit(OpCode opcode, long arg) => original.Invoke("Emit", [typeof(OpCode), typeof(long)], opcode, arg);
    public void Emit(OpCode opcode, float arg) => original.Invoke("Emit", [typeof(OpCode), typeof(float)], opcode, arg);
    public void Emit(OpCode opcode, double arg) => original.Invoke("Emit", [typeof(OpCode), typeof(double)], opcode, arg);
    public void Emit(OpCode opcode, string arg) => original.Invoke("Emit", [typeof(OpCode), typeof(string)], opcode, arg);
    public void Emit(OpCode opcode, Label arg) => original.Invoke("Emit", [typeof(OpCode), typeof(Label)], opcode, arg);
    public void Emit(OpCode opcode, Type arg) => original.Invoke("Emit", [typeof(OpCode), typeof(Type)], opcode, arg);
    public void Emit(OpCode opcode, MethodInfo arg) => original.Invoke("Emit", [typeof(OpCode), typeof(MethodInfo)], opcode, arg);
    public void Emit(OpCode opcode, LocalBuilder arg) => original.Invoke("Emit", [typeof(OpCode), typeof(LocalBuilder)], opcode, arg);
    public void MarkBlockBefore(ExceptionBlock block) => original.Invoke("MarkBlockBefore", [typeof(ExceptionBlock), typeof(Label?).MakeByRefType()], block, null);
    public void MarkLabel(Label label) => original.Invoke("MarkLabel", [typeof(Label)], label);
    public void MarkBlockAfter(ExceptionBlock block) => original.Invoke("MarkBlockAfter", [typeof(ExceptionBlock)], block);
    public Dictionary<int, CodeInstruction> GetInstructions() => original.Invoke<Dictionary<int, CodeInstruction>>("GetInstructions");
    public object GetOriginal() => original;
}