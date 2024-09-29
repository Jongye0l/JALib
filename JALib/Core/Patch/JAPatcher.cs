using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch;

public class JAPatcher : IDisposable {

    private List<JAPatchAttribute> patchData;
    private JAMod mod;
    private string patchIdFront;
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
                if(attribute.TryingCatch) {
                    TypeBuilder typeBuilder = JAMod.ModuleBuilder.DefineType($"JAPatch.{attribute.PatchId}.{JARandom.Instance.NextInt()}", TypeAttributes.NotPublic);
                    FieldBuilder methodField = typeBuilder.DefineField("OriginalMethod", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static);
                    FieldBuilder exceptionCatchField = typeBuilder.DefineField("ExceptionCatcher", typeof(Action<Exception>), FieldAttributes.Private | FieldAttributes.Static);
                    MethodBuilder methodBuilder = typeBuilder.DefineMethod(originalMethod.Name, MethodAttributes.Public | MethodAttributes.Static,
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
                    Type patchType = typeBuilder.CreateType();
                    patchType.SetValue("OriginalMethod", originalMethod);
                    patchType.SetValue("ExceptionCatcher", OnPatchException);
                    harmonyMethods[originalMethod] = value = new HarmonyMethod(patchType.Method(originalMethod.Name));
                } else harmonyMethods[originalMethod] = value = new HarmonyMethod(originalMethod);
            }
            attribute.Patch = JALib.Harmony.Patch(attribute.MethodBase,
                attribute.PatchType == PatchType.Prefix ? value : null,
                attribute.PatchType == PatchType.Postfix ? value : null,
                attribute.PatchType == PatchType.Transpiler ? value : null,
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
            patchData.Add(attribute);
            if(!patched) continue;
            try {
                Patch(attribute);
            } catch (Exception) {
                // ignored
            }
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