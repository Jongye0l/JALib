using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch;

public class JAPatcher : IDisposable {

    private static bool _isOldHarmony;
    private List<JAPatchBaseAttribute> patchData;
    private JAMod mod;
    public event FailPatch OnFailPatch;
    public bool patched { get; private set; }
    public bool usingWaiting = true;
    private static bool _doNotUnPatch;

    #region CustomPatchPatching
    internal static Dictionary<MethodBase, JAInternalPatchInfo> JaPatches = new();
    private static PatchWaiter _patchWaiter;
    internal static object HarmonyLocker = typeof(PatchProcessor).GetValue("locker");

    static JAPatcher() {
        JALogger.LogInternal("Starting JAPatcher Initialization...(0/4)");
        Harmony harmony = JALib.Harmony = new Harmony(JALib.ModId);
        Assembly assembly = typeof(Harmony).Assembly;
        Type patchFunctions = assembly.GetType("HarmonyLib.PatchFunctions");
        _isOldHarmony = assembly.GetName().Version < new Version(2, 0, 3, 0);
        if(_isOldHarmony) harmony.Patch(typeof(HarmonyLib.Patch).Constructor(), new HarmonyMethod(((Delegate) FixPatchCtorNull).Method));
        JALogger.LogInternal("Starting JAPatcher Reverse Patches.(1/4)");
        harmony.CreateReversePatcher(patchFunctions.Method("UpdateWrapper"), new HarmonyMethod(((Delegate) PatchUpdateWrapperReverse).Method)).Patch();
        MethodInfo reversePatchMethod = patchFunctions.Method("ReversePatch");
        harmony.CreateReversePatcher(reversePatchMethod, new HarmonyMethod(((Delegate) PatchReversePatchReverse).Method)).Patch();
        harmony.CreateReversePatcher(reversePatchMethod, new HarmonyMethod(((Delegate) UpdateReversePatch).Method)).Patch();
        Type methodPatcher = assembly.GetType("HarmonyLib.MethodPatcher");
        harmony.CreateReversePatcher(methodPatcher.Method("PrefixAffectsOriginal"), new HarmonyMethod(((Delegate) JAMethodPatcher.PrefixAffectsOriginal).Method)).Patch();
        harmony.Patch(((Delegate) JAMethodPatcher.AddOverride).Method, transpiler: new HarmonyMethod(((Delegate) JAMethodPatcher.EmitterPatch).Method));
        harmony.CreateReversePatcher(methodPatcher.Method("CreateReplacement"), new HarmonyMethod(((Delegate) JAMethodPatcher.CreateReplacement).Method)).Patch();
        harmony.CreateReversePatcher(assembly.GetType("HarmonyLib.HarmonySharedState").Method("GetPatchInfo"), new HarmonyMethod(((Delegate) GetPatchInfo).Method)).Patch();
        MethodInfo updateMethod = typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Method("UpdatePatchInfo");
        harmony.CreateReversePatcher(updateMethod, new HarmonyMethod(((Delegate) UpdatePatchInfo).Method)).Patch();
        harmony.CreateReversePatcher(updateMethod, new HarmonyMethod(((Delegate) UpdatePatchInfoOnlyPatchInfo).Method)).Patch();
        harmony.CreateReversePatcher(updateMethod, new HarmonyMethod(((Delegate) UpdatePatchInfoOnlyReplacement).Method)).Patch();
        harmony.Patch(((Delegate) JAMethodPatcher.SortPatchMethods).Method, transpiler: new HarmonyMethod(((Delegate) JAMethodPatcher.SortPatchMethodsTranspiler).Method));
        JALogger.LogInternal("Start Enter the Harmony Locker.(2/4)");
        lock(HarmonyLocker) {
            JALogger.LogInternal("Start JAPatcher Patches.(3/4)");
            JAMethodPatcher.LoadAddPrePostMethod(harmony);
            harmony.Patch(patchFunctions.Method("UpdateWrapper"), new HarmonyMethod(((Delegate) PatchUpdateWrapperPatch).Method));
            harmony.Patch(patchFunctions.Method("ReversePatch"), new HarmonyMethod(((Delegate) PatchReversePatchPatch).Method));
            harmony.Patch(assembly.GetType("HarmonyLib.MethodCopier").Method("GetInstructions"), new HarmonyMethod(((Delegate) GetInstructions).Method));
            JAPatchAttribute attribute = new(typeof(PatchProcessor).Method("Unpatch", typeof(MethodInfo)), PatchType.Replace, false) {
                Method = ((Delegate) UnpatchPatch1).Method
            };
            CustomPatch(attribute.MethodBase, attribute, null);
            attribute = new JAPatchAttribute(typeof(PatchProcessor).Method("Unpatch", typeof(HarmonyPatchType), typeof(string)), PatchType.Replace, false) {
                Method = ((Delegate) UnpatchPatch2).Method
            };
            CustomPatch(attribute.MethodBase, attribute, null);
        }
        JALogger.LogInternal("Complete JAPatcher Initialization.(4/4)");
    }

    private static void FixPatchCtorNull(ref string[] before, ref string[] after) {
        before ??= [];
        after ??= [];
    }

    private static bool PatchUpdateWrapperPatch(MethodBase original, PatchInfo patchInfo, ref MethodInfo __result) {
        try {
            if(!JaPatches.TryGetValue(original, out JAInternalPatchInfo jaPatchInfo)) return true;
            __result = PatchUpdateWrapper(original, patchInfo, jaPatchInfo);
            return false;
        } catch (Exception e) {
            JALib.Instance.LogReportException("Fail Patch Method '" + original.FullDescription() + '\'', e);
            return true;
        }
    }

    private static MethodInfo PatchUpdateWrapper(MethodBase original, PatchInfo patchInfo, JAInternalPatchInfo jaInternalPatchInfo) {
        if(JALib.Instance?.Setting?.logPatches ?? false) 
            JALib.Instance.Log("Patching Method '" + original.FullDescription() + '\'', 1);
        MethodInfo replacement = PatchUpdateWrapperReverse(original, patchInfo, jaInternalPatchInfo);
        foreach(ReversePatchData reversePatch in jaInternalPatchInfo.reversePatches) {
            if(_patchWaiter == null) UpdateReversePatch(reversePatch, patchInfo, jaInternalPatchInfo);
            else _patchWaiter.AddReversePatch(reversePatch);
        }
        return replacement;
    }

    private static MethodInfo PatchUpdateWrapperReverse(MethodBase original, PatchInfo patchInfo, JAInternalPatchInfo jaInternalPatchInfo) {
        _ = Transpiler(null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> list = [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Newobj, typeof(JAMethodPatcher).Constructor(typeof(MethodBase), typeof(PatchInfo), typeof(JAInternalPatchInfo)))
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
            list.Add(new CodeInstruction(OpCodes.Call, ((Delegate) JAMethodPatcher.CreateReplacement).Method));
            while(enumerator.MoveNext()) list.Add(enumerator.Current);
            return list;
        }
    }

    private static bool PatchReversePatchPatch(HarmonyMethod standin, MethodBase original, MethodInfo postTranspiler, ref MethodInfo __result) {
        try {
            if(standin == null || standin.method == null ||
               standin.reversePatchType == HarmonyReversePatchType.Snapshot ||
               !JaPatches.TryGetValue(original, out JAInternalPatchInfo jaPatchInfo) ||
               jaPatchInfo.replaces.Length == 0) return true;
            __result = PatchReversePatchReverse(standin, original, postTranspiler, jaPatchInfo);
            return false;
        } catch (Exception e) {
            JALib.Instance.LogReportException("Fail Reverse Patch Method '" + original.FullDescription() + "' to '" + standin.method.FullDescription() + '\'', e);
            return true;
        }
    }

    private static MethodInfo PatchReversePatchReverse(HarmonyMethod standin, MethodBase original, MethodInfo postTranspiler, JAInternalPatchInfo jaInternalPatchInfo) {
        _ = Transpiler(null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> list = [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldarg_3),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Newobj, typeof(JAMethodPatcher).Constructor(typeof(HarmonyMethod), typeof(MethodBase), typeof(JAInternalPatchInfo), typeof(MethodInfo)))
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

    private static MethodInfo UpdateReversePatch(ReversePatchData data, PatchInfo patchInfo, JAInternalPatchInfo jaInternalPatchInfo) {
        _ = Transpiler(null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> list = [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Newobj, typeof(JAMethodPatcher).Constructor(typeof(ReversePatchData), typeof(PatchInfo), typeof(JAInternalPatchInfo)))
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
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                if(code.operand is FieldInfo { Name: "method" }) code.operand = SimpleReflect.Field(typeof(ReversePatchData), "patchMethod");
                list.Add(code);
            }
            return list;
        }
    }

    internal static bool GetInstructions(ILGenerator generator, MethodBase method, int maxTranspilers, ref List<CodeInstruction> __result) {
        try {
            if(method == null || generator == null || maxTranspilers < 1 || !JaPatches.TryGetValue(method, out JAInternalPatchInfo jaPatchInfo) || jaPatchInfo.replaces.Length == 0) return true;
            __result = JAMethodPatcher.GetInstructions(generator, method, maxTranspilers, jaPatchInfo);
            return false;
        } catch (Exception e) {
            JALib.Instance.LogReportException("Fail to Get Instructions '" + method.FullDescription() + '\'', e);
            return true;
        }
    }

    internal static PatchInfo GetPatchInfo(MethodBase method) => throw new NotImplementedException();

    private static PatchProcessor UnpatchPatch1(PatchProcessor __instance, MethodInfo patch, MethodBase ___original) {
        Unpatch(___original, patch);
        return __instance;
    }

    private static PatchProcessor UnpatchPatch2(PatchProcessor __instance, HarmonyPatchType type, string harmonyID, MethodBase ___original) {
        Unpatch(___original, type switch {
            HarmonyPatchType.Prefix => AllPatchType.AllPrefix,
            HarmonyPatchType.Postfix => AllPatchType.AllPostfix,
            HarmonyPatchType.Transpiler => AllPatchType.AllTranspiler,
            HarmonyPatchType.Finalizer => AllPatchType.Finalizer,
            HarmonyPatchType.ReversePatch => AllPatchType.Reverse,
            HarmonyPatchType.All => AllPatchType.All,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        }, harmonyID);
        return __instance;
    }

    #endregion

    [Obsolete("Deprecated. Use JAPatchManager.GetPatchData instead.", true)]
    public static PatchData GetPatchData(MethodBase method) => JAPatchManager.GetPatchData(method);

    public static void Unpatch(MethodBase original, MethodInfo patch) {
        lock(HarmonyLocker) {
            PatchInfo patchInfo = GetPatchInfo(original) ?? new PatchInfo();
            JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(original) ?? new JAInternalPatchInfo();
            patchInfo.RemovePatch(patch);
            jaInternalPatchInfo.RemovePatch(patch);
            MethodInfo replacement = PatchUpdateWrapper(original, patchInfo, jaInternalPatchInfo);
            MethodInfo updateMethod = typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Method("UpdatePatchInfo");
            updateMethod.Invoke(null, updateMethod.GetParameters().Length == 2 ? [original, patchInfo] : [original, replacement, patchInfo]);
        }
    }

    public static void Unpatch(MethodBase original, AllPatchType type, string id) {
        lock(HarmonyLocker) {
            PatchInfo patchInfo = GetPatchInfo(original) ?? new PatchInfo();
            JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(original) ?? new JAInternalPatchInfo();
            if(type.HasFlag(AllPatchType.Prefix)) patchInfo.RemovePrefix(id);
            if(type.HasFlag(AllPatchType.Postfix)) patchInfo.RemovePostfix(id);
            if(type.HasFlag(AllPatchType.Transpiler)) patchInfo.RemoveTranspiler(id);
            if(type.HasFlag(AllPatchType.Finalizer)) patchInfo.RemoveFinalizer(id);
            if(type.HasFlag(AllPatchType.TryPrefix)) jaInternalPatchInfo.RemoveTryPrefix(id);
            if(type.HasFlag(AllPatchType.TryPostfix)) jaInternalPatchInfo.RemoveTryPostfix(id);
            if(type.HasFlag(AllPatchType.Replace)) jaInternalPatchInfo.RemoveReplace(id);
            if(type.HasFlag(AllPatchType.Remove)) jaInternalPatchInfo.RemoveRemove(id);
            if(type.HasFlag(AllPatchType.Override)) jaInternalPatchInfo.RemoveOverridePatch(id);
            MethodInfo replacement = PatchUpdateWrapper(original, patchInfo, jaInternalPatchInfo);
            MethodInfo updateMethod = typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Method("UpdatePatchInfo");
            updateMethod.Invoke(null, updateMethod.GetParameters().Length == 2 ? [original, patchInfo] : [original, replacement, patchInfo]);
        }
    }

    public delegate void FailPatch(string patchId, bool disabled);
    public JAPatcher(JAMod mod) {
        this.mod = mod;
        patchData = [];
    }

    public void Patch() {
        if(patched) return;
        patched = true;
        bool addedPatchWaiter = _patchWaiter == null;
        if(addedPatchWaiter) _patchWaiter = new PatchWaiter();
        _patchWaiter.AddPatcher(this);
        foreach(JAPatchBaseAttribute attribute in patchData) {
            try {
                Patch(attribute);
            } catch (Exception) {
                break;
            }
        }
        if(!usingWaiting) RunWaiterPatchForce();
        else if(addedPatchWaiter) RunWaiterPatch();
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
            if(attribute.MinVersion > VersionControl.releaseNumber || attribute.MaxVersion < VersionControl.releaseNumber) return;
            if(attribute.MethodBase == null) {
                JAOverridePatchAttribute overridePatch = attribute as JAOverridePatchAttribute;
                if(overridePatch != null) {
                    attribute.MethodName ??= overridePatch.Method.Name;
                    if(attribute.Class != null) attribute.ClassType ??= SimpleReflect.GetType(attribute.Class);
                    attribute.ClassType ??= overridePatch.Method.DeclaringType.BaseType;
                } else attribute.ClassType ??= SimpleReflect.GetType(attribute.Class);
                if(attribute.ArgumentTypesType == null && attribute.ArgumentTypes != null) attribute.ArgumentTypesType = new Type[attribute.ArgumentTypes.Length];
                if(attribute.ArgumentTypesType != null && attribute.ArgumentTypes != null) for(int i = 0; i < attribute.ArgumentTypes.Length; i++) attribute.ArgumentTypesType[i] ??= Type.GetType(attribute.ArgumentTypes[i]);
                List<MethodBase> list = [];
                FindMethod(list, attribute.ClassType, attribute.MethodName, attribute.ArgumentTypesType);
                if(list.Count == 1) attribute.MethodBase = list[0];
                else if(list.Count == 0) throw new MissingMethodException();
                else if(overridePatch != null) {
                    Dictionary<MethodBase, int> dictionary = new();
                    foreach(MethodBase @base in list) dictionary[@base] = @base.GetParameters().Length;
                    foreach(ParameterInfo parameter in overridePatch.Method.GetParameters()) {
                        foreach(MethodBase @base in list) {
                            if(@base.GetParameters().Any(p => p.Name == parameter.Name)) dictionary[@base]--;
                            else if(!parameter.Name.StartsWith("__")) {
                                dictionary.Remove(@base);
                                break;
                            }
                        }
                    }
                    if(dictionary.Count == 1) attribute.MethodBase = dictionary.First().Key;
                    else if(dictionary.Count == 0) throw new MissingMethodException();
                    else {
                        KeyValuePair<MethodBase, int> min = new(null, int.MaxValue);
                        foreach(KeyValuePair<MethodBase, int> value in dictionary) if(value.Value < min.Value) min = value;
                        attribute.MethodBase = min.Key;
                    }
                } else throw new AmbiguousMatchException();
                if(attribute.GenericType == null && attribute.GenericName != null) attribute.GenericType = new Type[attribute.GenericName.Length];
                if(attribute.GenericType != null) {
                    if(attribute.GenericName != null) for(int i = 0; i < attribute.GenericType.Length; i++) attribute.GenericType[i] ??= Type.GetType(attribute.GenericName[i]);
                    attribute.MethodBase = ((MethodInfo) attribute.MethodBase).MakeGenericMethod(attribute.GenericType);
                }
            }
            if(attribute is JAPatchAttribute patchAttribute) CustomPatch(attribute.MethodBase, patchAttribute, attribute.TryingCatch ? mod : null);
            else if(attribute is JAReversePatchAttribute reversePatchAttribute) CustomReversePatch(attribute.MethodBase, attribute.Method, reversePatchAttribute, mod);
            else if(attribute is JAOverridePatchAttribute overridePatchAttribute) OverridePatch(attribute.MethodBase, attribute.Method, overridePatchAttribute, mod);
            else throw new NotSupportedException("Unsupported Patch Type");
        } catch (Exception e) {
            if(PatchFailAction(attribute, e)) {
                mod.Error("Patch disabled is true, unpatching...");
                Unpatch();
                throw;
            }
        }
    }

    private bool PatchFailAction(JAPatchBaseAttribute attribute, Exception e) {
        try {
            StringBuilder keyBuilder = new();
            if(attribute == null) keyBuilder.Append("Unknown Patch");
            else {
                if(attribute is JAPatchAttribute patchAttribute) keyBuilder.Append(patchAttribute.PatchType);
                else if(attribute is JAReversePatchAttribute) keyBuilder.Append("Reverse");
                else if(attribute is JAOverridePatchAttribute overridePatchAttribute)
                    keyBuilder.Append("Override ").Append(overridePatchAttribute.targetType?.FullName ?? overridePatchAttribute.targetTypeName).Append(" Type");
                else keyBuilder.Append("Unknown");
                keyBuilder.Append(" Patch ").Append(attribute.PatchId).Append(" to ");
                if((object) attribute.MethodBase != null) keyBuilder.Append(attribute.MethodBase?.DeclaringType?.FullName).Append('.').Append(attribute.MethodBase?.Name);
                else keyBuilder.Append(attribute.MethodName);
            }
            keyBuilder.Append(" Failed");
            mod.LogReportException(keyBuilder.ToString(), e, 1);
        } catch (Exception exception) {
            mod.LogReportException("Fail to Build Patch Error Message", exception);
            mod.LogReportException("Original Patch Exception", e, 1);
        }
        bool disabled = attribute is JAPatchAttribute { Disable: true };
        try {
            OnFailPatch?.Invoke(attribute.PatchId, disabled);
        } catch (Exception exception) {
            mod.LogReportException("Fail to Invoke OnFailPatch Event", exception);
        }
        return disabled;
    }

#pragma warning disable CS0618
    private static void CustomPatch(MethodBase original, JAPatchAttribute attribute, JAMod mod) {
        if(!attribute.Method.IsStatic) throw new NotSupportedException("Patch Method is need to be Static");
        lock(HarmonyLocker) {
            PatchInfo patchInfo = GetPatchInfo(original) ?? new PatchInfo();
            JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(original) ?? (JaPatches[original] = new JAInternalPatchInfo());
            AddPatchInfo(attribute, patchInfo, jaInternalPatchInfo, mod, out bool updateHarmonyPatchInfo);
            List<MethodBase> warningPatches = [];
            foreach(HarmonyLib.Patch prefix in patchInfo.prefixes) 
                if(prefix.PatchMethod.ReturnType == typeof(bool)) warningPatches.Add(prefix.PatchMethod);
            foreach(TriedPatchData tryPrefix in jaInternalPatchInfo.tryPrefixes) 
                if(tryPrefix.PatchMethod.ReturnType == typeof(bool)) warningPatches.Add(tryPrefix.PatchMethod);
            foreach(HarmonyLib.Patch remove in jaInternalPatchInfo.removes) warningPatches.Add(remove.PatchMethod);
            foreach(HarmonyLib.Patch replace in jaInternalPatchInfo.replaces) warningPatches.Add(replace.PatchMethod);
            if(warningPatches.Count > 1) {
                StringBuilder sb = new();
                foreach(MethodBase method in warningPatches) sb.AppendLine(" - " + method.FullDescription());
                sb.Append('\n');
                if(JALib.Instance.Setting.logPrefixWarn) {
                    JALib.Instance.Warning("Multiple Prefix Patches that return bool(or Replace) detected on method '" + original.FullDescription() + "':");
                    Console.WriteLine(sb.ToString());
                }
            }
            if(_patchWaiter == null) {
                try {
                    MethodInfo replacement = PatchUpdateWrapper(original, patchInfo, jaInternalPatchInfo);
                    if(updateHarmonyPatchInfo) UpdatePatchInfo(original, replacement, patchInfo);
                    else UpdatePatchInfoOnlyReplacement(original, replacement);
                } catch (Exception e) {
                    throw new JAPatchException(original, patchInfo, jaInternalPatchInfo, e);
                }
            } else {
                if(updateHarmonyPatchInfo) UpdatePatchInfoOnlyPatchInfo(original, patchInfo);
                _patchWaiter.AddNormalPatch(original);
            }
        }
    }

    private static void AddPatchInfo(JAPatchAttribute attribute, PatchInfo patchInfo, JAInternalPatchInfo jaInternalPatchInfo, JAMod mod, out bool updateHarmonyPatchInfo) {
        string id = attribute.PatchId;
        HarmonyMethod patchMethod = new(attribute.Method, attribute.Priority, attribute.Before, attribute.After, attribute.Debug);
        updateHarmonyPatchInfo = false;
        switch(attribute.PatchType) {
            case PatchType.Prefix:
                if(CheckRemove(patchMethod.method)) jaInternalPatchInfo.AddRemoves(id, patchMethod);
                else if(mod != null) jaInternalPatchInfo.AddTryPrefixes(id, patchMethod, mod);
                else {
                    if(_isOldHarmony) patchInfo.AddPrefix(patchMethod.method, id, attribute.Priority, attribute.Before, attribute.After, attribute.Debug);
                    else patchInfo.Invoke("AddPrefixes", id, new[] { patchMethod });
                    updateHarmonyPatchInfo = true;
                }
                break;
            case PatchType.Postfix:
                if(mod != null) jaInternalPatchInfo.AddTryPostfixes(id, patchMethod, mod);
                else {
                    if(_isOldHarmony) patchInfo.AddPostfix(patchMethod.method, id, attribute.Priority, attribute.Before, attribute.After, attribute.Debug);
                    else patchInfo.Invoke("AddPostfixes", id, new[] { patchMethod });
                    updateHarmonyPatchInfo = true;
                }
                break;
            case PatchType.Transpiler:
                if(_isOldHarmony) patchInfo.AddTranspiler(patchMethod.method, id, attribute.Priority, attribute.Before, attribute.After, attribute.Debug);
                else patchInfo.Invoke("AddTranspilers", id, new[] { patchMethod });
                updateHarmonyPatchInfo = true;
                break;
            case PatchType.Finalizer:
                if(_isOldHarmony) patchInfo.AddFinalizer(patchMethod.method, id, attribute.Priority, attribute.Before, attribute.After, attribute.Debug);
                else patchInfo.Invoke("AddFinalizers", id, new[] { patchMethod });
                updateHarmonyPatchInfo = true;
                break;
            case PatchType.Replace:
                jaInternalPatchInfo.AddReplaces(id, patchMethod);
                break;
        }
    }

    private static void UpdatePatchInfo(MethodBase original, MethodInfo replacement, PatchInfo patchInfo) {
        _ = Transpiler(null, null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method) {
            if(method.GetParameters().Length == 3) return instructions;
            List<CodeInstruction> list = instructions.ToList();
            foreach(CodeInstruction instruction in list) {
                if(instruction.opcode == OpCodes.Ldarg_2) {
                    instruction.opcode = OpCodes.Ldarg_3;
                    break;
                }
            }
            return list;
        }
    }

    private static void UpdatePatchInfoOnlyPatchInfo(MethodBase original, PatchInfo patchInfo) {
        _ = Transpiler(null, null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method) {
            if(method.GetParameters().Length == 2) return instructions;
            List<CodeInstruction> list = [];
            foreach(CodeInstruction instruction in instructions) {
                if(instruction.operand is FieldInfo { Name: "originals" }) {
                    instruction.opcode = OpCodes.Ret;
                    instruction.operand = null;
                    list.Add(instruction);
                    break;
                }
                if(instruction.opcode == OpCodes.Ldarg_2) instruction.opcode = OpCodes.Ldarg_1;
                list.Add(instruction);
            }
            return list;
        }
    }

    private static void UpdatePatchInfoOnlyReplacement(MethodBase original, MethodInfo replacement) {
        _ = Transpiler(null, null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method) {
            if(method.GetParameters().Length == 2) return [ 
                new CodeInstruction(OpCodes.Ret)
            ];
            List<CodeInstruction> list = [];
            bool start = false;
            foreach(CodeInstruction instruction in instructions) {
                if(instruction.operand is FieldInfo { Name: "originals" }) {
                    start = true;
                    instruction.labels.Clear();
                }
                if(!start) continue;
                list.Add(instruction);
            }
            return list;
        }
    }
#pragma warning restore CS0618

    private static void CustomReversePatch(MethodBase original, MethodInfo patchMethod, JAReversePatchAttribute attribute, JAMod mod) {
        lock(HarmonyLocker) {
            PatchInfo patchInfo = GetPatchInfo(original) ?? new PatchInfo();
            JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(original) ?? (JaPatches[original] = new JAInternalPatchInfo());
            attribute.Data ??= new ReversePatchData {
                Original = original,
                PatchMethod = patchMethod,
                Debug = attribute.Debug,
                Attribute = attribute,
                Mod = mod
            };
            if(_patchWaiter == null) {
                try {
                    UpdateReversePatch(attribute.Data, patchInfo, jaInternalPatchInfo);
                } catch (Exception e) {
                    throw new JAPatchException(original, patchInfo, jaInternalPatchInfo, e);
                }
            } else _patchWaiter.AddReversePatch(attribute.Data);
            if(attribute.PatchType != ReversePatchType.Original && !attribute.PatchType.HasFlag(ReversePatchType.DontUpdate)) jaInternalPatchInfo.AddReversePatches(attribute.Data);
        }
    }

    private static void OverridePatch(MethodBase original, MethodInfo patchMethod, JAOverridePatchAttribute attribute, JAMod mod) {
        if(patchMethod.IsStatic) throw new NotSupportedException("Static Method Override");
        Type originalType;
        if(original.IsStatic) {
            if(original.GetParameters().Length == 0) throw new NotSupportedException("Static Method with no Parameters");
            originalType = original.GetParameters()[0].ParameterType;
        } else originalType = original.DeclaringType;
        Type patchType = patchMethod.DeclaringType;
        if(originalType == patchType) throw new NotSupportedException("Same Type Override");
        if(attribute.checkType && !originalType.IsAssignableFrom(patchType) && !patchType.IsAssignableFrom(originalType) && !patchType.IsInterface && !originalType.IsInterface) throw new NotSupportedException("Incompatible Types");
        lock(HarmonyLocker) {
            JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(original) ?? (JaPatches[original] = new JAInternalPatchInfo());
            attribute.targetType ??= attribute.targetTypeName == null ? patchMethod.DeclaringType : Type.GetType(attribute.targetTypeName);
            AddOverridePatch(patchMethod, attribute, jaInternalPatchInfo, mod);
            if(_patchWaiter == null) {
                PatchInfo patchInfo = GetPatchInfo(original) ?? new PatchInfo();
                try {
                    MethodInfo replacement = PatchUpdateWrapper(original, patchInfo, jaInternalPatchInfo);
                    UpdatePatchInfoOnlyReplacement(original, replacement);
                } catch (Exception e) {
                    throw new JAPatchException(original, patchInfo, jaInternalPatchInfo, e);
                }
            } else {
                _patchWaiter.AddNormalPatch(original);
            }
        }
    }

    private static void AddOverridePatch(MethodInfo patchMethod, JAOverridePatchAttribute attribute, JAInternalPatchInfo jaInternalPatchInfo, JAMod mod) {
        jaInternalPatchInfo.AddOverridePatches(new OverridePatchData(patchMethod, attribute, mod));
    }

    public static void RunWaiterPatchForce() {
        if(_patchWaiter == null) return;
        try {
            PatchWaiter patchWaiter = _patchWaiter;
            lock(HarmonyLocker) {
                MethodBase[] normalPatches = patchWaiter.NormalPatches.ToArray();
                for(int i = 0; i < normalPatches.Length; i++) {
                    MethodBase method = normalPatches[i];
                    PatchInfo patchInfo = GetPatchInfo(method) ?? new PatchInfo();
                    JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(method) ?? new JAInternalPatchInfo();
                    try {
                        MethodInfo replacement = PatchUpdateWrapper(method, patchInfo, jaInternalPatchInfo);
                        UpdatePatchInfoOnlyReplacement(method, replacement);
                    } catch (Exception e) {
                        try {
                            _doNotUnPatch = true;
                            e = new JAPatchException(method, patchInfo, jaInternalPatchInfo, e);
                            (PatchInfo newPatch, JAInternalPatchInfo newJaPatch) = ClonePatch(patchInfo, jaInternalPatchInfo);
                            JAPatcher[] patchers = patchWaiter.PendingPatcher.ToArray();
                            int j;
                        
                            for(j = 0; j < i; j++) patchWaiter.NormalPatches.Remove(normalPatches[j]);
                            JAPatchBaseAttribute[][] errorPatchesArray = new JAPatchBaseAttribute[patchers.Length][];
                        
                            for(j = patchers.Length - 1; j >= 0; j--) {
                                if(!patchers[j].FoundErrorPatch(method, ref patchInfo, jaInternalPatchInfo, newPatch, newJaPatch, ref e, out errorPatchesArray[j])) continue;
                                try {
                                    MethodInfo replacement = PatchUpdateWrapper(method, patchInfo, jaInternalPatchInfo);
                                    UpdatePatchInfo(method, replacement, patchInfo);
                                    goto Skip;
                                } catch (Exception) {
                                    break;
                                }
                            }
                        
                            for(; j < patchers.Length; j++) {
                                MethodInfo reverted = patchers[j].RevertErrorPatch(method, ref patchInfo, jaInternalPatchInfo, newPatch, newJaPatch, errorPatchesArray[j]);
                                if((object) reverted == null) continue;
                                UpdatePatchInfoOnlyReplacement(method, reverted);
                                goto Skip;
                            }
                        
                            UpdatePatchInfoOnlyPatchInfo(method, newPatch);
                            JaPatches[method] = newJaPatch;
                        
                            Skip:
                            patchWaiter.NormalPatches.Remove(method);
                            normalPatches = patchWaiter.NormalPatches.ToArray();
                            i = -1;
                        } finally {
                            _doNotUnPatch = false;
                        }
                    }
                }
                _patchWaiter = null;
                ReversePatchData[] reversePatches = patchWaiter.ReversePatches.ToArray();
                for(int i = 0; i < reversePatches.Length; i++) {
                    ReversePatchData data = reversePatches[i];
                    MethodBase method = data.Original;
                    PatchInfo patchInfo = GetPatchInfo(method) ?? new PatchInfo();
                    JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(method) ?? new JAInternalPatchInfo();
                    try {
                        UpdateReversePatch(data, patchInfo, jaInternalPatchInfo);
                    } catch (Exception e) {
                        foreach(JAPatcher patcher in patchWaiter.PendingPatcher) {
                            if(patcher.patchData.Contains(data.Attribute)) {
                                if(patcher.PatchFailAction(data.Attribute, new JAPatchException(data.PatchMethod, patchInfo, jaInternalPatchInfo, e))) {
                                    data.Mod.Error("Patch disabled is true, unpatching...");
                                    for(int j = 0; j < i; j++) patchWaiter.ReversePatches.Remove(reversePatches[j]);
                                    foreach(JAPatchBaseAttribute baseAttribute in patcher.patchData) {
                                        if(baseAttribute is not JAReversePatchAttribute reverse) continue;
                                        patchWaiter.ReversePatches.Remove(reverse.Data);
                                    }
                                    patcher.Unpatch();
                                    reversePatches = patchWaiter.ReversePatches.ToArray();
                                    i = -1;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        } catch (Exception e) {
            JALib.Instance.LogReportException("Failed to run patch waiter.", e);
        }
    }

    internal static void RunWaiterPatch() {
        if(!MainThread.IsQueueEmpty && MainThread.IsRunningOnMainThreadUpdate) {
            MainThread.ForceQueue(JALib.Instance, RunWaiterPatch);
            return;
        }
        RunWaiterPatchForce();
    }

    private static (PatchInfo, JAInternalPatchInfo) ClonePatch(PatchInfo patchInfo, JAInternalPatchInfo jaInternalPatchInfo) {
        return (new PatchInfo { 
               prefixes = patchInfo.prefixes.ToArray(),
               postfixes = patchInfo.postfixes.ToArray(),
               transpilers = patchInfo.transpilers.ToArray(),
               finalizers = patchInfo.finalizers.ToArray()
           }, new JAInternalPatchInfo {
               tryPrefixes = jaInternalPatchInfo.tryPrefixes.ToArray(),
               tryPostfixes = jaInternalPatchInfo.tryPostfixes.ToArray(),
               replaces = jaInternalPatchInfo.replaces.ToArray(),
               removes = jaInternalPatchInfo.removes.ToArray(),
               reversePatches = jaInternalPatchInfo.reversePatches.ToArray(),
               overridePatches = jaInternalPatchInfo.overridePatches.ToArray()
           });
    }

    private bool FoundErrorPatch(MethodBase method, ref PatchInfo patchInfo, JAInternalPatchInfo jaInternalPatchInfo, PatchInfo newPatch, JAInternalPatchInfo newJaInternalPatch,
        ref Exception e, out JAPatchBaseAttribute[] result) {
        List<JAPatchBaseAttribute> errorPatches = [];
        foreach(JAPatchBaseAttribute attribute in patchData) 
            if(attribute is not JAReversePatchAttribute && attribute.MethodBase == method) errorPatches.Add(attribute);
        for(int i = errorPatches.Count - 1; i >= 0; i--) {
            JAPatchBaseAttribute attribute = errorPatches[i];
            JAPatchAttribute patchAttribute = attribute as JAPatchAttribute;
            JAOverridePatchAttribute overridePatchAttribute = attribute as JAOverridePatchAttribute;
            if(patchAttribute != null) 
                UnpatchPatchAttribute(patchAttribute, newPatch, newJaInternalPatch);
            else if(overridePatchAttribute != null) 
                newJaInternalPatch.RemoveOverridePatch(overridePatchAttribute.PatchId);
            try {
                PatchUpdateWrapper(method, newPatch, newJaInternalPatch);
                if(PatchFailAction(attribute, e)) {
                    _doNotUnPatch = false;
                    UpdatePatchInfoOnlyPatchInfo(method, patchInfo);
                    mod.Error("Patch disabled is true, unpatching...");
                    Unpatch();
                    _doNotUnPatch = true;
                    patchInfo = GetPatchInfo(method) ?? new PatchInfo();
                    while(i-- > 0) {
                        patchAttribute = attribute as JAPatchAttribute;
                        if(patchAttribute != null) UnpatchPatchAttribute(patchAttribute, newPatch, newJaInternalPatch);
                        else if((overridePatchAttribute = attribute as JAOverridePatchAttribute) != null) newJaInternalPatch.RemoveOverridePatch(overridePatchAttribute.PatchId);
                    }
                    result = [];
                    return true;
                }
                if(patchAttribute != null) UnpatchPatchAttribute(patchAttribute, patchInfo, jaInternalPatchInfo);
                else if(overridePatchAttribute != null) jaInternalPatchInfo.RemoveOverridePatch(overridePatchAttribute.PatchId);
                result = new JAPatchBaseAttribute[errorPatches.Count - i - 1];
                for(int j = i + 1; j < errorPatches.Count; j++) result[j] = errorPatches[j];
                return true;
            } catch (Exception ex) {
                e = new JAPatchException(method, newPatch, newJaInternalPatch, ex);
            }
        }
        result = errorPatches.ToArray();
        return false;
    }

    private MethodInfo RevertErrorPatch(MethodBase method, ref PatchInfo patchInfo, JAInternalPatchInfo jaInternalPatchInfo, PatchInfo newPatch, JAInternalPatchInfo newJaInternalPatch, JAPatchBaseAttribute[] errorPatches) {
        for(int i = 0; i < errorPatches.Length; i++) {
            JAOverridePatchAttribute overridePatchAttribute;
            if(errorPatches[i] is JAPatchAttribute patchAttribute) 
                AddPatchInfo(patchAttribute, newPatch, newJaInternalPatch, mod, out bool _);
            else if((overridePatchAttribute = errorPatches[i] as JAOverridePatchAttribute) != null)
                AddOverridePatch(method.AsUnsafe<MethodInfo>(), overridePatchAttribute, newJaInternalPatch, mod);
            try {
                PatchUpdateWrapper(method, newPatch, newJaInternalPatch);
            } catch (Exception e) {
                if(PatchFailAction(errorPatches[i], e)) {
                    _doNotUnPatch = false;
                    UpdatePatchInfoOnlyPatchInfo(method, patchInfo);
                    mod.Error("Patch disabled is true, unpatching...");
                    Unpatch();
                    _doNotUnPatch = true;
                    patchInfo = GetPatchInfo(method) ?? new PatchInfo();
                    while(i-- > 0) {
                        patchAttribute = errorPatches[i] as JAPatchAttribute;
                        if(patchAttribute != null) UnpatchPatchAttribute(patchAttribute, newPatch, newJaInternalPatch);
                        else if((overridePatchAttribute = errorPatches[i] as JAOverridePatchAttribute) != null) newJaInternalPatch.RemoveOverridePatch(overridePatchAttribute.PatchId);
                    }
                }
                try {
                    return PatchUpdateWrapper(method, patchInfo, jaInternalPatchInfo);
                } catch (Exception) {
                    return null;
                }
            }
        }
        return null;
    }

    private static void UnpatchPatchAttribute(JAPatchAttribute patchAttribute, PatchInfo patchInfo, JAInternalPatchInfo jaInternalPatchInfo) {
        string id = patchAttribute.PatchId;
        switch(patchAttribute.PatchType) {
            case PatchType.Prefix:
                if(CheckRemove(patchAttribute.Method)) jaInternalPatchInfo.RemoveRemove(id);
                else if(patchAttribute.TryingCatch) jaInternalPatchInfo.RemoveTryPrefix(id);
                else patchInfo.RemovePrefix(id);
                break;
            case PatchType.Postfix:
                if(patchAttribute.TryingCatch) jaInternalPatchInfo.RemoveTryPostfix(id);
                else patchInfo.RemovePostfix(id);
                break;
            case PatchType.Transpiler:
                patchInfo.RemoveTranspiler(id);
                break;
            case PatchType.Finalizer:
                patchInfo.RemoveFinalizer(id);
                break;
            case PatchType.Replace:
                jaInternalPatchInfo.RemoveReplace(id);
                break;
        }
    }

    private static bool CheckRemove(MethodInfo method) {
        if(method.ReturnType != typeof(bool)) return false;
        List<CodeInstruction> code = PatchProcessor.GetCurrentInstructions(method);
        IEnumerator<CodeInstruction> enumerator = code.Where(c => c.opcode != OpCodes.Nop).GetEnumerator();
        return enumerator.MoveNext() && enumerator.Current.opcode == OpCodes.Ldc_I4_0 &&
               enumerator.MoveNext() && enumerator.Current.opcode == OpCodes.Ret;
    }

    public void Unpatch() {
        if(!patched || _doNotUnPatch) return;
        patched = false;
        foreach(JAPatchBaseAttribute baseAttribute in patchData) {
            if(baseAttribute is JAPatchAttribute patchAttribute) {
                MethodInfo patch = patchAttribute.Method;
                string id = patchAttribute.PatchId;
                lock(HarmonyLocker) {
                    PatchInfo patchInfo = GetPatchInfo(patchAttribute.MethodBase) ?? new PatchInfo();
                    JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(patchAttribute.MethodBase) ?? new JAInternalPatchInfo();
                    switch(patchAttribute.PatchType) {
                        case PatchType.Prefix:
                            if(CheckRemove(patch)) RemovePatch(patch, id, ref jaInternalPatchInfo.removes);
                            else if(patchAttribute.TryingCatch) RemovePatch(patch, id, ref jaInternalPatchInfo.tryPrefixes);
                            else RemovePatch(patch, id, ref patchInfo.prefixes);
                            break;
                        case PatchType.Postfix:
                            if(patchAttribute.TryingCatch) RemovePatch(patch, id, ref jaInternalPatchInfo.tryPostfixes);
                            else RemovePatch(patch, id, ref patchInfo.postfixes);
                            break;
                        case PatchType.Transpiler:
                            RemovePatch(patch, id, ref patchInfo.transpilers);
                            break;
                        case PatchType.Finalizer:
                            RemovePatch(patch, id, ref patchInfo.finalizers);
                            break;
                        case PatchType.Replace:
                            RemovePatch(patch, id, ref jaInternalPatchInfo.replaces);
                            break;
                    }
                    MethodInfo replacement = PatchUpdateWrapper(patchAttribute.MethodBase, patchInfo, jaInternalPatchInfo);
                    typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState").Invoke("UpdatePatchInfo", patchAttribute.MethodBase, replacement, patchInfo);
                    JaPatches[patchAttribute.MethodBase] = jaInternalPatchInfo;
                }
            } else if(baseAttribute is JAReversePatchAttribute reversePatchAttribute) {
                if(reversePatchAttribute.PatchType == ReversePatchType.Original || reversePatchAttribute.PatchType.HasFlag(ReversePatchType.DontUpdate)) continue;
                JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(reversePatchAttribute.Data.Original);
                if(jaInternalPatchInfo == null) continue;
                jaInternalPatchInfo.reversePatches = jaInternalPatchInfo.reversePatches.Where(patch => patch != reversePatchAttribute.Data).ToArray();
            } else if(baseAttribute is JAOverridePatchAttribute overridePatchAttribute) {
                JAInternalPatchInfo jaInternalPatchInfo = JaPatches.GetValueOrDefault(overridePatchAttribute.MethodBase);
                if(jaInternalPatchInfo == null) continue;
                jaInternalPatchInfo.overridePatches = jaInternalPatchInfo.overridePatches.Where(patch => patch.PatchMethod != overridePatchAttribute.Method).ToArray();
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
            foreach(JAPatchBaseAttribute attribute in method.GetCustomAttributes<JAPatchBaseAttribute>()) {
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
                        }
                        break;
                }
                attribute.Method = method;
                AddPatch(attribute);
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