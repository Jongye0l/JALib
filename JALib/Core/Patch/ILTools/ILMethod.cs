using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Core.Patch.ILTools.Calculate;
using JALib.Core.Patch.ILTools.Get;
using JALib.Core.Patch.ILTools.Set;
using JALib.Core.Patch.ILTools.Value;

namespace JALib.Core.Patch.ILTools;

public class ILMethod : IEnumerator<ILCode>, ILTool {
    public List<ILCode> Codes = [];
    public int Index;
    private readonly Dictionary<LocalBuilder, ILLocal> Locals = new();
    public MethodInfo MethodInfo;
    public ILParameter[] Parameters = [];

    public ILMethod() {
    }

    public ILMethod(IEnumerable<CodeInstruction> instructions, MethodInfo method) {
        MethodInfo = method;
        Parameters = new ILParameter[method.GetParameters().Length];
        foreach(ParameterInfo parameter in method.GetParameters())
            Parameters[parameter.Position] = new ILParameter(parameter.Position, parameter.ParameterType, parameter.Name);
        ConcurrentStack<ILCode> stack = new();
        List<Label> labels = [];
        List<ExceptionBlock> blocks = [];
        foreach(CodeInstruction instruction in instructions) {
            instruction.labels.AddRange(labels);
            labels.Clear();
            instruction.blocks.AddRange(blocks);
            blocks.Clear();
            if(instruction.opcode == OpCodes.Nop) {
                labels = instruction.labels;
                blocks = instruction.blocks;
            }
            if(instruction.opcode == OpCodes.Break) throw new NotSupportedException("Break is not supported");
            if(instruction.opcode == OpCodes.Ldarg_0) stack.Push(new ILParameterGet(Parameters[0]));
            if(instruction.opcode == OpCodes.Ldarg_1) stack.Push(new ILParameterGet(Parameters[1]));
            if(instruction.opcode == OpCodes.Ldarg_2) stack.Push(new ILParameterGet(Parameters[2]));
            if(instruction.opcode == OpCodes.Ldarg_3) stack.Push(new ILParameterGet(Parameters[3]));
            if(instruction.opcode == OpCodes.Ldloc_0) throw new NotSupportedException("Ldloc_0 is not supported");
            if(instruction.opcode == OpCodes.Ldloc_1) throw new NotSupportedException("Ldloc_1 is not supported");
            if(instruction.opcode == OpCodes.Ldloc_2) throw new NotSupportedException("Ldloc_2 is not supported");
            if(instruction.opcode == OpCodes.Ldloc_3) throw new NotSupportedException("Ldloc_3 is not supported");
            if(instruction.opcode == OpCodes.Stloc_0) throw new NotSupportedException("Stloc_0 is not supported");
            if(instruction.opcode == OpCodes.Stloc_1) throw new NotSupportedException("Stloc_1 is not supported");
            if(instruction.opcode == OpCodes.Stloc_2) throw new NotSupportedException("Stloc_2 is not supported");
            if(instruction.opcode == OpCodes.Stloc_3) throw new NotSupportedException("Stloc_3 is not supported");
            if(instruction.opcode == OpCodes.Ldarg_S) stack.Push(new ILParameterGet(Parameters[(byte) instruction.operand]));
            if(instruction.opcode == OpCodes.Ldarga_S) stack.Push(new ILParameterPointerGet(Parameters[(byte) instruction.operand]));
            if(instruction.opcode == OpCodes.Starg_S) stack.Push(new ILParameterSet(Parameters[(byte) instruction.operand]));
            if(instruction.opcode == OpCodes.Ldloc_S) stack.Push(new ILLocalGet(GetLocal((LocalBuilder) instruction.operand)));
            if(instruction.opcode == OpCodes.Ldloca_S) stack.Push(new ILLocalPointerGet(GetLocal((LocalBuilder) instruction.operand)));
            if(instruction.opcode == OpCodes.Stloc_S) stack.Push(new ILLocalSet(GetLocal((LocalBuilder) instruction.operand), Pop(stack)));
            if(instruction.opcode == OpCodes.Ldnull) stack.Push(new ILNull());
            if(instruction.opcode == OpCodes.Ldc_I4_M1) stack.Push(new ILInt(-1));
            if(instruction.opcode == OpCodes.Ldc_I4_0) stack.Push(new ILInt(0));
            if(instruction.opcode == OpCodes.Ldc_I4_1) stack.Push(new ILInt(1));
            if(instruction.opcode == OpCodes.Ldc_I4_2) stack.Push(new ILInt(2));
            if(instruction.opcode == OpCodes.Ldc_I4_3) stack.Push(new ILInt(3));
            if(instruction.opcode == OpCodes.Ldc_I4_4) stack.Push(new ILInt(4));
            if(instruction.opcode == OpCodes.Ldc_I4_5) stack.Push(new ILInt(5));
            if(instruction.opcode == OpCodes.Ldc_I4_6) stack.Push(new ILInt(6));
            if(instruction.opcode == OpCodes.Ldc_I4_7) stack.Push(new ILInt(7));
            if(instruction.opcode == OpCodes.Ldc_I4_8) stack.Push(new ILInt(8));
            if(instruction.opcode == OpCodes.Ldc_I4_S) stack.Push(new ILInt((sbyte) instruction.operand));
            if(instruction.opcode == OpCodes.Ldc_I4) stack.Push(new ILInt((int) instruction.operand));
            if(instruction.opcode == OpCodes.Ldc_I8) stack.Push(new ILLong((long) instruction.operand));
            if(instruction.opcode == OpCodes.Ldc_R4) stack.Push(new ILFloat((float) instruction.operand));
            if(instruction.opcode == OpCodes.Ldc_R8) stack.Push(new ILDouble((double) instruction.operand));
            if(instruction.opcode == OpCodes.Dup) throw new NotSupportedException("Dup is not supported");
            if(instruction.opcode == OpCodes.Pop) Codes.Add(Pop(stack));
            if(instruction.opcode == OpCodes.Jmp) throw new NotSupportedException("Jmp is not supported Currently"); // TODO: Support Jmp
            if(instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) {
                ILCall code = new(instruction, stack);
                if(code.MethodInfo.ReturnType != typeof(void)) stack.Push(code);
                else Codes.Add(code);
            }
            if(instruction.opcode == OpCodes.Calli) throw new NotSupportedException("Calli is not supported");
            if(instruction.opcode == OpCodes.Ret) {
                if(stack.Count > 1) throw new InvalidProgramException($"There are {stack.Count} data in the stack, but it has been returned.");
                Codes.Add(new ILReturn(stack.TryPop(out ILCode tool) ? tool : null));
            }
            if(instruction.opcode == OpCodes.Br_S) throw new NotSupportedException("Br.s is not supported Currently"); // TODO: Support Br.s
            if(instruction.opcode == OpCodes.Brfalse_S) throw new NotSupportedException("Brfalse.s is not supported Currently"); // TODO: Support Brfalse.s
            if(instruction.opcode == OpCodes.Brtrue_S) throw new NotSupportedException("Brtrue.s is not supported Currently"); // TODO: Support Brtrue.s
            if(instruction.opcode == OpCodes.Beq_S) throw new NotSupportedException("Beq.s is not supported Currently"); // TODO: Support Beq.s
            if(instruction.opcode == OpCodes.Bge_S) throw new NotSupportedException("Bge.s is not supported Currently"); // TODO: Support Bge.s
            if(instruction.opcode == OpCodes.Bgt_S) throw new NotSupportedException("Bgt.s is not supported Currently"); // TODO: Support Bgt.s
            if(instruction.opcode == OpCodes.Ble_S) throw new NotSupportedException("Ble.s is not supported Currently"); // TODO: Support Ble.s
            if(instruction.opcode == OpCodes.Blt_S) throw new NotSupportedException("Blt.s is not supported Currently"); // TODO: Support Blt.s
            if(instruction.opcode == OpCodes.Bne_Un_S) throw new NotSupportedException("Bne.un.s is not supported Currently"); // TODO: Support Bne.un.s
            if(instruction.opcode == OpCodes.Bge_Un_S) throw new NotSupportedException("Bge.un.s is not supported Currently"); // TODO: Support Bge.un.s
            if(instruction.opcode == OpCodes.Bgt_Un_S) throw new NotSupportedException("Bgt.un.s is not supported Currently"); // TODO: Support Bgt.un.s
            if(instruction.opcode == OpCodes.Ble_Un_S) throw new NotSupportedException("Ble.un.s is not supported Currently"); // TODO: Support Ble.un.s
            if(instruction.opcode == OpCodes.Blt_Un_S) throw new NotSupportedException("Blt.un.s is not supported Currently"); // TODO: Support Blt.un.s
            if(instruction.opcode == OpCodes.Br) throw new NotSupportedException("Br is not supported Currently"); // TODO: Support Br
            if(instruction.opcode == OpCodes.Brfalse) throw new NotSupportedException("Brfalse is not supported Currently"); // TODO: Support Brfalse
            if(instruction.opcode == OpCodes.Brtrue) throw new NotSupportedException("Brtrue is not supported Currently"); // TODO: Support Brtrue
            if(instruction.opcode == OpCodes.Beq) throw new NotSupportedException("Beq is not supported Currently"); // TODO: Support Beq
            if(instruction.opcode == OpCodes.Bge) throw new NotSupportedException("Bge is not supported Currently"); // TODO: Support Bge
            if(instruction.opcode == OpCodes.Bgt) throw new NotSupportedException("Bgt is not supported Currently"); // TODO: Support Bgt
            if(instruction.opcode == OpCodes.Ble) throw new NotSupportedException("Ble is not supported Currently"); // TODO: Support Ble
            if(instruction.opcode == OpCodes.Blt) throw new NotSupportedException("Blt is not supported Currently"); // TODO: Support Blt
            if(instruction.opcode == OpCodes.Bne_Un) throw new NotSupportedException("Bne.un is not supported Currently"); // TODO: Support Bne.un
            if(instruction.opcode == OpCodes.Bge_Un) throw new NotSupportedException("Bge.un is not supported Currently"); // TODO: Support Bge.un
            if(instruction.opcode == OpCodes.Bgt_Un) throw new NotSupportedException("Bgt.un is not supported Currently"); // TODO: Support Bgt.un
            if(instruction.opcode == OpCodes.Ble_Un) throw new NotSupportedException("Ble.un is not supported Currently"); // TODO: Support Ble.un
            if(instruction.opcode == OpCodes.Blt_Un) throw new NotSupportedException("Blt.un is not supported Currently"); // TODO: Support Blt.un
            if(instruction.opcode == OpCodes.Switch) throw new NotSupportedException("Switch is not supported Currently"); // TODO: Support Switch
            // TODO: Support Pointer
            if(instruction.opcode == OpCodes.Ldind_I1) throw new NotSupportedException("Ldind.i1 is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_U1) throw new NotSupportedException("Ldind.u1 is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_I2) throw new NotSupportedException("Ldind.i2 is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_U2) throw new NotSupportedException("Ldind.u2 is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_I4) throw new NotSupportedException("Ldind.i4 is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_U4) throw new NotSupportedException("Ldind.u4 is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_I8) throw new NotSupportedException("Ldind.i8 is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_I) throw new NotSupportedException("Ldind.i is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_R4) throw new NotSupportedException("Ldind.r4 is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_R8) throw new NotSupportedException("Ldind.r8 is not supported Currently");
            if(instruction.opcode == OpCodes.Ldind_Ref) throw new NotSupportedException("Ldind.ref is not supported Currently");
            if(instruction.opcode == OpCodes.Stind_Ref) throw new NotSupportedException("Stind.ref is not supported Currently");
            if(instruction.opcode == OpCodes.Stind_I1) throw new NotSupportedException("Stind.i1 is not supported Currently");
            if(instruction.opcode == OpCodes.Stind_I2) throw new NotSupportedException("Stind.i2 is not supported Currently");
            if(instruction.opcode == OpCodes.Stind_I4) throw new NotSupportedException("Stind.i4 is not supported Currently");
            if(instruction.opcode == OpCodes.Stind_I8) throw new NotSupportedException("Stind.i8 is not supported Currently");
            if(instruction.opcode == OpCodes.Stind_R4) throw new NotSupportedException("Stind.r4 is not supported Currently");
            if(instruction.opcode == OpCodes.Stind_R8) throw new NotSupportedException("Stind.r8 is not supported Currently");
            if(instruction.opcode == OpCodes.Add) stack.Push(new ILAdd(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Sub) stack.Push(new ILSub(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Mul) stack.Push(new ILMul(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Div) stack.Push(new ILDiv(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Div_Un) stack.Push(new ILDivU(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Rem) stack.Push(new ILRem(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Rem_Un) stack.Push(new ILRemU(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.And) stack.Push(new ILAnd(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Or) stack.Push(new ILOr(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Xor) stack.Push(new ILXor(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Shl) stack.Push(new ILShl(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Shr) stack.Push(new ILShr(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Shr_Un) stack.Push(new ILShrU(Pop(stack), Pop(stack)));
            if(instruction.opcode == OpCodes.Neg) stack.Push(new ILNeg(Pop(stack)));
            if(instruction.opcode == OpCodes.Not) stack.Push(new ILNot(Pop(stack)));
            if(instruction.opcode == OpCodes.Conv_I1) stack.Push(new ILConvert(Pop(stack), typeof(sbyte)));
            if(instruction.opcode == OpCodes.Conv_I2) stack.Push(new ILConvert(Pop(stack), typeof(short)));
            if(instruction.opcode == OpCodes.Conv_I4) stack.Push(new ILConvert(Pop(stack), typeof(int)));
            if(instruction.opcode == OpCodes.Conv_I8) stack.Push(new ILConvert(Pop(stack), typeof(long)));
            if(instruction.opcode == OpCodes.Conv_R4) stack.Push(new ILConvert(Pop(stack), typeof(float)));
            if(instruction.opcode == OpCodes.Conv_R8) stack.Push(new ILConvert(Pop(stack), typeof(double)));
            if(instruction.opcode == OpCodes.Conv_U1) stack.Push(new ILConvert(Pop(stack), typeof(byte)));
            if(instruction.opcode == OpCodes.Conv_U2) stack.Push(new ILConvert(Pop(stack), typeof(ushort)));
            if(instruction.opcode == OpCodes.Conv_U4) stack.Push(new ILConvert(Pop(stack), typeof(uint)));
            if(instruction.opcode == OpCodes.Conv_U8) stack.Push(new ILConvert(Pop(stack), typeof(ulong)));
            if(instruction.opcode == OpCodes.Cpobj) throw new NotSupportedException("Cpobj is not supported");
            if(instruction.opcode == OpCodes.Ldobj) throw new NotSupportedException("Ldobj is not supported");
            if(instruction.opcode == OpCodes.Ldstr) stack.Push(new ILString((string) instruction.operand));
            if(instruction.opcode == OpCodes.Newobj) stack.Push(new ILNewObj((ConstructorInfo) instruction.operand, stack));
            if(instruction.opcode == OpCodes.Castclass) stack.Push(new ILCast(Pop(stack), (Type) instruction.operand));
            // TODO: Support Other Opcodes

            if(instruction.opcode == OpCodes.Ldarg) stack.Push(new ILParameterGet(Parameters[(int) instruction.operand]));
        }
    }

    public void Dispose() {
    }

    public bool MoveNext() => ++Index < Codes.Count;

    public void Reset() => Index = 0;

    public ILCode Current => Codes[Index];

    object IEnumerator.Current => Current;

    private static ILCode Pop(ConcurrentStack<ILCode> stack) {
        if(!stack.TryPop(out ILCode tool)) throw new InvalidProgramException("Stack is empty");
        return tool;
    }

    public ILLocal GetLocal(LocalBuilder local) {
        if(Locals.TryGetValue(local, out ILLocal ilLocal)) return ilLocal;
        Locals[local] = ilLocal = new ILLocal(local);
        ilLocal.Index = Locals.Count - 1;
        return ilLocal;
    }

    public IEnumerable<CodeInstruction> Load(ILGenerator generator) => Codes.SelectMany(code => code.Load(generator));
}