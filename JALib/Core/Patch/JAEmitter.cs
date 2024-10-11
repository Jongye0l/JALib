using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch;

class JAEmitter(object original) {

    public void Emit(OpCode opcode) => original.Invoke("Emit", opcode);
    public void Emit(OpCode opcode, int arg) => original.Invoke("Emit", opcode, arg);
    public void Emit(OpCode opcode, long arg) => original.Invoke("Emit", opcode, arg);
    public void Emit(OpCode opcode, float arg) => original.Invoke("Emit", opcode, arg);
    public void Emit(OpCode opcode, double arg) => original.Invoke("Emit", opcode, arg);
    public void Emit(OpCode opcode, Label arg) => original.Invoke("Emit", opcode, arg);
    public void Emit(OpCode opcode, Type arg) => original.Invoke("Emit", opcode, arg);
    public void Emit(OpCode opcode, LocalBuilder arg) => original.Invoke("Emit", opcode, arg);
    public void MarkBlockBefore(ExceptionBlock block) => original.Invoke("MarkBlockBefore", block, null);
    public void MarkLabel(Label label) => original.Invoke("MarkLabel", label);
    public void MarkBlockAfter(ExceptionBlock block) => original.Invoke("MarkBlockAfter", block);
    public Dictionary<int, CodeInstruction> GetInstructions() => original.Invoke<Dictionary<int, CodeInstruction>>("GetInstructions");
}