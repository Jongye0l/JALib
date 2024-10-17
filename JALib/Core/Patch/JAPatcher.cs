﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch;

public class JAPatcher : IDisposable {

    private List<JAPatchBaseAttribute> patchData;
    private JAMod mod;
    public event FailPatch OnFailPatch;
    public bool patched { get; private set; }

    #region CustomPatchPatching
    private static Dictionary<MethodBase, JAPatchInfo> jaPatches = new();

    static JAPatcher() {
        Harmony harmony = JALib.Harmony;
        Assembly assembly = typeof(Harmony).Assembly;
        harmony.Patch(assembly.GetType("HarmonyLib.PatchFunctions").Method("UpdateWrapper"), new HarmonyMethod(((Delegate) PatchUpdateWrapperPatch).Method));
        harmony.Patch(assembly.GetType("HarmonyLib.PatchFunctions").Method("ReversePatch"), new HarmonyMethod(((Delegate) PatchReversePatchPatch).Method));
        harmony.Patch(assembly.GetType("HarmonyLib.MethodCopier").Method("GetInstructions"), new HarmonyMethod(((Delegate) GetInstructions).Method));
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
        MethodInfo replacement = new JAMethodPatcher(original, patchInfo, jaPatchInfo).CreateReplacement(out Dictionary<int, CodeInstruction> finalInstructions1);
        if(replacement == null)
            throw new MissingMethodException("Cannot create replacement for " + original.FullDescription());
        try {
            typeof(Memory).Invoke("DetourMethodAndPersist", original, replacement);
        } catch (Exception ex) {
            throw typeof(HarmonyException).Invoke<Exception>("Create", ex, finalInstructions1);
        }
        foreach(ReversePatchData reversePatch in jaPatchInfo.reversePatches) UpdateReversePatch(reversePatch, patchInfo, jaPatchInfo);
        return replacement;
    }

    private static bool PatchReversePatchPatch(HarmonyMethod standin, MethodBase original, MethodInfo postTranspiler, ref MethodInfo __result) {
        try {
            if(standin == null || standin.method == null ||
               standin.reversePatchType == HarmonyReversePatchType.Snapshot ||
               !jaPatches.TryGetValue(original, out JAPatchInfo jaPatchInfo) ||
               jaPatchInfo.replaces.Length == 0) return true;
            bool debug = standin.debug.GetValueOrDefault() || Harmony.DEBUG;
            Patches patchInfo = Harmony.GetPatchInfo(original);
            MethodInfo replacement = new JAMethodPatcher(standin.method, original, patchInfo, jaPatchInfo, postTranspiler, debug).CreateReplacement(out Dictionary<int, CodeInstruction> finalInstructions1);
            if (replacement == null)
                throw new MissingMethodException("Cannot create replacement for " + standin.method.FullDescription());
            try {
                string str = Memory.DetourMethod(standin.method, replacement);
                if (str != null)
                    throw new FormatException("Method " + standin.method.FullDescription() + " cannot be patched. Reason: " + str);
            } catch (Exception ex) {
                throw typeof(HarmonyException).Invoke<Exception>("Create", ex, finalInstructions1);
            }
            typeof(Harmony).Assembly.GetType("HarmonyLib.PatchTools").Invoke("RememberObject", standin.method, replacement);
            __result = replacement;
            return false;
        } catch (Exception e) {
            JALib.Instance.LogException(e);
            return true;
        }
    }

    internal static bool GetInstructions(ILGenerator generator, MethodBase method, int maxTranspilers, ref List<CodeInstruction> __result) {
        if(method == null || generator == null || maxTranspilers < 1 || !jaPatches.TryGetValue(method, out JAPatchInfo jaPatchInfo) || jaPatchInfo.replaces.Length == 0) return true;
        __result = JAMethodPatcher.GetInstructions(generator, method, maxTranspilers, jaPatchInfo);
        return false;
    }

    #endregion

    public delegate void FailPatch(string patchId);

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

    private void Patch(JAPatchBaseAttribute attribute) {
        try {
            if(attribute.MinVersion > GCNS.releaseNumber || attribute.MaxVersion < GCNS.releaseNumber) return;
            if(attribute.MethodBase == null) {
                attribute.ClassType ??= Type.GetType(attribute.Class);
                if(attribute.ArgumentTypesType == null && attribute.ArgumentTypes != null) attribute.ArgumentTypesType = new Type[attribute.ArgumentTypes.Length];
                if(attribute.ArgumentTypesType != null && attribute.ArgumentTypes != null) for(int i = 0; i < attribute.ArgumentTypes.Length; i++) attribute.ArgumentTypesType[i] ??= Type.GetType(attribute.ArgumentTypes[i]);
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
                if(attribute.GenericType == null && attribute.GenericName != null) attribute.GenericType = new Type[attribute.GenericName.Length];
                if(attribute.GenericType != null) {
                    if(attribute.GenericName != null) for(int i = 0; i < attribute.GenericType.Length; i++) attribute.GenericType[i] ??= Type.GetType(attribute.GenericName[i]);
                    attribute.GenericType ??= attribute.GenericName.Select(name => Type.GetType(name)).ToArray();
                    attribute.MethodBase = ((MethodInfo) attribute.MethodBase).MakeGenericMethod(attribute.GenericType);
                }
            }
            if(attribute is JAPatchAttribute patchAttribute) CustomPatch(attribute.MethodBase,
                new HarmonyMethod(attribute.Method, patchAttribute.Priority, patchAttribute.Before, patchAttribute.After, attribute.Debug), patchAttribute, attribute.TryingCatch ? mod : null);
            else if(attribute is JAReversePatchAttribute reversePatchAttribute) CustomReversePatch(attribute.MethodBase, attribute.Method, reversePatchAttribute, mod);
            else throw new NotSupportedException("Unsupported Patch Type");
        } catch (Exception e) {
            mod.Error($"Mod {mod.Name} Id {attribute.PatchId} Patch Failed");
            mod.LogException(e);
            OnFailPatch?.Invoke(attribute.PatchId);
            if(attribute is not JAPatchAttribute { Disable: true }) return;
            mod.Error($"Mod {mod.Name} is Disabled.");
            Unpatch();
            throw;
        }
    }

    private static void CustomPatch(MethodBase original, HarmonyMethod patchMethod, JAPatchAttribute attribute, JAMod mod) {
        lock (typeof(PatchProcessor).GetValue("locker")) {
            PatchInfo patchInfo = typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Invoke<PatchInfo>("GetPatchInfo", [original]) ?? new PatchInfo();
            JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(original) ?? new JAPatchInfo();
            string id = attribute.PatchId;
            switch(attribute.PatchType) {
                case PatchType.Prefix:
                    if(CheckRemove(patchMethod.method)) jaPatchInfo.AddRemoves(id, patchMethod);
                    else if(mod != null) jaPatchInfo.AddTryPrefixes(id, patchMethod, mod);
                    else patchInfo.Invoke("AddPrefixes", id, new[] { patchMethod });
                    break;
                case PatchType.Postfix:
                    if(mod != null) jaPatchInfo.AddTryPostfixes(id, patchMethod, mod);
                    else patchInfo.Invoke("AddPostfixes", id, new[] { patchMethod });
                    break;
                case PatchType.Transpiler:
                    patchInfo.Invoke("AddTranspilers", id, new[] { patchMethod });
                    break;
                case PatchType.Finalizer:
                    patchInfo.Invoke("AddFinalizers", id, new[] { patchMethod });
                    break;
                case PatchType.Replace:
                    jaPatchInfo.AddReplaces(id, patchMethod);
                    break;
            }
            MethodInfo replacement = PatchUpdateWrapper(original, patchInfo, jaPatchInfo);
            typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Invoke("UpdatePatchInfo", original, replacement, patchInfo);
            jaPatches[original] = jaPatchInfo;
        }
    }

    private static void CustomReversePatch(MethodBase original, MethodInfo patchMethod, JAReversePatchAttribute attribute, JAMod mod) {
        PatchInfo patchInfo = typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Invoke<PatchInfo>("GetPatchInfo", [original]) ?? new PatchInfo();
        JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(original) ?? new JAPatchInfo();
        MethodInfo replacement = UpdateReversePatch(attribute.Data ??= new ReversePatchData {
            original = original,
            patchMethod = patchMethod,
            debug = attribute.Debug,
            attribute = attribute,
            mod = mod
        }, patchInfo, jaPatchInfo);
        typeof(Harmony).Assembly.GetType("HarmonyLib.PatchTools").Invoke("RememberObject", patchMethod, replacement);
        if(!attribute.PatchType.HasFlag(ReversePatchType.DontUpdate)) jaPatchInfo.reversePatches.Add(attribute.Data);
    }

    private static MethodInfo UpdateReversePatch(ReversePatchData data, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        bool debug = data.debug || Harmony.DEBUG;
        MethodInfo patchMethod = data.patchMethod;
        MethodInfo replacement = new JAMethodPatcher(patchMethod, data.original, patchInfo, jaPatchInfo, debug, data.attribute, data.mod).CreateReplacement(out Dictionary<int, CodeInstruction> finalInstructions1);
        if (replacement == null)
            throw new MissingMethodException("Cannot create replacement for " + patchMethod.FullDescription());
        try {
            string str = Memory.DetourMethod(patchMethod, replacement);
            if (str != null)
                throw new FormatException("Method " + patchMethod.FullDescription() + " cannot be patched. Reason: " + str);
        } catch (Exception ex) {
            throw typeof(HarmonyException).Invoke<Exception>("Create", ex, finalInstructions1);
        }
        return replacement;
    }

    private static bool CheckRemove(MethodInfo method) {
        if(method.ReturnType != typeof(bool)) return false;
        List<CodeInstruction> code = PatchProcessor.GetCurrentInstructions(method);
        IEnumerator<CodeInstruction> enumerator = code.Where(c => c.opcode != OpCodes.Nop).GetEnumerator();
        return enumerator.MoveNext() && enumerator.Current.opcode == OpCodes.Ldc_I4_0 &&
               enumerator.MoveNext() && enumerator.Current.opcode == OpCodes.Ret;
    }

    private void OnPatchException(Exception exception) {
        mod.LogException(exception);
    }

    public void Unpatch() {
        if(!patched) return;
        patched = false;
        foreach(JAPatchBaseAttribute baseAttribute in patchData) {
            if(baseAttribute is JAPatchAttribute patchAttribute) {
                MethodInfo patch = patchAttribute.Method;
                string id = patchAttribute.PatchId;
                lock(typeof(PatchProcessor).GetValue("locker")) {
                    PatchInfo patchInfo = typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Invoke<PatchInfo>("GetPatchInfo", [patchAttribute.MethodBase]) ?? new PatchInfo();
                    JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(patchAttribute.MethodBase) ?? new JAPatchInfo();
                    switch(patchAttribute.PatchType) {
                        case PatchType.Prefix:
                            if(CheckRemove(patch)) RemovePatch(patch, id, ref jaPatchInfo.removes);
                            else if(patchAttribute.TryingCatch) RemovePatch(patch, id, ref jaPatchInfo.tryPrefixes);
                            else RemovePatch(patch, id, ref patchInfo.prefixes);
                            break;
                        case PatchType.Postfix:
                            if(patchAttribute.TryingCatch) RemovePatch(patch, id, ref jaPatchInfo.tryPostfixes);
                            else RemovePatch(patch, id, ref patchInfo.postfixes);
                            break;
                        case PatchType.Transpiler:
                            RemovePatch(patch, id, ref patchInfo.transpilers);
                            break;
                        case PatchType.Finalizer:
                            RemovePatch(patch, id, ref patchInfo.finalizers);
                            break;
                        case PatchType.Replace:
                            RemovePatch(patch, id, ref jaPatchInfo.replaces);
                            break;
                    }
                    MethodInfo replacement = PatchUpdateWrapper(patchAttribute.MethodBase, patchInfo, jaPatchInfo);
                    typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Invoke("UpdatePatchInfo", patchAttribute.MethodBase, replacement, patchInfo);
                    jaPatches[patchAttribute.MethodBase] = jaPatchInfo;
                }
            } else if(baseAttribute is JAReversePatchAttribute reversePatchAttribute) {
                if(reversePatchAttribute.PatchType.HasFlag(ReversePatchType.DontUpdate)) continue;
                JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(reversePatchAttribute.Data.original);
                if(jaPatchInfo == null) continue;
                jaPatchInfo.reversePatches.Remove(reversePatchAttribute.Data);
            }
        }
    }

    private static void RemovePatch<T>(MethodInfo methodInfo, string id, ref T[] patches) where T : HarmonyLib.Patch =>
        patches = patches.Where(patch => patch.owner != id && patch.PatchMethod != methodInfo).ToArray();


    public JAPatcher AddPatch(Type type) {
        foreach(MethodInfo method in type.Methods()) AddPatch(method);
        return this;
    }

    public JAPatcher AddPatch(MethodInfo method) {
        foreach(JAPatchBaseAttribute attribute in method.GetCustomAttributes<JAPatchBaseAttribute>()) {
            attribute.Method = method;
            AddPatch(attribute);
        }
        return this;
    }

    public JAPatcher AddPatch(Delegate @delegate) {
        return AddPatch(@delegate.Method);
    }

    public JAPatcher AddPatch(JAPatchBaseAttribute patch) {
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