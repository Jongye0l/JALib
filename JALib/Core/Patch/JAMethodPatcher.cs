using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.JAException;
using JALib.Tools;
using MonoMod.Utils;

namespace JALib.Core.Patch;

class JAMethodPatcher {
    private static Dictionary<int, int> _parameterMap = new();
    private static Dictionary<int, FieldInfo> _parameterFields = new();
    private readonly MethodBase original;
    private readonly MethodBase source;
    private readonly bool debug;
    private HarmonyLib.Patch[] prefixes;
    private HarmonyLib.Patch[] postfixes;
    private HarmonyLib.Patch[] transpilers;
    private HarmonyLib.Patch[] finalizers;
    private HarmonyLib.Patch[] removes;
    private HarmonyLib.Patch replace;
    private TriedPatchData[] tryPrefixes;
    private TriedPatchData[] tryPostfixes;
    private readonly object originalPatcher;
    private readonly ILGenerator il;
    private readonly int idx;
    private readonly JAEmitter emitter;
    private readonly Type returnType;
    private readonly bool useStructReturnBuffer;
    private readonly bool customReverse;

    public JAMethodPatcher(MethodBase original, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        this.original = original;
        debug = patchInfo.Debugging || Harmony.DEBUG;
        SortPatchMethods(original, patchInfo.prefixes.Concat(jaPatchInfo.tryPrefixes).Concat(jaPatchInfo.removes).ToArray(), debug, out prefixes);
        removes = jaPatchInfo.removes;
        SetupPrefixRemove();
        List<MethodInfo> prefix = prefixes.Select(patch => patch.PatchMethod).ToList();
        tryPrefixes = jaPatchInfo.tryPrefixes;
        List<MethodInfo> postfix, transpiler, finalizer;
        if(removes.Length > 0) {
            postfix = transpiler = finalizer = [];
            postfixes = transpilers = finalizers = tryPostfixes = [];
        } else {
            postfix = SortPatchMethods(original, patchInfo.postfixes.Concat(jaPatchInfo.tryPostfixes).ToArray(), debug, out postfixes);
            transpiler = SortPatchMethods(original, patchInfo.transpilers, debug, out transpilers);
            finalizer = SortPatchMethods(original, patchInfo.finalizers, debug, out finalizers);
            SortPatchMethods(original, jaPatchInfo.replaces, debug, out HarmonyLib.Patch[] replaces);
            replace = replaces.Last();
            tryPostfixes = jaPatchInfo.tryPostfixes;
            MethodInfo method = ((Delegate) ChangeParameter).Method;
            transpilers = new[] { CreateEmptyPatch(method) }.Concat(transpilers).ToArray();
            transpiler.Insert(0, method);
        }
        originalPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").New(original, null, prefix, postfix, transpiler, finalizer, debug);
        il = originalPatcher.GetValue<ILGenerator>("il");
        idx = prefixes.Length + postfixes.Length + finalizers.Length;
        emitter = new JAEmitter(originalPatcher.GetValue("emitter"));
        useStructReturnBuffer = originalPatcher.GetValue<bool>("useStructReturnBuffer");
        returnType = original is MethodInfo info ? info.ReturnType : typeof(void);
    }

    public JAMethodPatcher(MethodBase original, MethodBase source, Patches patchInfo, JAPatchInfo jaPatchInfo, MethodInfo postTranspiler, bool debug) {
        this.original = original;
        this.source = source;
        this.debug = debug;
        prefixes = postfixes = finalizers = removes = tryPrefixes = tryPostfixes = [];
        List<MethodInfo> none = [];
        List<MethodInfo> transpiler = SortPatchMethods(original, patchInfo.Transpilers.ToArray(), debug, out transpilers);
        if(postTranspiler != null) {
            transpiler.Add(postTranspiler);
            transpilers = transpilers.Concat([CreateEmptyPatch(postTranspiler)]).ToArray();
        }
        SortPatchMethods(original, jaPatchInfo.replaces, debug, out HarmonyLib.Patch[] replaces);
        replace = replaces.Last();
        if(replace != null) {
            MethodInfo method = ((Delegate) ChangeParameter).Method;
            transpilers = new[] { CreateEmptyPatch(method) }.Concat(transpilers).ToArray();
            transpiler.Insert(0, method);
        }
        originalPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").New(original, source, none, none, transpiler, none, debug);
        il = originalPatcher.GetValue<ILGenerator>("il");
        idx = prefixes.Length + postfixes.Length + finalizers.Length;
        emitter = new JAEmitter(originalPatcher.GetValue("emitter"));
        useStructReturnBuffer = originalPatcher.GetValue<bool>("useStructReturnBuffer");
        returnType = original is MethodInfo info ? info.ReturnType : typeof(void);
    }

    public JAMethodPatcher(MethodBase original, MethodBase source, PatchInfo patchInfo,
        JAPatchInfo jaPatchInfo, bool debug, JAReversePatchAttribute attribute, JAMod mod) {
        this.original = original;
        this.source = source;
        this.debug = debug;
        string customPatchMethodName = "<" + original.Name + ">";
        MethodInfo[] customPatchMethods = mod.GetType().GetMethods().Where(m => m.Name.Contains(customPatchMethodName)).ToArray();
        Func<MethodInfo, HarmonyLib.Patch> changeFunc = attribute.TryCatchChildren ? method => CreateEmptyTryPatch(method, mod) : CreateEmptyPatch;
        HarmonyLib.Patch[] children = customPatchMethods.Where(method => method.Name.Contains("Prefix")).Select(changeFunc).ToArray();
        if(attribute.PatchType.HasFlag(ReversePatchType.PrefixCombine)) {
            SortPatchMethods(original, patchInfo.prefixes.Concat(jaPatchInfo.tryPrefixes).Concat(jaPatchInfo.removes).ToArray(), debug, out prefixes);
            removes = jaPatchInfo.removes;
            SetupPrefixRemove();
            tryPrefixes = jaPatchInfo.tryPrefixes;
            if(attribute.TryCatchChildren) tryPrefixes = tryPrefixes.Concat(children.Select(patch => (TriedPatchData) patch)).ToArray();
            prefixes = prefixes.Concat(children).ToArray();
        } else {
            prefixes = children;
            removes = [];
            tryPrefixes = attribute.TryCatchChildren ? children.Select(patch => (TriedPatchData) patch).ToArray() : [];
        }
        if(removes.Length > 0) postfixes = transpilers = finalizers = tryPostfixes = [];
        else {
            children = customPatchMethods.Where(method => method.Name.Contains("Postfix")).Select(changeFunc).ToArray();
            if(attribute.PatchType.HasFlag(ReversePatchType.PostfixCombine)) {
                SortPatchMethods(original, patchInfo.postfixes.Concat(jaPatchInfo.tryPostfixes).ToArray(), debug, out postfixes);
                tryPostfixes = jaPatchInfo.tryPostfixes;
                if(attribute.TryCatchChildren) tryPostfixes = tryPostfixes.Concat(children.Select(patch => (TriedPatchData) patch)).ToArray();
                postfixes = postfixes.Concat(children).ToArray();
            } else {
                postfixes = children;
                tryPostfixes = attribute.TryCatchChildren ? children.Select(patch => (TriedPatchData) patch).ToArray() : [];
            }
            children = customPatchMethods.Where(method => method.Name.Contains("Transpiler")).Select(CreateEmptyPatch).ToArray();
            if(attribute.PatchType.HasFlag(ReversePatchType.TranspilerCombine)) {
                SortPatchMethods(original, patchInfo.transpilers.ToArray(), debug, out transpilers);
                transpilers = transpilers.Concat(children).ToArray();
            } else transpilers = children;
            children = customPatchMethods.Where(method => method.Name.Contains("Finalizer")).Select(CreateEmptyPatch).ToArray();
            if(attribute.PatchType.HasFlag(ReversePatchType.FinalizerCombine)) {
                SortPatchMethods(original, patchInfo.finalizers.ToArray(), debug, out finalizers);
                finalizers = finalizers.Concat(children).ToArray();
            } else finalizers = children;
            if(attribute.PatchType.HasFlag(ReversePatchType.ReplaceCombine)) {
                SortPatchMethods(original, jaPatchInfo.replaces, debug, out HarmonyLib.Patch[] replaces);
                replace = replaces.Last();
                if(replace != null) {
                    MethodInfo method = ((Delegate) ChangeParameter).Method;
                    transpilers = new[] { CreateEmptyPatch(method) }.Concat(transpilers).ToArray();
                }
            }
        }
        originalPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").New(original, source,
            prefixes.Select(patch => patch.PatchMethod).ToList(),
            postfixes.Select(patch => patch.PatchMethod).ToList(),
            transpilers.Select(patch => patch.PatchMethod).ToList(),
            finalizers.Select(patch => patch.PatchMethod).ToList(), debug);
        il = originalPatcher.GetValue<ILGenerator>("il");
        idx = prefixes.Length + postfixes.Length + finalizers.Length;
        emitter = new JAEmitter(originalPatcher.GetValue("emitter"));
        useStructReturnBuffer = originalPatcher.GetValue<bool>("useStructReturnBuffer");
        returnType = original is MethodInfo info ? info.ReturnType : typeof(void);
        customReverse = true;
    }

    private HarmonyLib.Patch CreateEmptyPatch(MethodInfo method) => new(method, 0, "", 0, [], [], debug);

    private TriedPatchData CreateEmptyTryPatch(MethodInfo method, JAMod mod) => new(method, 0, "", 0, [], [], debug, mod);

    private void SetupPrefixRemove() {
        bool a = false;
        prefixes = prefixes.Where(pre => {
            if(a) return false;
            if(!removes.Contains(pre)) return true;
            a = true;
            return false;
        }).ToArray();
    }

    private static List<MethodInfo> SortPatchMethods(MethodBase original, HarmonyLib.Patch[] patches, bool debug, out HarmonyLib.Patch[] sortedPatches) {
        object patchSorter = typeof(Harmony).Assembly.GetType("HarmonyLib.PatchSorter").New(patches, debug);
        List<MethodInfo> sortedMethods = patchSorter.Invoke<List<MethodInfo>>("Sort", original);
        sortedPatches = patchSorter.GetValue<HarmonyLib.Patch[]>("sortedPatchArray");
        return sortedMethods;
    }

    internal MethodInfo CreateReplacement(out Dictionary<int, CodeInstruction> finalInstructions) {
        Type methodPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher");
        LocalBuilder[] originalVariables = removes.Length > 0 ? [] : methodPatcher.Invoke<LocalBuilder[]>("DeclareLocalVariables", il, replace?.PatchMethod ?? source ?? original);
        Dictionary<string, LocalBuilder> privateVars = new();
        HarmonyLib.Patch[] fixes = removes.Length == 0 ? prefixes.Union(postfixes).Union(finalizers).ToArray() : prefixes;
        LocalBuilder resultVariable = null;
        if(idx > 0) {
            resultVariable = DeclareLocalVariable(returnType, true);
            privateVars["__result"] = resultVariable;
        }
        if(fixes.Any(fix => fix.PatchMethod.GetParameters().Any(p => p.Name == "__args"))) {
            originalPatcher.Invoke("PrepareArgumentArray");
            LocalBuilder local2 = il.DeclareLocal(typeof(object[]));
            emitter.Emit(OpCodes.Stloc, local2);
            privateVars["__args"] = local2;
        }
        Label? skipOriginalLabel = null;
        LocalBuilder runOriginalVariable = null;
        bool prefixAffectsOriginal = prefixes.Any(fix => methodPatcher.Invoke<bool>("PrefixAffectsOriginal", [fix.PatchMethod]));
        bool anyFixHasRunOriginalVar = fixes.Any(fix => fix.PatchMethod.GetParameters().Any(p => p.Name == "__runOriginal"));
        if(prefixAffectsOriginal || anyFixHasRunOriginalVar) {
            runOriginalVariable = DeclareLocalVariable(typeof(bool));
            emitter.Emit(OpCodes.Ldc_I4_1);
            emitter.Emit(OpCodes.Stloc, runOriginalVariable);
            if(prefixAffectsOriginal) skipOriginalLabel = il.DefineLabel();
        }
        foreach(HarmonyLib.Patch fix in fixes) {
            MethodInfo method = fix.PatchMethod;
            if(method.DeclaringType == null || privateVars.ContainsKey(method.DeclaringType.AssemblyQualifiedName)) continue;
            method.GetParameters().Where(patchParam => patchParam.Name == "__state")
                .Do(patchParam => privateVars[method.DeclaringType.AssemblyQualifiedName] =
                                      DeclareLocalVariable(patchParam.ParameterType));
        }
        LocalBuilder finalizedVariable = null;
        if(finalizers.Length > 0) {
            finalizedVariable = DeclareLocalVariable(typeof(bool));
            privateVars["__exception"] = DeclareLocalVariable(typeof(Exception));
            emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        }
        AddPrefixes(privateVars, runOriginalVariable, methodPatcher);
        if(skipOriginalLabel.HasValue) {
            emitter.Emit(OpCodes.Ldloc, runOriginalVariable);
            emitter.Emit(OpCodes.Brfalse, skipOriginalLabel.Value);
        }
        bool needsToStorePassthroughResult = false;
        bool hasReturnCode = false;
        if(removes.Length == 0) {
            JAMethodCopier copier = new(replace?.PatchMethod ?? source ?? original, il, originalVariables);
            copier.SetArgumentShift(useStructReturnBuffer);
            copier.SetDebugging(debug);
            copier.AddTranspiler(transpilers);
            if(replace != null || customReverse) {
                ParameterInfo[] replaceParameter = replace != null ? replace.PatchMethod.GetParameters() : source.GetParameters();
                ParameterInfo[] originalParameter = original.GetParameters();
                lock(_parameterMap) {
                    _parameterMap.Clear();
                    _parameterFields.Clear();
                    foreach(ParameterInfo parameterInfo in replaceParameter) {
                        if(parameterInfo.Name.ToLower() == "__instance" && !original.IsStatic) {
                            _parameterMap[parameterInfo.Position] = 0;
                            continue;
                        }
                        if(parameterInfo.Name.StartsWith("___")) {
                            FieldInfo field = original.DeclaringType.Field(parameterInfo.Name[3..]);
                            if(field != null) {
                                _parameterFields[parameterInfo.Position] = field;
                                continue;
                            }
                        }
                        ParameterInfo parameter = originalParameter.FirstOrDefault(info => info.Name == parameterInfo.Name);
                        if(parameter != null) {
                            if(parameter.ParameterType != parameterInfo.ParameterType) throw new PatchParameterException("Parameter type mismatch: " + parameterInfo.Name);
                            _parameterMap[parameterInfo.Position] = parameter.Position;
                            continue;
                        }
                        if(!customReverse) throw new PatchParameterException("Unknown Parameter: " + parameterInfo.Name);
                    }
                }

            }
            List<Label> endLabels = [];
            copier.Finalize(emitter, endLabels, out hasReturnCode);
            foreach(Label label in endLabels) emitter.MarkLabel(label);
            if(resultVariable != null & hasReturnCode) emitter.Emit(OpCodes.Stloc, resultVariable);
            if(skipOriginalLabel.HasValue) emitter.MarkLabel(skipOriginalLabel.Value);
            AddPostfixes(privateVars, runOriginalVariable, false);
            if(resultVariable != null & hasReturnCode)
                emitter.Emit(OpCodes.Ldloc, resultVariable);
            needsToStorePassthroughResult = AddPostfixes(privateVars, runOriginalVariable, true);
        }
        bool hasFinalizers = finalizers.Length > 0;
        if(hasFinalizers) {
            if(needsToStorePassthroughResult) {
                emitter.Emit(OpCodes.Stloc, resultVariable);
                emitter.Emit(OpCodes.Ldloc, resultVariable);
            }
            originalPatcher.Invoke("AddFinalizers", privateVars, runOriginalVariable, false);
            emitter.Emit(OpCodes.Ldc_I4_1);
            emitter.Emit(OpCodes.Stloc, finalizedVariable);
            Label label1 = il.DefineLabel();
            emitter.Emit(OpCodes.Ldloc, privateVars["__exception"]);
            emitter.Emit(OpCodes.Brfalse, label1);
            emitter.Emit(OpCodes.Ldloc, privateVars["__exception"]);
            emitter.Emit(OpCodes.Throw);
            emitter.MarkLabel(label1);
            emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock));
            emitter.Emit(OpCodes.Stloc, privateVars["__exception"]);
            emitter.Emit(OpCodes.Ldloc, finalizedVariable);
            Label label2 = il.DefineLabel();
            emitter.Emit(OpCodes.Brtrue, label2);
            bool rethrowPossible = originalPatcher.Invoke<bool>("AddFinalizers", privateVars, runOriginalVariable, true);
            emitter.MarkLabel(label2);
            Label label3 = il.DefineLabel();
            emitter.Emit(OpCodes.Ldloc, privateVars["__exception"]);
            emitter.Emit(OpCodes.Brfalse, label3);
            if(rethrowPossible) emitter.Emit(OpCodes.Rethrow);
            else {
                emitter.Emit(OpCodes.Ldloc, privateVars["__exception"]);
                emitter.Emit(OpCodes.Throw);
            }
            emitter.MarkLabel(label3);
            emitter.MarkBlockAfter(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            if(resultVariable != null) emitter.Emit(OpCodes.Ldloc, resultVariable);
        }
        if(useStructReturnBuffer) {
            LocalBuilder local4 = DeclareLocalVariable(returnType);
            emitter.Emit(OpCodes.Stloc, local4);
            emitter.Emit(original.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
            emitter.Emit(OpCodes.Ldloc, local4);
            emitter.Emit(OpCodes.Stobj, returnType);
        }
        if(hasFinalizers || hasReturnCode)
            emitter.Emit(OpCodes.Ret);
        finalInstructions = emitter.GetInstructions();
        if(debug) {
            FileLog.LogBuffered("DONE");
            FileLog.LogBuffered("");
            FileLog.FlushBuffer();
        }
        MethodInfo replacement = originalPatcher.GetValue<DynamicMethodDefinition>("patch").Generate();
        typeof(Harmony).Assembly.GetType("MonoMod.RuntimeDetour.DetourHelper").Method("Pin").MakeGenericMethod(typeof(MethodInfo)).Invoke([replacement]);
        return replacement;
    }

    private LocalBuilder DeclareLocalVariable(Type type, bool isReturnValue = false) {
        if(type.IsByRef && !isReturnValue) type = type.GetElementType();
        if(type.IsEnum)
            type = Enum.GetUnderlyingType(type);
        if(AccessTools.IsClass(type)) {
            LocalBuilder local = il.DeclareLocal(type);
            emitter.Emit(OpCodes.Ldnull);
            emitter.Emit(OpCodes.Stloc, local);
            return local;
        }
        if(AccessTools.IsStruct(type)) {
            LocalBuilder local = il.DeclareLocal(type);
            emitter.Emit(OpCodes.Ldloca, local);
            emitter.Emit(OpCodes.Initobj, type);
            return local;
        }
        if(!AccessTools.IsValue(type)) return null;
        LocalBuilder v = il.DeclareLocal(type);
        if(type == typeof(float)) emitter.Emit(OpCodes.Ldc_R4, 0.0f);
        else if(type == typeof(double)) emitter.Emit(OpCodes.Ldc_R8, 0.0);
        else if(type == typeof(long) || type == typeof(ulong)) emitter.Emit(OpCodes.Ldc_I8, 0L);
        else emitter.Emit(OpCodes.Ldc_I4, 0);
        emitter.Emit(OpCodes.Stloc, v);
        return v;
    }

    private void AddPrefixes(Dictionary<string, LocalBuilder> variables, LocalBuilder runOriginalVariable, Type methodPatcher) {
        foreach(HarmonyLib.Patch patch in prefixes) {
            MethodInfo fix = patch.PatchMethod;
            Label? skipLabel = methodPatcher.Invoke<bool>("PrefixAffectsOriginal", [fix]) ? il.DefineLabel() : new Label?();
            if(skipLabel.HasValue) {
                emitter.Emit(OpCodes.Ldloc, runOriginalVariable);
                emitter.Emit(OpCodes.Brfalse, skipLabel.Value);
            }
            LocalBuilder exceptionVar = tryPrefixes.Contains(patch) ? il.DeclareLocal(typeof(Exception)) : null;
            if(exceptionVar != null) emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            List<KeyValuePair<LocalBuilder, Type>> keyValuePairList = [];
            object[] args = [fix, variables, runOriginalVariable, false, null, keyValuePairList];
            originalPatcher.Invoke("EmitCallParameter", args);
            LocalBuilder tmpObjectVar = (LocalBuilder) args[4];
            emitter.Emit(OpCodes.Call, fix);
            if(fix.GetParameters().Any(p => p.Name == "__args"))
                originalPatcher.Invoke("RestoreArgumentArray", variables);
            if(tmpObjectVar != null) {
                emitter.Emit(OpCodes.Ldloc, tmpObjectVar);
                emitter.Emit(OpCodes.Unbox_Any, AccessTools.GetReturnedType(original));
                emitter.Emit(OpCodes.Stloc, variables["__result"]);
            }
            keyValuePairList.Do(tmpBoxVar => {
                emitter.Emit(this.original.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
                emitter.Emit(OpCodes.Ldloc, tmpBoxVar.Key);
                emitter.Emit(OpCodes.Unbox_Any, tmpBoxVar.Value);
                emitter.Emit(OpCodes.Stobj, tmpBoxVar.Value);
            });
            Type returnType = fix.ReturnType;
            if(returnType != typeof(void)) {
                if(returnType != typeof(bool)) throw new Exception($"Prefix patch {fix} has not \"bool\" or \"void\" return type: {returnType}");
                emitter.Emit(OpCodes.Stloc, runOriginalVariable);
            }
            if(exceptionVar != null) {
                emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock));
                emitter.Emit(OpCodes.Stloc, exceptionVar);
                emitter.Emit(OpCodes.Ldstr, ((TriedPatchData) patch).mod.Name);
                emitter.Emit(OpCodes.Call, typeof(JAMod).Method("GetMods", typeof(string)));
                emitter.Emit(OpCodes.Ldstr, "An error occurred while invoking a prefix patch " + patch.owner);
                emitter.Emit(OpCodes.Ldloc, exceptionVar);
                emitter.Emit(OpCodes.Call, typeof(JAMod).Method("LogException", typeof(string), typeof(Exception)));
                if(returnType == typeof(bool)) {
                    emitter.Emit(OpCodes.Ldc_I4_1);
                    emitter.Emit(OpCodes.Stloc, runOriginalVariable);
                }
                emitter.MarkBlockAfter(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            }
            if(!skipLabel.HasValue) continue;
            emitter.MarkLabel(skipLabel.Value);
            emitter.Emit(OpCodes.Nop);
        }
    }

    private bool AddPostfixes(Dictionary<string, LocalBuilder> variables, LocalBuilder runOriginalVariable, bool passthroughPatches) {
        bool result = false;
        foreach(HarmonyLib.Patch patch in postfixes) {
            MethodInfo fix = patch.PatchMethod;
            if(passthroughPatches != (fix.ReturnType != typeof(void))) continue;
            LocalBuilder exceptionVar = tryPostfixes.Contains(patch) ? il.DeclareLocal(typeof(Exception)) : null;
            if(exceptionVar != null) emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            List<KeyValuePair<LocalBuilder, Type>> tmpBoxVars = [];
            object[] args = [fix, variables, runOriginalVariable, false, null, tmpBoxVars];
            originalPatcher.Invoke("EmitCallParameter", args);
            LocalBuilder tmpObjectVar = (LocalBuilder) args[4];
            emitter.Emit(OpCodes.Call, fix);
            if(fix.GetParameters().Any(p => p.Name == "__args"))
                originalPatcher.Invoke("RestoreArgumentArray", variables);
            if(tmpObjectVar != null) {
                emitter.Emit(OpCodes.Ldloc, tmpObjectVar);
                emitter.Emit(OpCodes.Unbox_Any, AccessTools.GetReturnedType(original));
                emitter.Emit(OpCodes.Stloc, variables["__result"]);
            }
            tmpBoxVars.Do(tmpBoxVar => {
                emitter.Emit(original.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
                emitter.Emit(OpCodes.Ldloc, tmpBoxVar.Key);
                emitter.Emit(OpCodes.Unbox_Any, tmpBoxVar.Value);
                emitter.Emit(OpCodes.Stobj, tmpBoxVar.Value);
            });
            if(exceptionVar != null) {
                emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock));
                emitter.Emit(OpCodes.Stloc, exceptionVar);
                emitter.Emit(OpCodes.Ldstr, ((TriedPatchData) patch).mod.Name);
                emitter.Emit(OpCodes.Call, typeof(JAMod).Method("GetMods", typeof(string)));
                emitter.Emit(OpCodes.Ldstr, "An error occurred while invoking a postfix patch " + patch.owner);
                emitter.Emit(OpCodes.Ldloc, exceptionVar);
                emitter.Emit(OpCodes.Call, typeof(JAMod).Method("LogException", typeof(string), typeof(Exception)));
                emitter.MarkBlockAfter(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            }
            if(fix.ReturnType == typeof(void)) continue;
            ParameterInfo firstFixParam = fix.GetParameters().FirstOrDefault();
            if(firstFixParam != null && fix.ReturnType == firstFixParam.ParameterType) result = true;
            else {
                if(firstFixParam != null) throw new Exception($"Return type of pass through postfix {(object) fix} does not match type of its first parameter");
                throw new Exception($"Postfix patch {(object) fix} must have a \"void\" return type");
            }
        }
        return result;
    }

    private static IEnumerable<CodeInstruction> ChangeParameter(IEnumerable<CodeInstruction> instructions) {
        lock(_parameterMap) {
            foreach(CodeInstruction instruction in instructions) {
                int index = GetParameterIndex(instruction, out bool set);
                if(index > -1) {
                    if(_parameterMap.TryGetValue(index, out int newIndex)) {
                        yield return GetParameterInstruction(newIndex, set);
                    } else if(_parameterFields.TryGetValue(index, out FieldInfo info)) {
                        yield return new CodeInstruction(set ? OpCodes.Stfld : OpCodes.Ldfld, info);
                    } else yield return new CodeInstruction(set ? OpCodes.Starg : OpCodes.Ldarg, index * -1 - 2);
                } else {
                    yield return instruction;
                }
            }
        }
    }

    private static int GetParameterIndex(CodeInstruction instruction, out bool set) {
        int index = -1;
        set = false;
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
        } else if(instruction.opcode == OpCodes.Ldarg_0) index = 0;
        else if(instruction.opcode == OpCodes.Ldarg_1) index = 1;
        else if(instruction.opcode == OpCodes.Ldarg_2) index = 2;
        else if(instruction.opcode == OpCodes.Ldarg_3) index = 3;
        return index;
    }

    private static CodeInstruction GetParameterInstruction(int index, bool set) {
        if(set) return index < 256 ? new CodeInstruction(OpCodes.Starg_S, (byte) index) : new CodeInstruction(OpCodes.Starg, index);
        return index switch {
            0 => new Code.Ldarg_0_(),
            1 => new Code.Ldarg_1_(),
            2 => new Code.Ldarg_2_(),
            3 => new Code.Ldarg_3_(),
            _ => index < 256 ? new CodeInstruction(OpCodes.Ldarg_S, (byte) index) : new CodeInstruction(OpCodes.Ldarg, index)
        };
    }
}