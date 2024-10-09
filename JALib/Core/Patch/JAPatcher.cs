﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.JAException;
using JALib.Tools;

namespace JALib.Core.Patch;

public class JAPatcher : IDisposable {

    private List<JAPatchAttribute> patchData;
    private JAMod mod;
    public event FailPatch OnFailPatch;
    public bool patched { get; private set; }

    public delegate void FailPatch(string patchId);

    private static Dictionary<MethodInfo, HarmonyMethod> harmonyMethods = new();

    public JAPatcher(JAMod mod) {
        this.mod = mod;
        patchData = [];
    }

    public void Patch() {
        if(patched) return;
        patched = true;
        foreach(JAPatchAttribute attribute in patchData) {
            try {
                Patch(attribute);
            } catch (Exception) {
                break;
            }
        }
    }

    private void Patch(JAPatchAttribute attribute) {
        try {
            if(attribute.MinVersion > GCNS.releaseNumber || attribute.MaxVersion < GCNS.releaseNumber) return;
            if(attribute.MethodBase == null) {
                attribute.ClassType ??= Type.GetType(attribute.Class);
                if(attribute.ArgumentTypesType == null && attribute.ArgumentTypes != null) {
                    attribute.ArgumentTypesType = new Type[attribute.ArgumentTypes.Length];
                    for(int i = 0; i < attribute.ArgumentTypes.Length; i++)
                        attribute.ArgumentTypesType[i] = Type.GetType(attribute.ArgumentTypes[i]);
                }
                if(attribute.MethodName == ".ctor")
                    attribute.MethodBase = attribute.ArgumentTypesType == null ? attribute.ClassType.Constructor() : attribute.ClassType.Constructor(attribute.ArgumentTypesType);
                else if(attribute.MethodName == ".cctor") attribute.MethodBase = attribute.ClassType.TypeInitializer;
                else if(attribute.MethodName == "u+") attribute.MethodBase = attribute.ClassType.GetMethod("op_UnaryPlus");
                else if(attribute.MethodName == "u-") attribute.MethodBase = attribute.ClassType.GetMethod("op_UnaryNegation");
                else if(attribute.MethodName == "++") attribute.MethodBase = attribute.ClassType.GetMethod("op_Increment");
                else if(attribute.MethodName == "--") attribute.MethodBase = attribute.ClassType.GetMethod("op_Decrement");
                else if(attribute.MethodName == "!") attribute.MethodBase = attribute.ClassType.GetMethod("op_LogicalNot");
                else if(attribute.MethodName == "+") attribute.MethodBase = attribute.ClassType.GetMethod("op_Addition");
                else if(attribute.MethodName == "-") attribute.MethodBase = attribute.ClassType.GetMethod("op_Subtraction");
                else if(attribute.MethodName == "*") attribute.MethodBase = attribute.ClassType.GetMethod("op_Multiply");
                else if(attribute.MethodName == "/") attribute.MethodBase = attribute.ClassType.GetMethod("op_Division");
                else if(attribute.MethodName == "&") attribute.MethodBase = attribute.ClassType.GetMethod("op_BitwiseAnd");
                else if(attribute.MethodName == "|") attribute.MethodBase = attribute.ClassType.GetMethod("op_BitwiseOr");
                else if(attribute.MethodName == "^") attribute.MethodBase = attribute.ClassType.GetMethod("op_ExclusiveOr");
                else if(attribute.MethodName == "~") attribute.MethodBase = attribute.ClassType.GetMethod("op_OnesComplement");
                else if(attribute.MethodName == "==") attribute.MethodBase = attribute.ClassType.GetMethod("op_Equality");
                else if(attribute.MethodName == "!=") attribute.MethodBase = attribute.ClassType.GetMethod("op_Inequality");
                else if(attribute.MethodName == "<") attribute.MethodBase = attribute.ClassType.GetMethod("op_LessThan");
                else if(attribute.MethodName == ">") attribute.MethodBase = attribute.ClassType.GetMethod("op_GreaterThan");
                else if(attribute.MethodName == "<=") attribute.MethodBase = attribute.ClassType.GetMethod("op_LessThanOrEqual");
                else if(attribute.MethodName == ">=") attribute.MethodBase = attribute.ClassType.GetMethod("op_GreaterThanOrEqual");
                else if(attribute.MethodName == "<<") attribute.MethodBase = attribute.ClassType.GetMethod("op_LeftShift");
                else if(attribute.MethodName == ">>") attribute.MethodBase = attribute.ClassType.GetMethod("op_RightShift");
                else if(attribute.MethodName == "%") attribute.MethodBase = attribute.ClassType.GetMethod("op_Modulus");
                else if(attribute.MethodName == ".implicit") attribute.MethodBase = attribute.ClassType.GetMethod("op_Implicit");
                else if(attribute.MethodName == ".explicit") attribute.MethodBase = attribute.ClassType.GetMethod("op_Explicit");
                else if(attribute.MethodName == ".true") attribute.MethodBase = attribute.ClassType.GetMethod("op_True");
                else if(attribute.MethodName == ".false") attribute.MethodBase = attribute.ClassType.GetMethod("op_False");
                else if(attribute.MethodName == "[].get") attribute.MethodBase = attribute.ClassType.Getter("Item");
                else if(attribute.MethodName == "[].set") attribute.MethodBase = attribute.ClassType.Setter("Item");
                else if(attribute.MethodName.EndsWith(".get")) attribute.MethodBase = attribute.ClassType.Getter(attribute.MethodName[..4]);
                else if(attribute.MethodName.EndsWith(".set")) attribute.MethodBase = attribute.ClassType.Setter(attribute.MethodName[..4]);
                else attribute.MethodBase = attribute.ArgumentTypesType == null ? attribute.ClassType.Method(attribute.MethodName) : attribute.ClassType.Method(attribute.MethodName, attribute.ArgumentTypesType);
                if(attribute.GenericType != null || attribute.GenericName != null) {
                    attribute.GenericType ??= Type.GetType(attribute.GenericName);
                    attribute.MethodBase = ((MethodInfo) attribute.MethodBase).MakeGenericMethod(attribute.GenericType);
                }
            }
            if(!harmonyMethods.TryGetValue(attribute.Method, out HarmonyMethod value)) {
                MethodInfo originalMethod = attribute.Method;
                if(attribute.TryingCatch && attribute.PatchType is PatchType.Prefix or PatchType.Postfix || attribute.PatchType == PatchType.Replace) {
                    TypeBuilder typeBuilder = JAMod.ModuleBuilder.DefineType($"JALib.Patch.{attribute.PatchId}.{JARandom.Instance.NextInt()}", TypeAttributes.NotPublic);
                    FieldBuilder exceptionCatchField = !attribute.TryingCatch ? null : typeBuilder.DefineField("ExceptionCatcher", typeof(Action<Exception>), FieldAttributes.Public | FieldAttributes.Static);
                    MethodBuilder methodBuilder;
                    if(attribute.PatchType == PatchType.Replace) {
                        methodBuilder = typeBuilder.DefineMethod(originalMethod.Name, MethodAttributes.Private | MethodAttributes.Static,
                            typeof(IEnumerable<CodeInstruction>), [typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator)]);
                        methodBuilder.DefineParameter(1, ParameterAttributes.None, "instructions");
                        methodBuilder.DefineParameter(2, ParameterAttributes.None, "generator");
                        FieldBuilder runner = typeBuilder.DefineField("Runner", typeof(Func<IEnumerable<CodeInstruction>, ILGenerator, IEnumerable<CodeInstruction>>), FieldAttributes.Private | FieldAttributes.Static);
                        ILGenerator ilGenerator = methodBuilder.GetILGenerator();
                        ilGenerator.Emit(OpCodes.Ldsfld, runner);
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.Emit(OpCodes.Call, typeof(Func<IEnumerable<CodeInstruction>, ILGenerator, IEnumerable<CodeInstruction>>).Method("Invoke"));
                        ilGenerator.Emit(OpCodes.Ret);
                    } else {
                        FieldBuilder methodField = typeBuilder.DefineField("OriginalMethod", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static);
                        methodBuilder = typeBuilder.DefineMethod(originalMethod.Name, MethodAttributes.Private | MethodAttributes.Static,
                            originalMethod.ReturnType, originalMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
                        foreach(ParameterInfo parameter in originalMethod.GetParameters()) methodBuilder.DefineParameter(parameter.Position + 1, parameter.Attributes, parameter.Name);
                        ILGenerator ilGenerator = methodBuilder.GetILGenerator();
                        Label returnLabel = ilGenerator.DefineLabel();
                        ilGenerator.BeginExceptionBlock();
                        LocalBuilder objectLocal = ilGenerator.DeclareLocal(typeof(object));
                        ilGenerator.Emit(OpCodes.Ldsfld, methodField);
                        ilGenerator.Emit(OpCodes.Ldnull);
                        ilGenerator.Emit(OpCodes.Ldc_I4, originalMethod.GetParameters().Length);
                        ilGenerator.Emit(OpCodes.Newarr, typeof(object));
                        for(int i = 0; i < originalMethod.GetParameters().Length; i++) {
                            ilGenerator.Emit(OpCodes.Dup);
                            ilGenerator.Emit(OpCodes.Ldc_I4, i);
                            ilGenerator.Emit(OpCodes.Ldarg, i);
                            Type type = originalMethod.GetParameters()[i].ParameterType;
                            if(type.IsValueType) ilGenerator.Emit(OpCodes.Box, type);
                            ilGenerator.Emit(OpCodes.Stelem_Ref);
                        }
                        ilGenerator.Emit(OpCodes.Call, typeof(MethodInfo).Method("Invoke", typeof(object), typeof(object[])));
                        ilGenerator.Emit(OpCodes.Stloc, objectLocal);
                        ilGenerator.Emit(OpCodes.Leave, returnLabel);
                        ilGenerator.BeginCatchBlock(typeof(Exception));
                        LocalBuilder exceptionLocal = ilGenerator.DeclareLocal(typeof(Exception));
                        ilGenerator.Emit(OpCodes.Stloc, exceptionLocal);
                        ilGenerator.Emit(OpCodes.Ldsfld, exceptionCatchField);
                        ilGenerator.Emit(OpCodes.Ldloc, exceptionLocal);
                        ilGenerator.Emit(OpCodes.Call, typeof(Action<Exception>).Method("Invoke"));
                        if(originalMethod.ReturnType != typeof(void)) {
                            if(originalMethod.ReturnType.IsValueType) {
                                ilGenerator.Emit(originalMethod.ReturnType == typeof(bool) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                                ilGenerator.Emit(OpCodes.Box, originalMethod.ReturnType);
                            } else ilGenerator.Emit(OpCodes.Ldnull);
                            ilGenerator.Emit(OpCodes.Stloc, objectLocal);
                        }
                        ilGenerator.Emit(OpCodes.Leave, returnLabel);
                        ilGenerator.EndExceptionBlock();
                        ilGenerator.MarkLabel(returnLabel);
                        if(originalMethod.ReturnType != typeof(void)) {
                            ilGenerator.Emit(OpCodes.Ldloc, objectLocal);
                            if(originalMethod.ReturnType.IsValueType) ilGenerator.Emit(OpCodes.Unbox_Any, originalMethod.ReturnType);
                        }
                        ilGenerator.Emit(OpCodes.Ret);
                    }
                    Type patchType = typeBuilder.CreateType();
                    if(attribute.PatchType == PatchType.Replace) {
                        IEnumerable<CodeInstruction> Runner(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
                            Type originalReturnType = attribute.MethodBase is MethodInfo info ? info.ReturnType : typeof(void);
                            if(originalMethod.ReturnType != originalReturnType) throw new PatchReturnException(originalReturnType, originalMethod.ReturnType);
                            Dictionary<int, int> parameterMap = new();
                            Dictionary<int, FieldInfo> parameterFields = new();
                            List<int> objects = [];
                            ParameterInfo[] parameters = attribute.MethodBase.GetParameters();
                            foreach(ParameterInfo parameterInfo in originalMethod.GetParameters()) {
                                ParameterInfo parameter = parameters.FirstOrDefault(info => info.Name == parameterInfo.Name);
                                if(parameterInfo.Name.ToLower() == "__instance") {
                                    if(attribute.MethodBase.IsStatic) throw new PatchParameterException("Instance parameter in static method");
                                    if(parameterInfo.ParameterType != attribute.MethodBase.DeclaringType) throw new PatchParameterException("Instance parameter type mismatch");
                                    parameterMap[parameterInfo.Position] = 0;
                                    continue;
                                }
                                if(parameterInfo.Name.ToLower() == "__arguments") {
                                    if(parameterInfo.ParameterType != typeof(object[])) throw new PatchParameterException("Arguments parameter type mismatch");
                                    objects.Add(parameterInfo.Position);
                                    continue;
                                }
                                if(parameterInfo.Name.StartsWith("___")) {
                                    FieldInfo field = attribute.ClassType.Field(parameterInfo.Name[3..]);
                                    if(field == null) throw new PatchParameterException("Unknown Parameter: " + parameterInfo.Name);
                                    parameterFields[parameterInfo.Position] = field;
                                }
                                if(parameter != null) {
                                    if(parameter.ParameterType != parameterInfo.ParameterType) throw new PatchParameterException("Parameter type mismatch: " + parameterInfo.Name);
                                    parameterMap[parameterInfo.Position] = parameter.Position;
                                    continue;
                                }
                                throw new PatchParameterException("Unknown Parameter: " + parameterInfo.Name);
                            }
                            foreach(CodeInstruction instruction in PatchProcessor.GetCurrentInstructions(originalMethod)) {
                                int index = -1;
                                bool set = false;
                                if(instruction.opcode == OpCodes.Ldarg) index = (int) instruction.operand;
                                else if(instruction.opcode == OpCodes.Ldarga) index = (int) instruction.operand;
                                else if(instruction.opcode == OpCodes.Ldarg_S) index = (byte) instruction.operand;
                                else if(instruction.opcode == OpCodes.Ldarga_S) index = (byte) instruction.operand;
                                else if(instruction.opcode == OpCodes.Starg) {
                                    index = (int) instruction.operand;
                                    set = true;
                                } else if(instruction.opcode == OpCodes.Starg_S) {
                                    index = (byte) instruction.operand;
                                    set = true;
                                } else if(instruction.opcode == OpCodes.Ldarg_0) {
                                    index = 0;
                                    instruction.opcode = OpCodes.Ldarg;
                                } else if(instruction.opcode == OpCodes.Ldarg_1) {
                                    index = 1;
                                    instruction.opcode = OpCodes.Ldarg;
                                } else if(instruction.opcode == OpCodes.Ldarg_2) {
                                    index = 2;
                                    instruction.opcode = OpCodes.Ldarg;
                                } else if(instruction.opcode == OpCodes.Ldarg_3) {
                                    index = 3;
                                    instruction.opcode = OpCodes.Ldarg;
                                }
                                if(index != -1) {
                                    if(parameterMap.TryGetValue(index, out int intValue)) {
                                        instruction.operand = intValue;
                                    } else if(parameterFields.TryGetValue(index, out FieldInfo field)) {
                                        if(set) {
                                            if(field.IsStatic) yield return new CodeInstruction(OpCodes.Stsfld, field);
                                            else {
                                                LocalBuilder localBuilder = generator.DeclareLocal(field.FieldType);
                                                yield return new CodeInstruction(OpCodes.Stloc, localBuilder);
                                                yield return new CodeInstruction(OpCodes.Ldarg_0);
                                                yield return new CodeInstruction(OpCodes.Ldloc, localBuilder);
                                                yield return new CodeInstruction(OpCodes.Stfld, field);
                                            }
                                        } else {
                                            if(field.IsStatic) yield return new CodeInstruction(OpCodes.Ldsfld, field);
                                            else {
                                                yield return new CodeInstruction(OpCodes.Ldarg_0);
                                                yield return new CodeInstruction(OpCodes.Ldfld, field);
                                            }
                                        }
                                        continue;
                                    } else throw new PatchParameterException("Unknown Parameter: " + index);
                                }
                                yield return instruction;
                            }
                        }
                        patchType.SetValue("Runner", Runner);
                    } else {
                        patchType.SetValue("OriginalMethod", originalMethod);
                        patchType.SetValue("ExceptionCatcher", OnPatchException);
                    }
                    harmonyMethods[originalMethod] = value = new HarmonyMethod(patchType.Method(originalMethod.Name));
                } else harmonyMethods[originalMethod] = value = new HarmonyMethod(originalMethod);
            }
            attribute.Patch = JALib.Harmony.Patch(attribute.MethodBase,
                attribute.PatchType == PatchType.Prefix ? value : null,
                attribute.PatchType == PatchType.Postfix ? value : null,
                attribute.PatchType is PatchType.Transpiler or PatchType.Replace ? value : null,
                attribute.PatchType == PatchType.Finalizer ? value : null);
        } catch (Exception e) {
            mod.Error($"Mod {mod.Name} Id {attribute.PatchId} Patch Failed");
            mod.LogException(e);
            OnFailPatch?.Invoke(attribute.PatchId);
            if(!attribute.Disable) return;
            mod.Error($"Mod {mod.Name} is Disabled.");
            Unpatch();
            throw;
        }
    }

    private void OnPatchException(Exception exception) {
        mod.LogException(exception);
    }

    public void Unpatch() {
        if(!patched) return;
        patched = false;
        foreach(JAPatchAttribute patchData in patchData) JALib.Harmony.Unpatch(patchData.MethodBase, harmonyMethods[patchData.Method].method);
    }

    public JAPatcher AddPatch(Type type) {
        foreach(MethodInfo method in type.Methods()) AddPatch(method);
        return this;
    }

    public JAPatcher AddPatch(MethodInfo method) {
        foreach(JAPatchAttribute attribute in method.GetCustomAttributes<JAPatchAttribute>()) {
            attribute.Method = method;
            AddPatch(attribute);
        }
        return this;
    }

    public JAPatcher AddPatch(Delegate @delegate) {
        return AddPatch(@delegate.Method);
    }

    public JAPatcher AddPatch(JAPatchAttribute patch) {
        patchData.Add(patch);
        if(!patched) return this;
        try {
            Patch(patch);
        } catch (Exception) {
            // ignored
        }
        return this;
    }

    public JAPatcher AddPatch(MethodInfo method, JAPatchAttribute patch) {
        patch.Method = method;
        return AddPatch(patch);
    }

    public JAPatcher AddPatch(Delegate @delegate, JAPatchAttribute patch) {
        return AddPatch(@delegate.Method, patch);
    }

    public void Dispose() {
        Unpatch();
        GC.SuppressFinalize(patchData);
        GC.SuppressFinalize(this);
    }
}