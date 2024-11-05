using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Bootstrap;
using JALib.Tools;

namespace JALib.Core.Patch;

public class JAPatcher : IDisposable {

    private static bool _isOldHarmony;
    private List<JAPatchBaseAttribute> patchData;
    private JAMod mod;
    public event FailPatch OnFailPatch;
    public bool patched { get; private set; }

    #region CustomPatchPatching
    private static Dictionary<MethodBase, JAPatchInfo> jaPatches = new();

    static JAPatcher() {
        Harmony harmony = JALib.Harmony = typeof(JABootstrap).GetValue<Harmony>("harmony") ?? new Harmony("JALib");
        Assembly assembly = typeof(Harmony).Assembly;
        Type patchFunctions = assembly.GetType("HarmonyLib.PatchFunctions");
        _isOldHarmony = assembly.GetName().Version < new Version(2, 0, 3, 0);
        if(_isOldHarmony) harmony.Patch(typeof(HarmonyLib.Patch).Constructor(), new HarmonyMethod(((Delegate) FixPatchCtorNull).Method));
        harmony.CreateReversePatcher(patchFunctions.Method("UpdateWrapper"), new HarmonyMethod(((Delegate) PatchUpdateWrapperReverse).Method)).Patch();
        MethodInfo reversePatchMethod = patchFunctions.Method("ReversePatch");
        harmony.CreateReversePatcher(reversePatchMethod, new HarmonyMethod(((Delegate) PatchReversePatchReverse).Method)).Patch();
        harmony.CreateReversePatcher(reversePatchMethod, new HarmonyMethod(((Delegate) UpdateReversePatch).Method)).Patch();
        Type methodPatcher = assembly.GetType("HarmonyLib.MethodPatcher");
        harmony.CreateReversePatcher(methodPatcher.Method("PrefixAffectsOriginal"), new HarmonyMethod(((Delegate) JAMethodPatcher.PrefixAffectsOriginal).Method)).Patch();
        harmony.Patch(((Delegate) JAMethodPatcher.AddOverride).Method, transpiler: new HarmonyMethod(((Delegate) JAMethodPatcher.AddOverridePatch).Method));
        harmony.CreateReversePatcher(methodPatcher.Method("CreateReplacement"), new HarmonyMethod(((Delegate) JAMethodPatcher.CreateReplacement).Method)).Patch();
        JAMethodPatcher.LoadAddPrePostMethod(harmony);
        harmony.CreateReversePatcher(assembly.GetType("HarmonyLib.HarmonySharedState").Method("GetPatchInfo"), new HarmonyMethod(((Delegate) GetPatchInfo).Method)).Patch();
        harmony.Patch(patchFunctions.Method("UpdateWrapper"), new HarmonyMethod(((Delegate) PatchUpdateWrapperPatch).Method));
        harmony.Patch(patchFunctions.Method("ReversePatch"), new HarmonyMethod(((Delegate) PatchReversePatchPatch).Method));
        harmony.Patch(assembly.GetType("HarmonyLib.MethodCopier").Method("GetInstructions"), new HarmonyMethod(((Delegate) GetInstructions).Method));
    }

    private static void FixPatchCtorNull(ref string[] before, ref string[] after) {
        before ??= [];
        after ??= [];
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
        MethodInfo replacement = PatchUpdateWrapperReverse(original, patchInfo, jaPatchInfo);
        foreach(ReversePatchData reversePatch in jaPatchInfo.reversePatches) UpdateReversePatch(reversePatch, patchInfo, jaPatchInfo);
        return replacement;
    }

    private static MethodInfo PatchUpdateWrapperReverse(MethodBase original, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        _ = Transpiler(null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> list = [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Newobj, typeof(JAMethodPatcher).Constructor(typeof(MethodBase), typeof(PatchInfo), typeof(JAPatchInfo)))
            ];
            using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                if(code.opcode != OpCodes.Ldloca_S && code.opcode != OpCodes.Ldloca) continue;
                enumerator.MoveNext();
                if(enumerator.Current.opcode != OpCodes.Call && enumerator.Current.opcode != OpCodes.Callvirt ||
                   enumerator.Current.operand is not MethodInfo { Name: "CreateReplacement" }) continue;
                list.Add(code);
                break;
            }
            list.Add(new CodeInstruction(OpCodes.Call, typeof(JAMethodPatcher).Method("CreateReplacement")));
            while(enumerator.MoveNext()) list.Add(enumerator.Current);
            return list;
        }
    }

    private static bool PatchReversePatchPatch(HarmonyMethod standin, MethodBase original, MethodInfo postTranspiler, ref MethodInfo __result) {
        try {
            if(standin == null || standin.method == null ||
               standin.reversePatchType == HarmonyReversePatchType.Snapshot ||
               !jaPatches.TryGetValue(original, out JAPatchInfo jaPatchInfo) ||
               jaPatchInfo.replaces.Length == 0) return true;
            __result = PatchReversePatchReverse(standin, original, postTranspiler, jaPatchInfo);
            return false;
        } catch (Exception e) {
            JALib.Instance.LogException(e);
            return true;
        }
    }

    private static MethodInfo PatchReversePatchReverse(HarmonyMethod standin, MethodBase original, MethodInfo postTranspiler, JAPatchInfo jaPatchInfo) {
        _ = Transpiler(null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> list = [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldarg_3),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Newobj, typeof(JAMethodPatcher).Constructor(typeof(HarmonyMethod), typeof(MethodBase), typeof(JAPatchInfo), typeof(MethodInfo)))
            ];
            using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                if(code.opcode != OpCodes.Ldloca_S && code.opcode != OpCodes.Ldloca) continue;
                enumerator.MoveNext();
                if(enumerator.Current.opcode != OpCodes.Call && enumerator.Current.opcode != OpCodes.Callvirt ||
                   enumerator.Current.operand is not MethodInfo { Name: "CreateReplacement" }) continue;
                list.Add(code);
                break;
            }
            list.Add(new CodeInstruction(OpCodes.Call, typeof(JAMethodPatcher).Method("CreateReplacement")));
            while(enumerator.MoveNext()) list.Add(enumerator.Current);
            return list;
        }
    }

    private static MethodInfo UpdateReversePatch(ReversePatchData data, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        _ = Transpiler(null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> list = [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Newobj, typeof(JAMethodPatcher).Constructor(typeof(ReversePatchData), typeof(PatchInfo), typeof(JAPatchInfo)))
            ];
            using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                if(code.opcode != OpCodes.Ldloca_S && code.opcode != OpCodes.Ldloca) continue;
                enumerator.MoveNext();
                if(enumerator.Current.opcode != OpCodes.Call && enumerator.Current.opcode != OpCodes.Callvirt ||
                   enumerator.Current.operand is not MethodInfo { Name: "CreateReplacement" }) continue;
                list.Add(code);
                break;
            }
            list.Add(new CodeInstruction(OpCodes.Call, typeof(JAMethodPatcher).Method("CreateReplacement")));
            while(enumerator.MoveNext()) list.Add(enumerator.Current);
            return list;
        }
    }

    internal static bool GetInstructions(ILGenerator generator, MethodBase method, int maxTranspilers, ref List<CodeInstruction> __result) {
        try {
            if(method == null || generator == null || maxTranspilers < 1 || !jaPatches.TryGetValue(method, out JAPatchInfo jaPatchInfo) || jaPatchInfo.replaces.Length == 0) return true;
            __result = JAMethodPatcher.GetInstructions(generator, method, maxTranspilers, jaPatchInfo);
            return false;
        } catch (Exception e) {
            JALib.Instance.LogException(e);
            return true;
        }
    }

    private static PatchInfo GetPatchInfo(MethodBase method) => throw new NotImplementedException();

    #endregion

    public delegate void FailPatch(string patchId, bool disabled);
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

    private static void FindMethod(List<MethodBase> list, Type type, string name, Type[] argumentTypes) {
        if(name == ".ctor") {
            if(argumentTypes == null) list.AddRange(type.Constructors());
            else list.Add(type.Constructor(argumentTypes));
        } else if(name == ".cctor") list.Add(type.TypeInitializer);
        else if(name.EndsWith(".get")) {
            string realName = name[..^4];
            if(realName == "[]") realName = "Item";
            list.Add(type.Getter(realName));
        } else if(name.EndsWith(".set")) {
            string realName = name[..^4];
            if(realName == "[]") realName = "Item";
            list.Add(type.Setter(realName));
        }
        else AddMethods(list, type, name switch {
            "u+" => "op_UnaryPlus",
            "u-" => "op_UnaryNegation",
            "++" => "op_Increment",
            "--" => "op_Decrement",
            "!" => "op_LogicalNot",
            "+" => "op_Addition",
            "-" => "op_Subtraction",
            "*" => "op_Multiply",
            "/" => "op_Division",
            "&" => "op_BitwiseAnd",
            "|" => "op_BitwiseOr",
            "^" => "op_ExclusiveOr",
            "~" => "op_OnesComplement",
            "==" => "op_Equality",
            "!=" => "op_Inequality",
            "<" => "op_LessThan",
            ">" => "op_GreaterThan",
            "<=" => "op_LessThanOrEqual",
            ">=" => "op_GreaterThanOrEqual",
            "<<" => "op_LeftShift",
            ">>" => "op_RightShift",
            "%" => "op_Modulus",
            ".implicit" => "op_Implicit",
            ".explicit" => "op_Explicit",
            ".true" => "op_True",
            ".false" => "op_False",
            _ => name
        }, argumentTypes);
    }

    private static void AddMethods(List<MethodBase> list, Type type, string name, Type[] argumentTypes) {
        if(argumentTypes == null) list.AddRange(type.Methods(name));
        else list.Add(type.Method(name, argumentTypes));
    }

    private void Patch(JAPatchBaseAttribute attribute) {
        try {
            if(attribute.MinVersion > JAPatchBaseAttribute.GetCurrentVersion || attribute.MaxVersion < JAPatchBaseAttribute.GetCurrentVersion) return;
            if(attribute.MethodBase == null) {
                if(attribute is JAOverridePatchAttribute overridePatch) {
                    attribute.MethodName ??= overridePatch.Method.Name;
                    if(attribute.Class != null) attribute.ClassType ??= Type.GetType(attribute.Class);
                    attribute.ClassType ??= overridePatch.Method.DeclaringType.BaseType;
                } else attribute.ClassType ??= Type.GetType(attribute.Class);
                if(attribute.ArgumentTypesType == null && attribute.ArgumentTypes != null) attribute.ArgumentTypesType = new Type[attribute.ArgumentTypes.Length];
                if(attribute.ArgumentTypesType != null && attribute.ArgumentTypes != null) for(int i = 0; i < attribute.ArgumentTypes.Length; i++) attribute.ArgumentTypesType[i] ??= Type.GetType(attribute.ArgumentTypes[i]);
                List<MethodBase> list = [];
                FindMethod(list, attribute.ClassType, attribute.MethodName, attribute.ArgumentTypesType);
                if(list.Count == 1) attribute.MethodBase = list[0];
                else if(list.Count == 0) throw new MissingMethodException();
                else if(attribute is JAOverridePatchAttribute overridePatch2) {
                    Dictionary<MethodBase, int> dictionary = new();
                    foreach(MethodBase @base in list) dictionary[@base] = @base.GetParameters().Length;
                    foreach(ParameterInfo parameter in overridePatch2.Method.GetParameters()) {
                        foreach(MethodBase @base in list) {
                            if(@base.GetParameters().Any(p => p.Name == parameter.Name)) dictionary[@base]--;
                            else if(!parameter.Name.StartsWith("___")) {
                                dictionary.Remove(@base);
                                break;
                            }
                        }
                    }
                    if(dictionary.Count == 1) attribute.MethodBase = dictionary.First().Key;
                    else if(dictionary.Count == 0) throw new MissingMethodException();
                    else if(dictionary.Values.All(v => v == 0)) {
                        (MethodBase, int) min = (null, int.MaxValue);
                        foreach((MethodBase key, int value) in dictionary) if(value < min.Item2) min = (key, value);
                        attribute.MethodBase = min.Item1;
                    } else throw new AmbiguousMatchException();
                } else throw new AmbiguousMatchException();
                if(attribute.GenericType == null && attribute.GenericName != null) attribute.GenericType = new Type[attribute.GenericName.Length];
                if(attribute.GenericType != null) {
                    if(attribute.GenericName != null) for(int i = 0; i < attribute.GenericType.Length; i++) attribute.GenericType[i] ??= Type.GetType(attribute.GenericName[i]);
                    attribute.MethodBase = ((MethodInfo) attribute.MethodBase).MakeGenericMethod(attribute.GenericType);
                }
            }
            if(attribute is JAPatchAttribute patchAttribute) CustomPatch(attribute.MethodBase,
                new HarmonyMethod(attribute.Method, patchAttribute.Priority, patchAttribute.Before, patchAttribute.After, attribute.Debug), patchAttribute, attribute.TryingCatch ? mod : null);
            else if(attribute is JAReversePatchAttribute reversePatchAttribute) CustomReversePatch(attribute.MethodBase, attribute.Method, reversePatchAttribute, mod);
            else if(attribute is JAOverridePatchAttribute overridePatchAttribute) OverridePatch(attribute.MethodBase, attribute.Method, overridePatchAttribute);
            else throw new NotSupportedException("Unsupported Patch Type");
        } catch (Exception e) {
            mod.Error($"Mod {mod.Name} Id {attribute.PatchId} Patch Failed");
            mod.LogException(e);
            bool disabled = attribute is JAPatchAttribute { Disable: true };
            OnFailPatch?.Invoke(attribute.PatchId, disabled);
            if(!disabled) return;
            mod.Error($"Mod {mod.Name} is Disabled.");
            Unpatch();
            throw;
        }
    }

#pragma warning disable CS0618
    private static void CustomPatch(MethodBase original, HarmonyMethod patchMethod, JAPatchAttribute attribute, JAMod mod) {
        lock (typeof(PatchProcessor).GetValue("locker")) {
            PatchInfo patchInfo = GetPatchInfo(original) ?? new PatchInfo();
            JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(original) ?? (jaPatches[original] = new JAPatchInfo());
            string id = attribute.PatchId;
            switch(attribute.PatchType) {
                case PatchType.Prefix:
                    if(CheckRemove(patchMethod.method)) jaPatchInfo.AddRemoves(id, patchMethod);
                    else if(mod != null) jaPatchInfo.AddTryPrefixes(id, patchMethod, mod);
                    else if(_isOldHarmony) patchInfo.AddPrefix(patchMethod.method, id, attribute.Priority, attribute.Before, attribute.After, attribute.Debug);
                    else patchInfo.Invoke("AddPrefixes", id, new[] { patchMethod });
                    break;
                case PatchType.Postfix:
                    if(mod != null) jaPatchInfo.AddTryPostfixes(id, patchMethod, mod);
                    else if(_isOldHarmony) patchInfo.AddPostfix(patchMethod.method, id, attribute.Priority, attribute.Before, attribute.After, attribute.Debug);
                    else patchInfo.Invoke("AddPostfixes", id, new[] { patchMethod });
                    break;
                case PatchType.Transpiler:
                    if(_isOldHarmony) patchInfo.AddTranspiler(patchMethod.method, id, attribute.Priority, attribute.Before, attribute.After, attribute.Debug);
                    else patchInfo.Invoke("AddTranspilers", id, new[] { patchMethod });
                    break;
                case PatchType.Finalizer:
                    if(_isOldHarmony) patchInfo.AddFinalizer(patchMethod.method, id, attribute.Priority, attribute.Before, attribute.After, attribute.Debug);
                    else patchInfo.Invoke("AddFinalizers", id, new[] { patchMethod });
                    break;
                case PatchType.Replace:
                    jaPatchInfo.AddReplaces(id, patchMethod);
                    break;
            }
            MethodInfo replacement = PatchUpdateWrapper(original, patchInfo, jaPatchInfo);
            MethodInfo updateMethod = typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Method("UpdatePatchInfo");
            updateMethod.Invoke(null, updateMethod.GetParameters().Length == 2 ? [original, patchInfo] : [original, replacement, patchInfo]);
        }
    }
#pragma warning restore CS0618

    private static void CustomReversePatch(MethodBase original, MethodInfo patchMethod, JAReversePatchAttribute attribute, JAMod mod) {
        PatchInfo patchInfo = GetPatchInfo(original) ?? new PatchInfo();
        JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(original) ?? (jaPatches[original] = new JAPatchInfo());
        UpdateReversePatch(attribute.Data ??= new ReversePatchData {
            original = original,
            patchMethod = patchMethod,
            debug = attribute.Debug,
            attribute = attribute,
            mod = mod
        }, patchInfo, jaPatchInfo);
        if(attribute.PatchType != ReversePatchType.Original && !attribute.PatchType.HasFlag(ReversePatchType.DontUpdate)) jaPatchInfo.AddReversePatches(attribute.Data);
    }

    private static void OverridePatch(MethodBase original, MethodInfo patchMethod, JAOverridePatchAttribute attribute) {
        if(patchMethod.IsStatic) throw new NotSupportedException("Static Method Override");
        Type originalType;
        if(original.IsStatic) {
            if(original.GetParameters().Length == 0) throw new NotSupportedException("Static Method with no Parameters");
            originalType = original.GetParameters()[0].ParameterType;
        } else originalType = original.DeclaringType;
        Type patchType = patchMethod.DeclaringType;
        if(originalType == patchType) throw new NotSupportedException("Same Type Override");
        if(!originalType.IsAssignableFrom(patchType) && !patchType.IsAssignableFrom(originalType) && !patchType.IsInterface && !originalType.IsInterface) throw new NotSupportedException("Incompatible Types");
        PatchInfo patchInfo = GetPatchInfo(original) ?? new PatchInfo();
        JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(original) ?? (jaPatches[original] = new JAPatchInfo());
        attribute.targetType ??= attribute.targetTypeName == null ? patchMethod.DeclaringType : Type.GetType(attribute.targetTypeName);
        OverridePatchData data = new() {
            patchMethod = patchMethod,
            debug = attribute.Debug,
            IgnoreBasePatch = attribute.IgnoreBasePatch,
            targetType = attribute.targetType
        };
        jaPatchInfo.AddOverridePatches(data);
        PatchUpdateWrapper(original, patchInfo, jaPatchInfo);
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
                    PatchInfo patchInfo = GetPatchInfo(patchAttribute.MethodBase) ?? new PatchInfo();
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
                if(reversePatchAttribute.PatchType == ReversePatchType.Original || reversePatchAttribute.PatchType.HasFlag(ReversePatchType.DontUpdate)) continue;
                JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(reversePatchAttribute.Data.original);
                if(jaPatchInfo == null) continue;
                jaPatchInfo.reversePatches = jaPatchInfo.reversePatches.Where(patch => patch != reversePatchAttribute.Data).ToArray();
            } else if(baseAttribute is JAOverridePatchAttribute overridePatchAttribute) {
                JAPatchInfo jaPatchInfo = jaPatches.GetValueOrDefault(overridePatchAttribute.MethodBase);
                if(jaPatchInfo == null) continue;
                jaPatchInfo.overridePatches = jaPatchInfo.overridePatches.Where(patch => patch.patchMethod != overridePatchAttribute.Method).ToArray();
            }
        }
    }

    private static void RemovePatch<T>(MethodInfo methodInfo, string id, ref T[] patches) where T : HarmonyLib.Patch =>
        patches = patches.Where(patch => patch.owner != id && patch.PatchMethod != methodInfo).ToArray();


    public JAPatcher AddPatch(Type type) {
        foreach(MethodInfo method in type.Methods()) AddPatch(method);
        return this;
    }

    public JAPatcher AddPatch(Type type, PatchBinding binding) {
        foreach(MethodInfo method in type.Methods())
            foreach(JAPatchBaseAttribute attribute in method.GetCustomAttributes<JAPatchBaseAttribute>())
                switch(attribute) {
                    case JAReversePatchAttribute when !binding.HasFlag(PatchBinding.Reverse):
                    case JAOverridePatchAttribute when !binding.HasFlag(PatchBinding.Override):
                        continue;
                    case JAPatchAttribute patch:
                        switch(patch.PatchType) {
                            case PatchType.Prefix when !binding.HasFlag(PatchBinding.Prefix):
                            case PatchType.Postfix when !binding.HasFlag(PatchBinding.Postfix):
                            case PatchType.Transpiler when !binding.HasFlag(PatchBinding.Transpiler):
                            case PatchType.Finalizer when !binding.HasFlag(PatchBinding.Finalizer):
                            case PatchType.Replace when !binding.HasFlag(PatchBinding.Replace):
                                continue;
                            default:
                                goto default;
                        }
                    default:
                        attribute.Method = method;
                        AddPatch(attribute);
                        break;
                }
        return this;
    }

    public JAPatcher AddAllPatch(PatchBinding binding) => AddAllPatch(mod.GetType().Assembly, binding);

    public JAPatcher AddAllPatch(Assembly assembly, PatchBinding binding) {
        foreach(Type type in assembly.GetTypes()) AddPatch(type, binding);
        return this;
    }

    public JAPatcher AddPatch(string nameSpace) => AddPatch(mod.GetType().Assembly, nameSpace);

    public JAPatcher AddPatch(string nameSpace, PatchBinding binding) => AddPatch(mod.GetType().Assembly, nameSpace, binding);

    public JAPatcher AddPatch(Assembly assembly, string nameSpace) {
        foreach(Type type in assembly.GetTypes().Where(t => t.Namespace == nameSpace)) AddPatch(type);
        return this;
    }

    public JAPatcher AddPatch(Assembly assembly, string nameSpace, PatchBinding binding) {
        foreach(Type type in assembly.GetTypes().Where(t => t.Namespace == nameSpace)) AddPatch(type, binding);
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