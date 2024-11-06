using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using JALib.JAException;
using JALib.Tools;

namespace JALib.Core.Patch;

#pragma warning disable CS0414
class JAMethodPatcher {
    private static Dictionary<int, int> _parameterMap = new();
    private static Dictionary<int, FieldInfo> _parameterFields = new();
    private readonly bool debug;
    private HarmonyLib.Patch[] prefixes;
    private HarmonyLib.Patch[] postfixes;
    private HarmonyLib.Patch[] transpilers;
    private HarmonyLib.Patch[] finalizers;
    private HarmonyLib.Patch[] removes;
    private MethodBase replace;
    private TriedPatchData[] tryPrefixes;
    private TriedPatchData[] tryPostfixes;
    private OverridePatchData[] overridePatches;
    private readonly object originalPatcher;
    private readonly bool customReverse;

    public JAMethodPatcher(MethodBase original, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        debug = patchInfo.Debugging || Harmony.DEBUG || jaPatchInfo.IsDebug();
        SortPatchMethods(original, patchInfo.prefixes.Concat(jaPatchInfo.tryPrefixes).Concat(jaPatchInfo.removes).ToArray(), debug, out prefixes);
        removes = jaPatchInfo.removes;
        SetupPrefixRemove();
        List<MethodInfo> prefix = prefixes.Select(patch => patch.PatchMethod).ToList();
        tryPrefixes = jaPatchInfo.tryPrefixes;
        List<MethodInfo> postfix = SortPatchMethods(original, patchInfo.postfixes.Concat(jaPatchInfo.tryPostfixes).ToArray(), debug, out postfixes);
        List<MethodInfo> finalizer = SortPatchMethods(original, patchInfo.finalizers, debug, out finalizers);
        List<MethodInfo> transpiler;
        tryPostfixes = jaPatchInfo.tryPostfixes;
        overridePatches = jaPatchInfo.overridePatches;
        if(removes.Length > 0) {
            transpiler = [];
            transpilers = [];
            overridePatches = overridePatches.Where(patch => patch.IgnoreBasePatch).ToArray();
        } else {
            transpiler = SortPatchMethods(original, patchInfo.transpilers, debug, out transpilers);
            SetupReplace(original, jaPatchInfo, transpiler);
        }
        originalPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").New(original, null, prefix, postfix, transpiler, finalizer, debug);
    }

    public JAMethodPatcher(HarmonyMethod standin, MethodBase source, JAPatchInfo jaPatchInfo, MethodInfo postTranspiler) {
        MethodBase original = standin.method;
        Patches patchInfo = Harmony.GetPatchInfo(source);
        debug = standin.debug.GetValueOrDefault() || Harmony.DEBUG;
        prefixes = postfixes = finalizers = removes = tryPrefixes = tryPostfixes = [];
        overridePatches = [];
        List<MethodInfo> none = [];
        List<MethodInfo> transpiler = SortPatchMethods(original, patchInfo.Transpilers.ToArray(), debug, out transpilers);
        if(postTranspiler != null) {
            transpiler.Add(postTranspiler);
            transpilers = transpilers.Concat([CreateEmptyPatch(postTranspiler)]).ToArray();
        }
        SetupReplace(original, jaPatchInfo, transpiler);
        originalPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").New(original, source, none, none, transpiler, none, debug);
    }

    public JAMethodPatcher(ReversePatchData data, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        customReverse = true;
        MethodBase original = data.patchMethod;
        debug = data.debug || Harmony.DEBUG;
        JAReversePatchAttribute attribute = data.attribute;
        JAMod mod = data.mod;
        string customPatchMethodName = "<" + original.Name + ">";
        MethodInfo[] customPatchMethods = original.DeclaringType.Methods().Where(m => m.Name.Contains(customPatchMethodName)).ToArray();
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
        children = customPatchMethods.Where(method => method.Name.Contains("Finalizer")).Select(CreateEmptyPatch).ToArray();
        if(attribute.PatchType.HasFlag(ReversePatchType.FinalizerCombine)) {
            SortPatchMethods(original, patchInfo.finalizers.ToArray(), debug, out finalizers);
            finalizers = finalizers.Concat(children).ToArray();
        } else finalizers = children;
        overridePatches = attribute.PatchType.HasFlag(ReversePatchType.OverrideCombine) ? jaPatchInfo.overridePatches : [];
        if(removes.Length > 0) {
            transpilers = [];
            overridePatches = overridePatches.Where(patch => patch.IgnoreBasePatch).ToArray();
        } else {
            children = new[] {((Delegate) ChangeParameter).Method}.Concat(customPatchMethods.Where(method => method.Name.Contains("Transpiler"))).Select(CreateEmptyPatch).ToArray();
            if(attribute.PatchType.HasFlag(ReversePatchType.TranspilerCombine)) {
                SortPatchMethods(original, patchInfo.transpilers.ToArray(), debug, out transpilers);
                transpilers = transpilers.Concat(children).ToArray();
            } else transpilers = children;
            if(attribute.PatchType.HasFlag(ReversePatchType.ReplaceCombine)) SetupReplace(original, jaPatchInfo, null);
        }
        originalPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").New(original, data.original,
            prefixes.Select(patch => patch.PatchMethod).ToList(),
            postfixes.Select(patch => patch.PatchMethod).ToList(),
            transpilers.Select(patch => patch.PatchMethod).ToList(),
            finalizers.Select(patch => patch.PatchMethod).ToList(), debug);
    }

    private void SetupReplace(MethodBase original, JAPatchInfo jaPatchInfo, List<MethodInfo> transpiler) {
        SortPatchMethods(original, jaPatchInfo.replaces, debug, out HarmonyLib.Patch[] replaces);
        replace = replaces.Length == 0 ? null : replaces.Last().PatchMethod;
        if(replace == null || customReverse) return;
        MethodInfo method = ((Delegate) ChangeParameter).Method;
        HarmonyLib.Patch[] newTranspilers = new HarmonyLib.Patch[transpilers.Length + 1];
        transpilers.CopyTo(newTranspilers, 0);
        newTranspilers[^1] = CreateEmptyPatch(method);
        transpiler.Add(method);
    }

    private HarmonyLib.Patch CreateEmptyPatch(MethodInfo method) => new(method, 0, "", 0, [], [], debug);

    private TriedPatchData CreateEmptyTryPatch(MethodInfo method, JAMod mod) => new(method, 0, "", 0, [], [], debug, mod);

    private void SetupPrefixRemove() {
        bool a = false;
        prefixes = prefixes.Where(pre => {
            if(a && PrefixAffectsOriginal(pre.PatchMethod)) return false;
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

    internal static bool PrefixAffectsOriginal(MethodInfo fix) => throw new NotImplementedException();

    internal static void AddOverride(JAMethodPatcher patcher, ILGenerator il, MethodBase original, JAEmitter emitter, bool ignore, ref Label? label) {
        foreach(OverridePatchData patch in patcher.overridePatches) {
            if(patch.IgnoreBasePatch != ignore) continue;
            label ??= il.DefineLabel();
            Label endLabel = il.DefineLabel();
            if(patch.tryCatch) emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock), out _);
            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Isinst, patch.targetType);
            emitter.Emit(OpCodes.Brfalse, endLabel);
            emitter.Emit(OpCodes.Ldarg_0);
            foreach(ParameterInfo parameter in patch.patchMethod.GetParameters()) {
                ParameterInfo originalParameter = original.GetParameters().FirstOrDefault(p => p.Name == parameter.Name);
                if(originalParameter == null && parameter.Name.StartsWith("__") && int.TryParse(parameter.Name[2..], out int i)) originalParameter = original.GetParameters()[i];
                if(originalParameter != null) EmitArg(emitter, originalParameter.Position + (original.IsStatic ? 0 : 1));
                else if(parameter.Name.StartsWith("___")) {
                    FieldInfo field = patch.targetType.GetField(parameter.Name[3..]);
                    if(field == null) throw new Exception("Field Not Found: " + parameter.Name);
                    if(field.IsStatic) emitter.Emit(parameter.ParameterType.IsByRef ? OpCodes.Ldsflda : OpCodes.Ldsfld, field);
                    else {
                        emitter.Emit(OpCodes.Ldarg_0);
                        emitter.Emit(parameter.ParameterType.IsByRef ? OpCodes.Ldflda : OpCodes.Ldfld, field);
                    }
                } else emitter.Emit(OpCodes.Ldnull);
            }
            emitter.Emit(OpCodes.Call, patch.patchMethod);
            emitter.Emit(OpCodes.Br, label.Value);
            emitter.MarkLabel(endLabel);
            if(patch.tryCatch) {
                emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock), out _);
                emitter.Emit(OpCodes.Ldsfld, patch.mod.staticField);
                emitter.Emit(OpCodes.Ldstr, patch.id);
                emitter.Emit(OpCodes.Ldc_I4_2);
                emitter.Emit(OpCodes.Call, ((Delegate) JAMod.LogPatchException).Method);
                emitter.MarkBlockAfter(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            }
        }
    }

    internal static IEnumerable<CodeInstruction> EmitterPatch(IEnumerable<CodeInstruction> transpiler, ILGenerator generator) {
        CodeInstruction[] list = transpiler.ToArray();
        Type emitterType = typeof(Harmony).Assembly.GetType("HarmonyLib.Emitter");
        foreach(CodeInstruction instruction in list)
            if(instruction.operand is MethodInfo method && method.DeclaringType == typeof(JAEmitter))
                instruction.operand = emitterType.Method(method.Name, method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        return list;
    }

    internal static MethodInfo CreateReplacement(JAMethodPatcher patcher, out Dictionary<int, CodeInstruction> finalInstructions) {
        _ = Transpiler(null, null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            Type methodPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher");
            LocalBuilder patcher = generator.DeclareLocal(methodPatcher);
            LocalBuilder gotoFinishLabel = generator.DeclareLocal(typeof(Label?));
            LocalBuilder gotoPostfixLabel = generator.DeclareLocal(typeof(Label?));
            CodeInstruction originalArg0 = new(OpCodes.Ldloc, patcher);
            List<CodeInstruction> list = [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher")),
                new(OpCodes.Stloc, patcher),
                new(OpCodes.Ldloca, gotoFinishLabel),
                new(OpCodes.Initobj, typeof(Label?)),
                new(OpCodes.Ldloca, gotoPostfixLabel),
                new(OpCodes.Initobj, typeof(Label?)),
                new(OpCodes.Ldarg_0),
                originalArg0,
                new(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "il")),
                originalArg0,
                new(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "original")),
                originalArg0,
                new(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "emitter")),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ldloca, gotoFinishLabel),
                new(OpCodes.Call, ((Delegate) AddOverride).Method)
            ];
            using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            int state = 0;
            FieldInfo replace = SimpleReflect.Field(typeof(JAMethodPatcher), "replace");
            Label removeLabel = generator.DefineLabel();
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                Recheck:
                if(code.opcode == OpCodes.Ldarg_0) code = originalArg0.Clone().WithLabels(code.labels);
                if(code.opcode == OpCodes.Call && code.operand is MethodInfo { Name: "AddPrefixes" }) {
                    list.Add(new CodeInstruction(OpCodes.Ldarg_0).WithLabels(code.labels));
                    code = new CodeInstruction(OpCodes.Call, typeof(JAMethodPatcher).Method("AddPrefixes"));
                }
                if(code.opcode == OpCodes.Call && code.operand is MethodInfo { Name: "AddPostfixes" } methodInfo) {
                    list.Add(new CodeInstruction(OpCodes.Ldarg_0).WithLabels(code.labels));
                    code = new CodeInstruction(OpCodes.Call, typeof(JAMethodPatcher).Method(methodInfo.GetParameters().Length == 3 ? "AddPostfixes" : "AddPostfixes_old"));
                }
                switch(state) {
                    case 0:
                        if(code.opcode == OpCodes.Ldfld && code.operand is FieldInfo { Name: "il" }) {
                            CodeInstruction oldCode = list[^1];
                            list.Remove(oldCode);
                            Label falseLabel = generator.DefineLabel();
                            Label skipLabel = generator.DefineLabel();
                            list.AddRange([
                                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(oldCode.labels),
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "removes")),
                                new CodeInstruction(OpCodes.Ldlen),
                                new CodeInstruction(OpCodes.Brfalse, falseLabel),
                                new CodeInstruction(OpCodes.Call, typeof(Array).Method("Empty").MakeGenericMethod(typeof(LocalBuilder))),
                                new CodeInstruction(OpCodes.Br, skipLabel),
                                originalArg0.Clone().WithLabels(falseLabel),
                                code,
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Ldfld, replace)
                            ]);
                            enumerator.MoveNext();
                            enumerator.MoveNext();
                            CodeInstruction next = enumerator.Current;
                            enumerator.MoveNext();
                            list.Add(enumerator.Current);
                            enumerator.MoveNext();
                            CodeInstruction moveLabel = enumerator.Current;
                            list.AddRange([
                                moveLabel,
                                new CodeInstruction(OpCodes.Pop),
                                originalArg0,
                                next,
                                new CodeInstruction(OpCodes.Dup),
                                moveLabel
                            ]);
                            while(enumerator.MoveNext()) {
                                CodeInstruction cur = enumerator.Current;
                                if(cur.opcode == OpCodes.Ldarg_0) cur = originalArg0;
                                list.Add(cur);
                                if(cur.opcode == OpCodes.Call) break;
                            }
                            enumerator.MoveNext();
                            code = enumerator.Current;
                            code.labels.Add(skipLabel);
                            state++;
                            goto Recheck;
                        }
                        break;
                    case 1:
                        if(code.opcode == OpCodes.Call && code.operand is MethodInfo { Name: "AddPrefixes" }) {
                            list.AddRange([
                                code,
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "removes")),
                                new CodeInstruction(OpCodes.Ldlen),
                                new CodeInstruction(OpCodes.Brtrue, removeLabel)
                            ]);
                            state++;
                            continue;
                        }
                        break;
                    case 2:
                        if(code.opcode == OpCodes.Ldfld && code.operand is FieldInfo { Name: "source" }) {
                            CodeInstruction oldCode = list[^1];
                            list.Remove(oldCode);
                            enumerator.MoveNext();
                            enumerator.MoveNext();
                            CodeInstruction moveLabel = enumerator.Current;
                            list.AddRange([
                                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(oldCode.labels),
                                originalArg0,
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "il")),
                                originalArg0,
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "original")),
                                originalArg0,
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "emitter")),
                                new CodeInstruction(OpCodes.Ldc_I4_0),
                                new CodeInstruction(OpCodes.Ldloca, gotoPostfixLabel),
                                new CodeInstruction(OpCodes.Call, ((Delegate) AddOverride).Method),
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Ldfld, replace),
                                new CodeInstruction(OpCodes.Dup),
                                moveLabel,
                                new CodeInstruction(OpCodes.Pop),
                                originalArg0,
                                code,
                                new CodeInstruction(OpCodes.Dup),
                                moveLabel
                            ]);
                            state++;
                            continue;
                        }
                        break;
                    case 3:
                        if(code.opcode == OpCodes.Newobj && code.operand is ConstructorInfo info && info.DeclaringType == typeof(List<Label>)) {
                            enumerator.MoveNext();
                            Label notNullLabel = generator.DefineLabel();
                            Label notIf = generator.DefineLabel();
                            LocalBuilder locking = generator.DeclareLocal(typeof(bool).MakeByRefType());
                            Label replaceIsSet = generator.DefineLabel();
                            Label sourceIsSet = generator.DefineLabel();
                            list.AddRange([
                                code,
                                enumerator.Current,
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Ldfld, replace),
                                new CodeInstruction(OpCodes.Ldnull),
                                new CodeInstruction(OpCodes.Call, typeof(MethodBase).Method("op_Inequality")),
                                new CodeInstruction(OpCodes.Brtrue, notNullLabel),
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "customReverse")),
                                new CodeInstruction(OpCodes.Brfalse, notIf),
                                new CodeInstruction(OpCodes.Ldsfld, SimpleReflect.Field(typeof(JAMethodPatcher), "_parameterMap")).WithLabels(notNullLabel).WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock)),
                                new CodeInstruction(OpCodes.Ldloca, locking),
                                new CodeInstruction(OpCodes.Call, typeof(Monitor).Method("Enter", typeof(object), locking.LocalType)),
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Ldfld, replace),
                                new CodeInstruction(OpCodes.Dup),
                                new CodeInstruction(OpCodes.Brtrue, replaceIsSet),
                                new CodeInstruction(OpCodes.Pop),
                                originalArg0,
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "original")),
                                originalArg0.Clone().WithLabels(replaceIsSet),
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "source")),
                                new CodeInstruction(OpCodes.Dup),
                                new CodeInstruction(OpCodes.Brtrue, sourceIsSet),
                                new CodeInstruction(OpCodes.Pop),
                                originalArg0,
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "original")),
                                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(sourceIsSet),
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "customReverse")),
                                new CodeInstruction(OpCodes.Call, ((Delegate) SetupParameter).Method)
                            ]);
                            List<CodeInstruction> finalInstructions = [];
                            while(enumerator.MoveNext()) {
                                CodeInstruction cur = enumerator.Current;
                                if(cur.opcode == OpCodes.Ldarg_0) cur = originalArg0;
                                finalInstructions.Add(cur);
                                if(cur.opcode == OpCodes.Pop) break;
                            }
                            list.AddRange(finalInstructions);
                            Label tryLeave = generator.DefineLabel();
                            Label lockFail = generator.DefineLabel();
                            Label after = generator.DefineLabel();
                            list.AddRange([
                                new CodeInstruction(OpCodes.Leave, tryLeave),
                                new CodeInstruction(OpCodes.Ldloc, locking).WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock)),
                                new CodeInstruction(OpCodes.Brfalse, lockFail),
                                new CodeInstruction(OpCodes.Ldsfld, SimpleReflect.Field(typeof(JAMethodPatcher), "_parameterMap")),
                                new CodeInstruction(OpCodes.Call, typeof(Monitor).Method("Exit")),
                                new CodeInstruction(OpCodes.Endfinally).WithLabels(lockFail).WithBlocks(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock)),
                                new CodeInstruction(OpCodes.Br, after).WithLabels(tryLeave),
                            ]);
                            int count = list.Count;
                            list.AddRange(finalInstructions);
                            list[count] = list[count].Clone().WithLabels(notIf);
                            list[^1] = list[^1].Clone().WithLabels(after);
                            state++;
                            continue;
                        }
                        break;
                    case 4:
                        if(code.opcode == OpCodes.Callvirt && code.operand is MethodInfo { Name: "Emit" }) {
                            list.AddRange([
                                code,
                                new CodeInstruction(OpCodes.Ldloca, gotoPostfixLabel),
                                new CodeInstruction(OpCodes.Call, typeof(Label?).Method("get_HasValue")),
                                new CodeInstruction(OpCodes.Brfalse, removeLabel),
                                originalArg0,
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "emitter")),
                                new CodeInstruction(OpCodes.Ldloca, gotoPostfixLabel),
                                new CodeInstruction(OpCodes.Call, typeof(Label?).Method("get_Value")),
                                new CodeInstruction(OpCodes.Call, typeof(Harmony).Assembly.GetType("HarmonyLib.Emitter").Method("MarkLabel", typeof(Label))),
                            ]);
                            enumerator.MoveNext();
                            code = enumerator.Current.WithLabels(removeLabel);
                            state++;
                        }
                        break;
                    case 5:
                        if(code.opcode == OpCodes.Ldsfld && code.operand is FieldInfo { Name: "Ret" }) {
                            int loc = list.Count - 2;
                            List<Label> labels = list[loc].labels;
                            Label skipFinish = generator.DefineLabel();
                            list[loc--].labels = [skipFinish];
                            Label skip = (Label) list[loc].operand;
                            Label run = generator.DefineLabel();
                            labels.Add(run);
                            if(JAPatcher._isOldHarmony) list.RemoveAt(loc);
                            else list.RemoveRange(--loc, 2);
                            list.InsertRange(loc, [
                                new CodeInstruction(OpCodes.Brtrue, run),
                                new CodeInstruction(OpCodes.Ldloca, gotoFinishLabel),
                                new CodeInstruction(OpCodes.Call, typeof(Label?).Method("get_HasValue")),
                                new CodeInstruction(OpCodes.Brfalse, skip),
                                new CodeInstruction(OpCodes.Ldloca, gotoFinishLabel) {
                                    labels = labels
                                },
                                new CodeInstruction(OpCodes.Call, typeof(Label?).Method("get_HasValue")),
                                new CodeInstruction(OpCodes.Brfalse, skipFinish),
                                originalArg0,
                                new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(methodPatcher, "emitter")),
                                new CodeInstruction(OpCodes.Ldloca, gotoFinishLabel),
                                new CodeInstruction(OpCodes.Call, typeof(Label?).Method("get_Value")),
                                new CodeInstruction(OpCodes.Call, typeof(Harmony).Assembly.GetType("HarmonyLib.Emitter").Method("MarkLabel", typeof(Label))),
                            ]);
                            state++;
                        }
                        break;
                }
                list.Add(code);
            }
            return list;
        }
    }

    #region AddPrePost

    private static FieldInfo[] AddPrefixesSubArguments;
    private static FieldInfo[] AddPostfixesSubArguments;

    internal static void LoadAddPrePostMethod(Harmony harmony) {
        Type methodPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher");
        MethodInfo methodInfo = methodPatcher.Method("AddPrefixes");
        List<CodeInstruction> instructions = PatchProcessor.GetCurrentInstructions(methodInfo);
        MethodInfo subMethod = null;
        AddPrefixesSubArguments = new FieldInfo[3];
        using(IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator()) {
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                if(code.opcode == OpCodes.Ldarg_0 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Stfld && next.operand is FieldInfo field)
                        AddPrefixesSubArguments[0] = field;
                }
                if(code.opcode == OpCodes.Ldarg_1 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Stfld && next.operand is FieldInfo field)
                        AddPrefixesSubArguments[1] = field;
                }
                if(code.opcode == OpCodes.Ldarg_2 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Stfld && next.operand is FieldInfo field)
                        AddPrefixesSubArguments[2] = field;
                }
                if(code.opcode == OpCodes.Ldftn && code.operand is MethodInfo m) subMethod = m;
            }
        }
        harmony.CreateReversePatcher(subMethod, new HarmonyMethod(((Delegate) AddPrefixes_b__0).Method)).Patch();
        methodInfo = methodPatcher.Method("AddPostfixes");
        instructions = PatchProcessor.GetCurrentInstructions(methodInfo);
        AddPostfixesSubArguments = new FieldInfo[5];
        using(IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator()) {
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                if(code.opcode == OpCodes.Ldarg_0 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Stfld && next.operand is FieldInfo field)
                        AddPostfixesSubArguments[0] = field;
                }
                if(code.opcode == OpCodes.Ldarg_1 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Stfld && next.operand is FieldInfo field)
                        AddPostfixesSubArguments[1] = field;
                }
                if(code.opcode == OpCodes.Ldarg_2 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Stfld && next.operand is FieldInfo field)
                        AddPostfixesSubArguments[2] = field;
                }
                if(code.opcode == OpCodes.Ldarg_3 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Stfld && next.operand is FieldInfo field)
                        AddPostfixesSubArguments[3] = field;
                }
                if(code.opcode == OpCodes.Ldc_I4_0 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Stfld && next.operand is FieldInfo field)
                        AddPostfixesSubArguments[4] = field;
                }
                if(code.opcode == OpCodes.Ldftn && code.operand is MethodInfo method) subMethod = method;
            }
        }
        harmony.CreateReversePatcher(subMethod, new HarmonyMethod(((Delegate) AddPostfixes_b__0).Method)).Patch();
        harmony.Patch(((Delegate) EmitArg).Method, transpiler: new HarmonyMethod(((Delegate) EmitterPatch).Method));
        harmony.Patch(methodPatcher.Method("EmitCallParameter"), transpiler: new HarmonyMethod(((Delegate) EmitCallParameterFix).Method));
    }

    private static void AddPrefixes(object _, Dictionary<string, LocalBuilder> variables, LocalBuilder runOriginalVariable, JAMethodPatcher patcher) {
        foreach(HarmonyLib.Patch patch in patcher.prefixes) AddPrefixes_b__0(patcher, patch, variables, runOriginalVariable);
    }

    private static void AddPrefixes_b__0(JAMethodPatcher patcher, HarmonyLib.Patch patch, Dictionary<string, LocalBuilder> variables, LocalBuilder runOriginalVariable) {
        _ = Transpiler(null, null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            Assembly harmonyAssembly = typeof(Harmony).Assembly;
            Type emitterType = harmonyAssembly.GetType("HarmonyLib.Emitter");
            LocalBuilder fix = generator.DeclareLocal(typeof(MethodInfo));
            CodeInstruction originalPatcher = new(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher"));
            LocalBuilder emitter = generator.DeclareLocal(emitterType);
            List<CodeInstruction> list = [
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldfld, SimpleReflect.Field(typeof(HarmonyLib.Patch), "patchMethod")),
                new(OpCodes.Stloc, fix),
                new(OpCodes.Ldarg_0),
                originalPatcher,
                new(OpCodes.Ldfld, SimpleReflect.Field(harmonyAssembly.GetType("HarmonyLib.MethodPatcher"), "emitter")),
                new(OpCodes.Stloc, emitter)
            ];
            using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            LocalBuilder notUsingLocal = generator.DeclareLocal(typeof(Label?));
            int state = 0;
            LocalBuilder requireTry = generator.DeclareLocal(typeof(bool));
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                Recheck:
                if(code.opcode == OpCodes.Ldarg_0 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Ldfld || next.opcode == OpCodes.Ldflda) {
                        FieldInfo field = (FieldInfo) next.operand;
                        if(field == AddPrefixesSubArguments[0] && enumerator.MoveNext()) {
                            CodeInstruction next2 = enumerator.Current;
                            if(next2.opcode == OpCodes.Ldfld && next2.operand is FieldInfo { Name: "emitter" })
                                code = new CodeInstruction(OpCodes.Ldloc, emitter).WithLabels(code.labels);
                            else {
                                list.AddRange([
                                    code,
                                    originalPatcher
                                ]);
                                code = next2;
                                goto Recheck;
                            }
                        } else if(field == AddPrefixesSubArguments[1]) code = new CodeInstruction(OpCodes.Ldarg_2).WithLabels(code.labels).WithBlocks(code.blocks);
                        else if(field == AddPrefixesSubArguments[2]) code = new CodeInstruction(OpCodes.Ldarg_3).WithLabels(code.labels).WithBlocks(code.blocks);
                        else {
                            MethodInfo method = null;
                            while(enumerator.MoveNext()) {
                                code = enumerator.Current;
                                if(code.opcode == OpCodes.Ldftn && code.operand is MethodInfo info) method = info;
                                if(code.opcode == OpCodes.Call && code.operand is MethodInfo { Name: "Do" }) break;
                            }
                            LocalBuilder enumeratorVar = generator.DeclareLocal(typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator));
                            LocalBuilder tmpBoxVar = generator.DeclareLocal(typeof(KeyValuePair<LocalBuilder, Type>));
                            Label start = generator.DefineLabel();
                            Label check = generator.DefineLabel();
                            list.AddRange([
                                new CodeInstruction(OpCodes.Callvirt, typeof(List<KeyValuePair<LocalBuilder, Type>>).Method("GetEnumerator")),
                                new CodeInstruction(OpCodes.Stloc, enumeratorVar),
                                new CodeInstruction(OpCodes.Br, check),
                                new CodeInstruction(OpCodes.Ldloca, enumeratorVar).WithLabels(start),
                                new CodeInstruction(OpCodes.Call, typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator).Method("get_Current")),
                                new CodeInstruction(OpCodes.Stloc, tmpBoxVar)
                            ]);
                            List<CodeInstruction> methodInstructions = PatchProcessor.GetCurrentInstructions(method, generator: generator);
                            list.Invoke("EnsureCapacity", list.Count + methodInstructions.Count);
                            IEnumerator<CodeInstruction> codes = methodInstructions.GetEnumerator();
                            while(codes.MoveNext()) {
                                CodeInstruction repeat = codes.Current;
                                if(repeat.opcode == OpCodes.Ret) continue;
                                if(repeat.opcode == OpCodes.Ldarg_0) {
                                    codes.MoveNext();
                                    codes.MoveNext();
                                    if(codes.Current.operand is FieldInfo { Name: "emitter" }) list.Add(new CodeInstruction(OpCodes.Ldloc, emitter));
                                    else list.AddRange([
                                        repeat,
                                        originalPatcher,
                                        codes.Current
                                    ]);
                                    continue;
                                }
                                if(repeat.opcode == OpCodes.Ldarga_S) repeat = new CodeInstruction(OpCodes.Ldloca, tmpBoxVar);
                                list.Add(repeat);
                            }
                            list.AddRange([
                                new CodeInstruction(OpCodes.Ldloca, enumeratorVar).WithLabels(check),
                                new CodeInstruction(OpCodes.Call, typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator).Method("MoveNext")),
                                new CodeInstruction(OpCodes.Brtrue, start)
                            ]);
                            continue;
                        }
                    } else throw new Exception("This Code Is Not field: " + next.opcode);
                } else if(code.opcode == OpCodes.Ldarg_1) code = new CodeInstruction(OpCodes.Ldloc, fix).WithLabels(code.labels);
                else if(code.opcode == OpCodes.Ldsfld && code.operand is FieldInfo field && field.FieldType == typeof(Func<ParameterInfo, bool>)) {
                    while(enumerator.MoveNext()) if(enumerator.Current.opcode == OpCodes.Call) break;
                    list.Add(new CodeInstruction(OpCodes.Call, ((Delegate) CheckArgs).Method));
                    continue;
                }
                switch(state) {
                    case 0:
                    case 1:
                        if(code.opcode == OpCodes.Callvirt && code.operand is MethodInfo { Name: "Emit" }) {
                            state++;
                            if(state == 1) {
                                list.Add(code);
                                enumerator.MoveNext();
                                code = enumerator.Current;
                                Label falseLabel = generator.DefineLabel();
                                list.AddRange([
                                    new CodeInstruction(OpCodes.Ldarg_0).WithLabels(code.labels),
                                    new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "tryPrefixes")),
                                    new CodeInstruction(OpCodes.Ldarg_1),
                                    new CodeInstruction(OpCodes.Call, typeof(Enumerable).Methods().First(m => m.Name == "Contains").MakeGenericMethod(typeof(HarmonyLib.Patch))),
                                    new CodeInstruction(OpCodes.Dup),
                                    new CodeInstruction(OpCodes.Stloc, requireTry),
                                    new CodeInstruction(OpCodes.Brfalse, falseLabel),
                                    new CodeInstruction(OpCodes.Ldloc, emitter),
                                    new CodeInstruction(OpCodes.Ldc_I4_0),
                                    new CodeInstruction(OpCodes.Ldnull),
                                    new CodeInstruction(OpCodes.Newobj, typeof(ExceptionBlock).Constructor(typeof(ExceptionBlockType), typeof(Type))),
                                    new CodeInstruction(OpCodes.Ldloca, notUsingLocal),
                                    new CodeInstruction(OpCodes.Callvirt, emitterType.Method("MarkBlockBefore")),
                                    new CodeInstruction(OpCodes.Nop).WithLabels(falseLabel)
                                ]);
                                code.labels.Clear();
                                goto Recheck;
                            }
                        }
                        break;
                    case 2:
                        if(code.opcode == OpCodes.Throw) state++;
                        break;
                    case 3:
                        if((code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt) && code.operand is MethodInfo { Name: "Emit" }) {
                            list.Add(code);
                            enumerator.MoveNext();
                            code = enumerator.Current;
                            Label skipLabel = generator.DefineLabel();
                            list.AddRange([
                                new CodeInstruction(OpCodes.Ldloc, requireTry).WithLabels(code.labels),
                                new CodeInstruction(OpCodes.Brfalse, skipLabel)
                            ]);
                            code.labels.Clear();
                            List<CodeInstruction> handle = PatchProcessor.GetCurrentInstructions(((Delegate) handleException).Method, generator: generator);
                            list.Invoke("EnsureCapacity", list.Count + handle.Count);
                            foreach(CodeInstruction instruction in handle) {
                                if(instruction.operand is LocalBuilder) {
                                    if(instruction.opcode == OpCodes.Ldloca_S) instruction.opcode = OpCodes.Ldloca;
                                    instruction.operand = notUsingLocal;
                                }
                                if(instruction.opcode == OpCodes.Ldarg_0) list.Add(new CodeInstruction(OpCodes.Ldloc, emitter).WithLabels(instruction.labels));
                                else if(instruction.opcode == OpCodes.Ldarg_2) list.Add(new CodeInstruction(OpCodes.Ldsfld, SimpleReflect.Field(typeof(OpCodes), "Ldc_I4_0")));
                                else if(instruction.operand is MethodInfo info && info.DeclaringType == typeof(JAEmitter)) {
                                    instruction.operand = harmonyAssembly.GetType("HarmonyLib.Emitter").Method(info.Name, info.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
                                    list.Add(instruction);
                                } else if(instruction.opcode == OpCodes.Ret) list.Add(code.WithLabels(skipLabel));
                                else list.Add(instruction);
                            }
                            state++;
                            continue;
                        }
                        break;
                }
                list.Add(code);
            }
            return list;
        }
    }

    private static bool AddPostfixes(object _, Dictionary<string, LocalBuilder> variables, LocalBuilder runOriginalVariable, bool passthroughPatches, JAMethodPatcher patcher) {
        bool result = false;
        foreach(HarmonyLib.Patch patch in patcher.postfixes) if(passthroughPatches == (patch.PatchMethod.ReturnType != typeof (void)))
            AddPostfixes_b__0(patcher, patch, variables, runOriginalVariable, passthroughPatches, ref result);
        return result;
    }

    private static bool AddPostfixes_old(object _, Dictionary<string, LocalBuilder> variables, bool passthroughPatches, JAMethodPatcher patcher) => AddPostfixes(_, variables, null, passthroughPatches, patcher);

    private static void AddPostfixes_b__0(JAMethodPatcher patcher, HarmonyLib.Patch patch, Dictionary<string, LocalBuilder> variables, LocalBuilder runOriginalVariable, bool passthroughPatches, ref bool result) {
        _ = Transpiler(null, null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            Assembly harmonyAssembly = typeof(Harmony).Assembly;
            Type emitterType = harmonyAssembly.GetType("HarmonyLib.Emitter");
            LocalBuilder fix = generator.DeclareLocal(typeof(MethodInfo));
            CodeInstruction originalPatcher = new(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher"));
            LocalBuilder emitter = generator.DeclareLocal(emitterType);
            LocalBuilder notUsingLocal = generator.DeclareLocal(typeof(Label?));
            LocalBuilder requireTry = generator.DeclareLocal(typeof(bool));
            Label falseLabel = generator.DefineLabel();
            List<CodeInstruction> list = [
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldfld, SimpleReflect.Field(typeof(HarmonyLib.Patch), "patchMethod")),
                new(OpCodes.Stloc, fix),
                new(OpCodes.Ldarg_0),
                originalPatcher,
                new(OpCodes.Ldfld, SimpleReflect.Field(harmonyAssembly.GetType("HarmonyLib.MethodPatcher"), "emitter")),
                new(OpCodes.Stloc, emitter),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "tryPostfixes")),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Call, typeof(Enumerable).Methods().First(m => m.Name == "Contains").MakeGenericMethod(typeof(HarmonyLib.Patch))),
                new(OpCodes.Dup),
                new(OpCodes.Stloc, requireTry),
                new(OpCodes.Brfalse, falseLabel),
                new(OpCodes.Ldloc, emitter),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldnull),
                new(OpCodes.Newobj, typeof(ExceptionBlock).Constructor(typeof(ExceptionBlockType), typeof(Type))),
                new(OpCodes.Ldloca, notUsingLocal),
                new(OpCodes.Callvirt, emitterType.Method("MarkBlockBefore")),
                new CodeInstruction(OpCodes.Nop).WithLabels(falseLabel)
            ];
            using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            bool parameterCheck = true;
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                Recheck:
                if(code.opcode == OpCodes.Ldarg_0 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    if(next.opcode == OpCodes.Ldfld || next.opcode == OpCodes.Ldflda || next.opcode == OpCodes.Stfld) {
                        FieldInfo field = (FieldInfo) next.operand;
                        if(field == AddPostfixesSubArguments[0]) {
                            CodeInstruction next2 = enumerator.MoveNext() ? enumerator.Current : null;
                            if(next2 != null && next2.opcode == OpCodes.Ldfld && next2.operand is FieldInfo { Name: "emitter" })
                                code = new CodeInstruction(OpCodes.Ldloc, emitter).WithLabels(code.labels).WithBlocks(code.blocks);
                            else {
                                list.AddRange([
                                    code,
                                    originalPatcher
                                ]);
                                code = next2;
                                goto Recheck;
                            }
                        } else if(field == AddPostfixesSubArguments[1]) code = new CodeInstruction(OpCodes.Ldarg_2).WithLabels(code.labels).WithBlocks(code.blocks);
                        else if(field == AddPostfixesSubArguments[2]) code = new CodeInstruction(OpCodes.Ldarg_3).WithLabels(code.labels).WithBlocks(code.blocks);
                        else if(field == AddPostfixesSubArguments[3]) code = new CodeInstruction(OpCodes.Ldarg_S, 4).WithLabels(code.labels).WithBlocks(code.blocks);
                        else if(field == AddPostfixesSubArguments[4]) code = new CodeInstruction(next.opcode == OpCodes.Stfld ? OpCodes.Starg_S : OpCodes.Ldarg_S, 5).WithLabels(code.labels).WithBlocks(code.blocks);
                        else {
                            MethodInfo method = null;
                            while(enumerator.MoveNext()) {
                                code = enumerator.Current;
                                if(code.opcode == OpCodes.Ldftn && code.operand is MethodInfo info) method = info;
                                if(code.opcode == OpCodes.Call && code.operand is MethodInfo { Name: "Do" }) break;
                            }
                            LocalBuilder enumeratorVar = generator.DeclareLocal(typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator));
                            LocalBuilder tmpBoxVar = generator.DeclareLocal(typeof(KeyValuePair<LocalBuilder, Type>));
                            Label start = generator.DefineLabel();
                            Label check = generator.DefineLabel();
                            list.AddRange([
                                new CodeInstruction(OpCodes.Callvirt, typeof(List<KeyValuePair<LocalBuilder, Type>>).Method("GetEnumerator")),
                                new CodeInstruction(OpCodes.Stloc, enumeratorVar),
                                new CodeInstruction(OpCodes.Br, check),
                                new CodeInstruction(OpCodes.Ldloca, enumeratorVar).WithLabels(start),
                                new CodeInstruction(OpCodes.Call, typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator).Method("get_Current")),
                                new CodeInstruction(OpCodes.Stloc, tmpBoxVar)
                            ]);
                            List<CodeInstruction> methodInstructions = PatchProcessor.GetCurrentInstructions(method, generator: generator);
                            list.Invoke("EnsureCapacity", list.Count + methodInstructions.Count);
                            IEnumerator<CodeInstruction> codes = methodInstructions.GetEnumerator();
                            while(codes.MoveNext()) {
                                CodeInstruction repeat = codes.Current;
                                if(repeat.opcode == OpCodes.Ret) continue;
                                if(repeat.opcode == OpCodes.Ldarg_0) {
                                    codes.MoveNext();
                                    codes.MoveNext();
                                    if(codes.Current.operand is FieldInfo { Name: "emitter" }) list.Add(new CodeInstruction(OpCodes.Ldloc, emitter));
                                    else list.AddRange([
                                        repeat,
                                        originalPatcher,
                                        codes.Current
                                    ]);
                                    continue;
                                }
                                if(repeat.opcode == OpCodes.Ldarga_S) repeat = new CodeInstruction(OpCodes.Ldloca, tmpBoxVar);
                                list.Add(repeat);
                            }
                            list.AddRange([
                                new CodeInstruction(OpCodes.Ldloca, enumeratorVar).WithLabels(check),
                                new CodeInstruction(OpCodes.Call, typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator).Method("MoveNext")),
                                new CodeInstruction(OpCodes.Brtrue, start)
                            ]);
                            continue;
                        }
                    } else {
                        if(!enumerator.MoveNext() || enumerator.Current.opcode != OpCodes.Stfld) throw new Exception("This Code Is Not field: " + next.opcode);
                        list.AddRange([
                            new CodeInstruction(OpCodes.Ldarg_S, 5),
                            next,
                            new CodeInstruction(OpCodes.Stind_I1)
                        ]);
                        continue;
                    }
                } else if(code.opcode == OpCodes.Ldarg_1) {
                    enumerator.MoveNext();
                    code = enumerator.Current;
                    if(parameterCheck && code.operand is MethodInfo { Name: "get_ReturnType" }) {
                        Label skipLabel = generator.DefineLabel();
                        list.AddRange([
                            new CodeInstruction(OpCodes.Ldloc, requireTry),
                            new CodeInstruction(OpCodes.Brfalse, skipLabel)
                        ]);
                        List<CodeInstruction> handle = PatchProcessor.GetCurrentInstructions(((Delegate) handleException).Method, generator: generator);
                        list.Invoke("EnsureCapacity", list.Count + handle.Count);
                        foreach(CodeInstruction instruction in handle) {
                            if(instruction.operand is LocalBuilder) {
                                if(instruction.opcode == OpCodes.Ldloca_S) instruction.opcode = OpCodes.Ldloca;
                                instruction.operand = notUsingLocal;
                            }
                            if(instruction.opcode == OpCodes.Ldarg_0) list.Add(new CodeInstruction(OpCodes.Ldloc, emitter).WithLabels(instruction.labels));
                            else if(instruction.opcode == OpCodes.Ldarg_2) list.Add(new CodeInstruction(OpCodes.Ldsfld, SimpleReflect.Field(typeof(OpCodes), "Ldc_I4_1")));
                            else if(instruction.operand is MethodInfo info && info.DeclaringType == typeof(JAEmitter)) {
                                instruction.operand = harmonyAssembly.GetType("HarmonyLib.Emitter").Method(info.Name, info.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
                                list.Add(instruction);
                            } else if(instruction.opcode == OpCodes.Ret) list.Add(new CodeInstruction(OpCodes.Nop).WithLabels(skipLabel));
                            else list.Add(instruction);
                        }
                        parameterCheck = false;
                    }
                    list.Add(new CodeInstruction(OpCodes.Ldloc, fix));
                    goto Recheck;
                }
                else if(code.opcode == OpCodes.Ldsfld && code.operand is FieldInfo field && field.FieldType == typeof(Func<ParameterInfo, bool>)) {
                    while(enumerator.MoveNext()) if(enumerator.Current.opcode == OpCodes.Call) break;
                    list.Add(new CodeInstruction(OpCodes.Call, ((Delegate) CheckArgs).Method));
                    continue;
                }
                list.Add(code);
            }
            return list;
        }
    }

    private static void handleException(JAEmitter emitter, HarmonyLib.Patch patch, OpCode isPrefix) {
        emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock), out _);
        emitter.Emit(OpCodes.Ldsfld, ((TriedPatchData) patch).mod.staticField);
        emitter.Emit(OpCodes.Ldstr, patch.owner);
        emitter.Emit(isPrefix);
        emitter.Emit(OpCodes.Call, ((Delegate) JAMod.LogPatchException).Method);
        emitter.MarkBlockAfter(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
    }

    private static bool CheckArgs(ParameterInfo[] parameters) {
        foreach(ParameterInfo p in parameters) if(p.Name == "__args") return true;
        return false;
    }

    internal static IEnumerable<CodeInstruction> EmitCallParameterFix(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        LocalBuilder method = generator.DeclareLocal(typeof(MethodBase));
        LocalBuilder instanceId = generator.DeclareLocal(typeof(int));
        Label skip = generator.DefineLabel();
        Type methodPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher");
        FieldInfo source = SimpleReflect.Field(methodPatcher, "source");
        FieldInfo original = SimpleReflect.Field(methodPatcher, "original");
        List<CodeInstruction> list = [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, source),
            new(OpCodes.Dup),
            new(OpCodes.Brtrue, skip),
            new(OpCodes.Pop),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, original),
            new CodeInstruction(OpCodes.Stloc, method).WithLabels(skip),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, source),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, original),
            new(OpCodes.Call, ((Delegate) GetInstanceIndex).Method),
            new(OpCodes.Stloc, instanceId),
        ];
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        while(enumerator.MoveNext()) {
            CodeInstruction code = enumerator.Current;
            if(code.operand is FieldInfo { Name: "original" }) {
                list[^1] = new CodeInstruction(OpCodes.Ldloc, method);
                continue;
            }
            if(code.operand is MethodInfo { Name: "get_IsStatic" }) {
                list.Add(code);
                enumerator.MoveNext();
                code = enumerator.Current;
                if(code.opcode == OpCodes.Brfalse || code.opcode == OpCodes.Brfalse_S) {
                    Label skipLabel = generator.DefineLabel();
                    enumerator.MoveNext();
                    list.AddRange([
                        new CodeInstruction(OpCodes.Brtrue, skipLabel),
                        new CodeInstruction(OpCodes.Ldloc, instanceId),
                        new CodeInstruction(OpCodes.Ldc_I4_M1),
                        new CodeInstruction(OpCodes.Bne_Un, (Label) code.operand),
                        enumerator.Current.WithLabels(skipLabel)
                    ]);
                    continue;
                }
            } else if(code.operand is FieldInfo { Name: "Ldarg_0" }) {
                enumerator.MoveNext();
                list.AddRange([
                    new CodeInstruction(OpCodes.Ldloc, instanceId),
                    new CodeInstruction(OpCodes.Call, ((Delegate) EmitArg).Method),
                ]);
                continue;
            } else if(code.operand is FieldInfo { Name: "Ldarga" }) {
                list.Add(code);
                enumerator.MoveNext();
                code = enumerator.Current;
                if(code.opcode == OpCodes.Ldc_I4_0) code = new CodeInstruction(OpCodes.Ldloc, instanceId);
            } else if(code.operand is MethodInfo { Name: "GetArgumentIndex" }) {
                list.AddRange([
                    code,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, source),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, original),
                    new CodeInstruction(OpCodes.Call, ((Delegate) GetArgIndex).Method),
                ]);
                continue;
            }
            list.Add(code);
        }
        return list;
    }

    private static void EmitArg(JAEmitter emitter, int index) {
        switch(index) {
            case >= 0 and < 4:
                emitter.Emit(index switch {
                    0 => OpCodes.Ldarg_0,
                    1 => OpCodes.Ldarg_1,
                    2 => OpCodes.Ldarg_2,
                    3 => OpCodes.Ldarg_3
                });
                break;
            case < 256:
                emitter.Emit(OpCodes.Ldarg_S, (byte) index);
                break;
            default:
                emitter.Emit(OpCodes.Ldarg, index);
                break;
        }
    }

    private static int GetInstanceIndex(MethodBase source, MethodBase original) {
        if(source == null) return 0;
        if(source.IsStatic) return -1;
        foreach(ParameterInfo parameter in original.GetParameters()) if(parameter.Name == "__instance") return parameter.Position + (original.IsStatic ? 0 : 1);
        return -1;
    }

    private static int GetArgIndex(int index, MethodBase source, MethodBase original) {
        if(source == null || index == -1) return index;
        if(!source.IsStatic) index--;
        ParameterInfo curParam = source.GetParameters()[index];
        foreach(ParameterInfo parameter in original.GetParameters()) if(parameter.Name == curParam.Name) return parameter.Position + (original.IsStatic ? 0 : 1);
        return -1;
    }

    #endregion

    private static void SetupParameter(MethodBase replace, MethodBase original, bool customReverse) {
        ParameterInfo[] replaceParameter = replace.GetParameters();
        ParameterInfo[] originalParameter = original.GetParameters();
        _parameterMap.Clear();
        _parameterFields.Clear();
        foreach(ParameterInfo parameterInfo in replaceParameter) {
            if(parameterInfo.Name.ToLower() == "__instance" && !original.IsStatic) {
                _parameterMap[parameterInfo.Position] = 0;
                continue;
            }
            if(parameterInfo.Name.StartsWith("___")) {
                FieldInfo field = SimpleReflect.Field(original.DeclaringType, parameterInfo.Name[3..]);
                if(field != null) {
                    _parameterFields[parameterInfo.Position] = field;
                    continue;
                }
            }
            if(parameterInfo.Name.StartsWith("__")) {
                if(int.TryParse(parameterInfo.Name[2..], out int index)) {
                    _parameterMap[parameterInfo.Position] = index;
                    continue;
                }
            }
            ParameterInfo parameter = originalParameter.FirstOrDefault(info => info.Name == parameterInfo.Name);
            int isNonStatic = original.IsStatic ? 0 : 1;
            if(parameter != null) {
                if(parameter.ParameterType != parameterInfo.ParameterType) throw new PatchParameterException("Parameter type mismatch: " + parameterInfo.Name);
                _parameterMap[parameterInfo.Position] = parameter.Position + isNonStatic;
                continue;
            }
            if(!customReverse) throw new PatchParameterException("Unknown Parameter: " + parameterInfo.Name);
        }
    }

    private static IEnumerable<CodeInstruction> ChangeParameter(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        List<CodeInstruction> list = instructions.ToList();
        for(int i = 0; i < list.Count; i++) {
            int index = GetParameterIndex(list[i], out bool set, out bool loc);
            if(index <= -1) continue;
            if(_parameterMap.TryGetValue(index, out int newIndex)) {
                list[i] = GetParameterInstruction(newIndex, set, loc);
            } else if(_parameterFields.TryGetValue(index, out FieldInfo info)) {
                if(info.IsStatic) list[i] = new CodeInstruction(set ? OpCodes.Stsfld : loc ? OpCodes.Ldsflda : OpCodes.Ldsfld, info);
                else if(!set) {
                    list[i++] = new CodeInstruction(OpCodes.Ldarg_0);
                    list.Insert(i, new CodeInstruction(loc ? OpCodes.Ldflda : OpCodes.Ldfld, info));
                } else {
                    if(i > 0 && IsNonPopLdCode(list[i - 1].opcode)) {
                        list.Insert(i++ - 1, new CodeInstruction(OpCodes.Ldarg_0));
                        list[i] = new CodeInstruction(OpCodes.Stfld, info);
                    } else {
                        LocalBuilder local = generator.DeclareLocal(info.FieldType);
                        list[i++] = new CodeInstruction(OpCodes.Stloc, local);
                        list.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                        list.Insert(i++, new CodeInstruction(OpCodes.Ldloc, local));
                        list.Insert(i, new CodeInstruction(OpCodes.Stfld, info));
                    }
                }
            } else list[i] = new CodeInstruction(set ? OpCodes.Starg : OpCodes.Ldarg, index * -1 - 2);
        }
        return list;
    }

    private static bool IsNonPopLdCode(OpCode code) => code.Name.StartsWith("ld") && code.GetValue<byte>("pop") == 0;

    private static int GetParameterIndex(CodeInstruction instruction, out bool set, out bool loc) {
        int index = -1;
        set = false;
        loc = false;
        if(instruction.opcode == OpCodes.Ldarg) index = (int) instruction.operand;
        else if(instruction.opcode == OpCodes.Ldarga) {
            index = (int) instruction.operand;
            loc = true;
        } else if(instruction.opcode == OpCodes.Ldarg_S) index = (byte) instruction.operand;
        else if(instruction.opcode == OpCodes.Ldarga_S) {
            index = (byte) instruction.operand;
            loc = true;
        }
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

    private static CodeInstruction GetParameterInstruction(int index, bool set, bool loc) {
        if(set) return index < 256 ? new CodeInstruction(OpCodes.Starg_S, (byte) index) : new CodeInstruction(OpCodes.Starg, index);
        if(loc) return index < 256 ? new CodeInstruction(OpCodes.Ldarga_S, (byte) index) : new CodeInstruction(OpCodes.Ldarga, index);
        return index switch {
            0 => new CodeInstruction(OpCodes.Ldarg_0),
            1 => new CodeInstruction(OpCodes.Ldarg_1),
            2 => new CodeInstruction(OpCodes.Ldarg_2),
            3 => new CodeInstruction(OpCodes.Ldarg_3),
            _ => index < 256 ? new CodeInstruction(OpCodes.Ldarg_S, (byte) index) : new CodeInstruction(OpCodes.Ldarg, index)
        };
    }

    internal static List<CodeInstruction> GetInstructions(ILGenerator generator, MethodBase method, int maxTranspilers, JAPatchInfo jaPatchInfo) {
        Type methodPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher");
        MethodInfo replace = SortPatchMethods(method, jaPatchInfo.replaces, false, out _).Last();
        LocalBuilder[] existingVariables = method != null ? methodPatcher.Invoke<LocalBuilder[]>("DeclareLocalVariables", generator, replace) : throw new ArgumentNullException(nameof (method));
        bool useShift = typeof(Harmony).Assembly.GetType("HarmonyLib.StructReturnBuffer").Invoke<bool>("NeedsFix", [method]);
        object methodCopier = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodCopier").New(replace, generator, existingVariables);
        methodCopier.Invoke("SetArgumentShift", useShift);
        Patches patchInfo = Harmony.GetPatchInfo(method);
        List<MethodInfo> transpilers = methodCopier.GetValue<List<MethodInfo>>("transpilers");
        transpilers.Add(((Delegate) ChangeParameter).Method);
        maxTranspilers--;
        if(patchInfo != null) {
            List<MethodInfo> sortedPatchMethods = SortPatchMethods(method, patchInfo.Transpilers.ToArray(), false, out _);
            for(int index = 0; index < maxTranspilers && index < sortedPatchMethods.Count; ++index)
                transpilers.Add(sortedPatchMethods[index]);
        }
        lock(_parameterMap) {
            SetupParameter(replace, method, false);
            return methodCopier.Invoke<List<CodeInstruction>>("Finalize", [typeof(Harmony).Assembly.GetType("HarmonyLib.Emitter"), typeof(List<Label>), typeof(bool).MakeByRefType()], null, null, false);
        }
    }
}