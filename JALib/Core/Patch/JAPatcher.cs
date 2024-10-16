﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.JAException;
using JALib.Tools;
using JALib.Tools.ByteTool;

namespace JALib.Core.Patch;

public class JAPatcher : IDisposable {

    private List<JAPatchAttribute> patchData;
    private JAMod mod;
    public event FailPatch OnFailPatch;
    public bool patched { get; private set; }

    public delegate void FailPatch(string patchId);

    private static Dictionary<MethodInfo, HarmonyMethod> harmonyMethods = new();
    private static Dictionary<MethodBase, JAPatchInfo> jaPatches = new();

    static JAPatcher() {
        JALib.Harmony.Patch(typeof(Harmony).Assembly.GetType("HarmonyLib.PatchFunctions").Method("UpdateWrapper"), new HarmonyMethod(((Delegate) PatchUpdateWrapperPatch).Method));
    }

    private static bool PatchUpdateWrapperPatch(MethodBase original, PatchInfo patchInfo, ref MethodInfo __result) {
        try {
            if(!jaPatches.TryGetValue(original, out JAPatchInfo jaPatchInfo)) return true;
            __result = PatchUpdateWrapper(original, patchInfo, jaPatchInfo);
            return false;
        } catch (Exception e) {
            JALib.Instance.LogException(e);
            return true;
        }
    }

    private static MethodInfo PatchUpdateWrapper(MethodBase original, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        MethodInfo replacement = new JAMethodPatcher(original, null, patchInfo, jaPatchInfo).CreateReplacement(out Dictionary<int, CodeInstruction> finalInstructions1);
        if(replacement == null)
            throw new MissingMethodException("Cannot create replacement for " + original.FullDescription());
        try {
            typeof(Memory).Invoke("DetourMethodAndPersist", original, replacement);
        } catch (Exception ex) {
            throw typeof(HarmonyException).Invoke<Exception>("Create", ex, finalInstructions1);
        }
        return replacement;
    }

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
                if(attribute.TryingCatch && attribute.PatchType is PatchType.Prefix or PatchType.Postfix) {
                    TypeBuilder typeBuilder = JAMod.ModuleBuilder.DefineType($"JALib.Patch.{attribute.PatchId}.{JARandom.Instance.NextInt()}", TypeAttributes.NotPublic);
                    FieldBuilder exceptionCatchField = !attribute.TryingCatch ? null : typeBuilder.DefineField("ExceptionCatcher", typeof(Action<Exception>), FieldAttributes.Public | FieldAttributes.Static);
                    FieldBuilder methodField = typeBuilder.DefineField("OriginalMethod", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static);
                    MethodBuilder methodBuilder = typeBuilder.DefineMethod(originalMethod.Name, MethodAttributes.Private | MethodAttributes.Static,
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
            attribute.Patch = CustomPatch(attribute.MethodBase, value, (byte) attribute.PatchType, attribute.TryingCatch ? mod : null);
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

    private static MethodInfo CustomPatch(MethodBase original, HarmonyMethod patchMethod, byte patchType, JAMod mod) {
        Harmony harmony = JALib.Harmony;
        lock (typeof(PatchProcessor).GetValue("locker")) {
            PatchInfo patchInfo = typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Invoke<PatchInfo>("GetPatchInfo", [original]) ?? new PatchInfo();
            JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(original) ?? new JAPatchInfo();
            switch(patchType) {
                case 0:
                    if(mod != null) jaPatchInfo.AddTryPrefixes(harmony.Id, patchMethod, mod);
                    else patchInfo.Invoke("AddPrefixes", harmony.Id, new[] { patchMethod });
                    break;
                case 1:
                    if(mod != null) jaPatchInfo.AddTryPostfixes(harmony.Id, patchMethod, mod);
                    else patchInfo.Invoke("AddPostfixes", harmony.Id, new[] { patchMethod });
                    break;
                case 2:
                    patchInfo.Invoke("AddTranspilers", harmony.Id, new[] { patchMethod });
                    break;
                case 3:
                    patchInfo.Invoke("AddFinalizers", harmony.Id, new[] { patchMethod });
                    break;
                case 4:
                    jaPatchInfo.AddReplaces(harmony.Id, patchMethod);
                    break;
                case 5:
                    jaPatchInfo.AddRemoves(harmony.Id, patchMethod);
                    break;
            }
            MethodInfo replacement = PatchUpdateWrapper(original, patchInfo, jaPatchInfo);
            typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Invoke("UpdatePatchInfo", original, replacement, patchInfo);
            jaPatches[original] = jaPatchInfo;
            return replacement;
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