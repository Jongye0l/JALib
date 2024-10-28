using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using JALib.JAException;
using JALib.Tools;

namespace JALib.Core.Patch;

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
    private readonly object originalPatcher;
    private readonly bool customReverse;

    public JAMethodPatcher(MethodBase original, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
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
            replace = replaces.Length == 0 ? null : replaces.Last().PatchMethod;
            tryPostfixes = jaPatchInfo.tryPostfixes;
            if(replace != null) {
                MethodInfo method = ((Delegate) ChangeParameter).Method;
                transpilers = new[] { CreateEmptyPatch(method) }.Concat(transpilers).ToArray();
                transpiler.Insert(0, method);
            }
        }
        originalPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").New(original, null, prefix, postfix, transpiler, finalizer, debug);
    }

    public JAMethodPatcher(HarmonyMethod standin, MethodBase source, JAPatchInfo jaPatchInfo, MethodInfo postTranspiler) {
        MethodBase original = standin.method;
        Patches patchInfo = Harmony.GetPatchInfo(source);
        debug = standin.debug.GetValueOrDefault() || Harmony.DEBUG;
        prefixes = postfixes = finalizers = removes = tryPrefixes = tryPostfixes = [];
        List<MethodInfo> none = [];
        List<MethodInfo> transpiler = SortPatchMethods(original, patchInfo.Transpilers.ToArray(), debug, out transpilers);
        if(postTranspiler != null) {
            transpiler.Add(postTranspiler);
            transpilers = transpilers.Concat([CreateEmptyPatch(postTranspiler)]).ToArray();
        }
        SortPatchMethods(original, jaPatchInfo.replaces, debug, out HarmonyLib.Patch[] replaces);
        replace = replaces.Length == 0 ? null : replaces.Last().PatchMethod;
        if(replace != null) {
            MethodInfo method = ((Delegate) ChangeParameter).Method;
            transpilers = new[] { CreateEmptyPatch(method) }.Concat(transpilers).ToArray();
            transpiler.Insert(0, method);
        }
        originalPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").New(original, source, none, none, transpiler, none, debug);
    }

    public JAMethodPatcher(ReversePatchData data, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        MethodBase original = data.patchMethod;
        debug = data.debug || Harmony.DEBUG;
        JAReversePatchAttribute attribute = data.attribute;
        JAMod mod = data.mod;
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
                replace = replaces.Length == 0 ? null : replaces.Last().PatchMethod;
                if(replace != null) {
                    MethodInfo method = ((Delegate) ChangeParameter).Method;
                    transpilers = new[] { CreateEmptyPatch(method) }.Concat(transpilers).ToArray();
                }
            }
        }
        originalPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").New(original, data.original,
            prefixes.Select(patch => patch.PatchMethod).ToList(),
            postfixes.Select(patch => patch.PatchMethod).ToList(),
            transpilers.Select(patch => patch.PatchMethod).ToList(),
            finalizers.Select(patch => patch.PatchMethod).ToList(), debug);
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

    internal static MethodInfo CreateReplacement(JAMethodPatcher patcher, out Dictionary<int, CodeInstruction> finalInstructions) {
        _ = Transpiler(null, null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            LocalBuilder patcher = generator.DeclareLocal(typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher"));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher"));
            yield return new CodeInstruction(OpCodes.Stloc, patcher);
            using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            int state = 0;
            FieldInfo replace = SimpleReflect.Field(typeof(JAMethodPatcher), "replace");
            CodeInstruction originalArg0 = new(OpCodes.Ldloc, patcher);
            Label removeLabel = generator.DefineLabel();
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                if(code.opcode == OpCodes.Ldarg_0) code = originalArg0.Clone().WithLabels(code.labels);
                if(code.opcode == OpCodes.Call && code.operand is MethodInfo { Name: "AddPrefixes" }) {
                    yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(code.labels);
                    code = new CodeInstruction(OpCodes.Call, typeof(JAMethodPatcher).Method("AddPrefixes"));
                }
                if(code.opcode == OpCodes.Call && code.operand is MethodInfo { Name: "AddPostfixes" }) {
                    yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(code.labels);
                    code = new CodeInstruction(OpCodes.Call, typeof(JAMethodPatcher).Method("AddPostfixes"));
                }
                switch(state) {
                    case 0:
                        if(code.opcode == OpCodes.Ldfld && code.operand is FieldInfo { Name: "il" }) {
                            yield return new CodeInstruction(OpCodes.Pop);
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "removes"));
                            yield return new CodeInstruction(OpCodes.Ldlen);
                            Label falseLabel = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Brfalse, falseLabel);
                            yield return new CodeInstruction(OpCodes.Call, typeof(Array).Method("Empty").MakeGenericMethod(typeof(LocalBuilder)));
                            Label skipLabel = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Br, skipLabel);
                            yield return originalArg0.Clone().WithLabels(falseLabel);
                            yield return code;
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, replace);
                            enumerator.MoveNext();
                            enumerator.MoveNext();
                            CodeInstruction next = enumerator.Current;
                            enumerator.MoveNext();
                            yield return enumerator.Current;
                            enumerator.MoveNext();
                            CodeInstruction moveLabel = enumerator.Current;
                            yield return moveLabel;
                            yield return new CodeInstruction(OpCodes.Pop);
                            yield return originalArg0;
                            yield return next;
                            yield return new CodeInstruction(OpCodes.Dup);
                            yield return moveLabel;
                            while(enumerator.MoveNext()) {
                                CodeInstruction cur = enumerator.Current;
                                if(cur.opcode == OpCodes.Ldarg_0) cur = originalArg0;
                                yield return cur;
                                if(cur.opcode == OpCodes.Call) break;
                            }
                            yield return new CodeInstruction(OpCodes.Nop).WithLabels(skipLabel);
                            state++;
                            continue;
                        }
                        break;
                    case 1:
                        if(code.opcode == OpCodes.Ldfld && code.operand is FieldInfo { Name: "source" }) {
                            yield return new CodeInstruction(OpCodes.Pop);
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "removes"));
                            yield return new CodeInstruction(OpCodes.Ldlen);
                            enumerator.MoveNext();
                            enumerator.MoveNext();
                            CodeInstruction moveLabel = enumerator.Current;
                            yield return new CodeInstruction(OpCodes.Brtrue, removeLabel);
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, replace);
                            yield return new CodeInstruction(OpCodes.Dup);
                            yield return moveLabel;
                            yield return new CodeInstruction(OpCodes.Pop);
                            yield return originalArg0;
                            yield return code;
                            yield return new CodeInstruction(OpCodes.Dup);
                            yield return moveLabel;
                            state++;
                            continue;
                        }
                        break;
                    case 2:
                        if(code.opcode == OpCodes.Newobj && code.operand is ConstructorInfo info && info.DeclaringType == typeof(List<Label>)) {
                            yield return code;
                            enumerator.MoveNext();
                            yield return enumerator.Current;
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, replace);
                            yield return new CodeInstruction(OpCodes.Ldnull);
                            yield return new CodeInstruction(OpCodes.Call, typeof(MethodBase).Method("op_Inequality"));
                            Label notNullLabel = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Brtrue, notNullLabel);
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "customReverse"));
                            Label notIf = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Brfalse, notIf);
                            LocalBuilder locking = generator.DeclareLocal(typeof(bool).MakeByRefType());
                            yield return new CodeInstruction(OpCodes.Ldc_I4_0).WithLabels(notNullLabel);
                            yield return new CodeInstruction(OpCodes.Stloc, locking);
                            yield return new CodeInstruction(OpCodes.Ldsfld, SimpleReflect.Field(typeof(JAMethodPatcher), "_parameterMap")).WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
                            yield return new CodeInstruction(OpCodes.Ldloca, locking);
                            yield return new CodeInstruction(OpCodes.Call, typeof(Monitor).Method("Enter", typeof(object), locking.LocalType));
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, replace);
                            yield return new CodeInstruction(OpCodes.Dup);
                            Label replaceIsSet = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Brtrue, replaceIsSet);
                            yield return new CodeInstruction(OpCodes.Pop);
                            yield return originalArg0;
                            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher"), "source"));
                            yield return originalArg0.Clone().WithLabels(replaceIsSet);
                            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher"), "original"));
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "customReverse"));
                            yield return new CodeInstruction(OpCodes.Call, typeof(JAMethodPatcher).Method("SetupParameter"));
                            List<CodeInstruction> finalInstructions = [];
                            while(enumerator.MoveNext()) {
                                CodeInstruction cur = enumerator.Current;
                                if(cur.opcode == OpCodes.Ldarg_0) cur = originalArg0;
                                finalInstructions.Add(cur);
                                if(cur.opcode == OpCodes.Pop) break;
                            }
                            foreach(CodeInstruction finalInstruction in finalInstructions) yield return finalInstruction;
                            Label tryLeave = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Leave, tryLeave);
                            yield return new CodeInstruction(OpCodes.Ldloc, locking).WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
                            Label lockFail = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Brfalse, lockFail);
                            yield return new CodeInstruction(OpCodes.Ldsfld, SimpleReflect.Field(typeof(JAMethodPatcher), "_parameterMap"));
                            yield return new CodeInstruction(OpCodes.Call, typeof(Monitor).Method("Exit"));
                            yield return new CodeInstruction(OpCodes.Endfinally).WithLabels(lockFail).WithBlocks(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                            Label after = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Br, after).WithLabels(tryLeave);
                            yield return new CodeInstruction(OpCodes.Nop).WithLabels(notIf);
                            foreach(CodeInstruction finalInstruction in finalInstructions) yield return finalInstruction;
                            yield return new CodeInstruction(OpCodes.Nop).WithLabels(after);
                            state++;
                            continue;
                        }
                        break;
                    case 3:
                        if(code.opcode == OpCodes.Call && code.operand is MethodInfo { Name: "AddPostfixes" }) {
                            yield return code;
                            enumerator.MoveNext();
                            code = enumerator.Current;
                            if(code.opcode == OpCodes.Stloc || code.opcode == OpCodes.Stloc_S) {
                                yield return code;
                                code = new CodeInstruction(OpCodes.Nop).WithLabels(removeLabel);
                            }
                        }
                        break;
                }
                yield return code;
            }
        }
    }

    #region AddPrePost

    private static FieldInfo[] AddPrefixesSubArguments;
    private static FieldInfo[] AddPostfixesSubArguments;

    internal static void LoadAddPrePostMethod(Harmony harmony) {
        MethodInfo methodInfo = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").Method("AddPrefixes");
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
        methodInfo = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher").Method("AddPostfixes");
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
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(HarmonyLib.Patch), "patchMethod"));
            yield return new CodeInstruction(OpCodes.Stloc, fix);
            LocalBuilder emitter = generator.DeclareLocal(emitterType);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher"));
            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(harmonyAssembly.GetType("HarmonyLib.MethodPatcher"), "emitter"));
            yield return new CodeInstruction(OpCodes.Stloc, emitter);
            using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            LocalBuilder exceptionVar = generator.DeclareLocal(typeof(LocalBuilder));
            LocalBuilder notUsingLocal = generator.DeclareLocal(typeof(Label?));
            int state = 0;
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
                                yield return code;
                                yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher"));
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
                            yield return new CodeInstruction(OpCodes.Callvirt, typeof(List<KeyValuePair<LocalBuilder, Type>>).Method("GetEnumerator"));
                            yield return new CodeInstruction(OpCodes.Stloc, enumeratorVar);
                            Label start = generator.DefineLabel();
                            Label check = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Br, check);
                            yield return new CodeInstruction(OpCodes.Ldloca, enumeratorVar).WithLabels(start);
                            yield return new CodeInstruction(OpCodes.Call, typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator).Method("get_Current"));
                            yield return new CodeInstruction(OpCodes.Stloc, tmpBoxVar);
                            IEnumerator<CodeInstruction> codes = PatchProcessor.GetCurrentInstructions(method, generator: generator).GetEnumerator();
                            while(codes.MoveNext()) {
                                CodeInstruction repeat = codes.Current;
                                if(repeat.opcode == OpCodes.Ret) continue;
                                if(repeat.opcode == OpCodes.Ldarg_0) {
                                    codes.MoveNext();
                                    codes.MoveNext();
                                    if(codes.Current.operand is FieldInfo { Name: "emitter" })
                                        yield return new CodeInstruction(OpCodes.Ldloc, emitter);
                                    else {
                                        yield return repeat;
                                        yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher"));
                                        yield return codes.Current;
                                    }
                                    continue;
                                }
                                if(repeat.opcode == OpCodes.Ldarga_S) repeat = new CodeInstruction(OpCodes.Ldloca, tmpBoxVar);
                                yield return repeat;
                            }
                            yield return new CodeInstruction(OpCodes.Ldloca, enumeratorVar).WithLabels(check);
                            yield return new CodeInstruction(OpCodes.Call, typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator).Method("MoveNext"));
                            yield return new CodeInstruction(OpCodes.Brtrue, start);
                            continue;
                        }
                    } else throw new Exception("This Code Is Not field: " + next.opcode);
                } else if(code.opcode == OpCodes.Ldarg_1) code = new CodeInstruction(OpCodes.Ldloc, fix);
                else if(code.opcode == OpCodes.Ldsfld && code.operand is FieldInfo field && field.FieldType == typeof(Func<ParameterInfo, bool>)) {
                    while(enumerator.MoveNext()) if(enumerator.Current.opcode == OpCodes.Call) break;
                    yield return new CodeInstruction(OpCodes.Call, ((Delegate) CheckArgs).Method);
                    continue;
                }
                switch(state) {
                    case 0:
                        if(code.opcode == OpCodes.Newobj && code.operand is ConstructorInfo consInfo && consInfo.DeclaringType == typeof(List<KeyValuePair<LocalBuilder, Type>>)) {
                            yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(code.labels);
                            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "tryPrefixes"));
                            yield return new CodeInstruction(OpCodes.Ldarg_1);
                            yield return new CodeInstruction(OpCodes.Call, typeof(Enumerable).Methods().First(m => m.Name == "Contains").MakeGenericMethod(typeof(HarmonyLib.Patch)));
                            Label falseLabel = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Brfalse, falseLabel);
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(harmonyAssembly.GetType("HarmonyLib.MethodPatcher"), "il"));
                            yield return new CodeInstruction(OpCodes.Ldtoken, typeof(Exception));
                            yield return new CodeInstruction(OpCodes.Call, typeof(Type).Method("GetTypeFromHandle"));
                            yield return new CodeInstruction(OpCodes.Callvirt, typeof(ILGenerator).Method("DeclareLocal", typeof(Type)));
                            yield return new CodeInstruction(OpCodes.Stloc, exceptionVar);
                            yield return new CodeInstruction(OpCodes.Ldloc, emitter);
                            yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                            yield return new CodeInstruction(OpCodes.Ldnull);
                            yield return new CodeInstruction(OpCodes.Newobj, typeof(ExceptionBlock).Constructor(typeof(ExceptionBlockType), typeof(Type)));
                            yield return new CodeInstruction(OpCodes.Ldloca, notUsingLocal);
                            yield return new CodeInstruction(OpCodes.Callvirt, emitterType.Method("MarkBlockBefore"));
                            yield return new CodeInstruction(OpCodes.Nop).WithLabels(falseLabel);
                            code.labels.Clear();
                            state++;
                        }
                        break;
                    case 1:
                        if(code.opcode == OpCodes.Throw) state++;
                        break;
                    case 2:
                        if((code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt) && code.operand is MethodInfo { Name: "Emit" }) {
                            yield return code;
                            foreach(CodeInstruction instruction in PatchProcessor.GetCurrentInstructions(((Delegate) handleException).Method, generator: generator)) {
                                if(instruction.opcode == OpCodes.Ldloc_0 || instruction.opcode == OpCodes.Ldloc_2 ||
                                   instruction.opcode == OpCodes.Stloc_0 || instruction.opcode == OpCodes.Stloc_2) continue;
                                if(instruction.operand is LocalBuilder) {
                                    if(instruction.opcode == OpCodes.Ldloca_S) instruction.opcode = OpCodes.Ldloc;
                                    instruction.operand = notUsingLocal;
                                }
                                if(instruction.opcode == OpCodes.Ldarg_0) yield return new CodeInstruction(OpCodes.Ldloc, emitter).WithLabels(instruction.labels);
                                else if(instruction.opcode == OpCodes.Ldarg_2) yield return new CodeInstruction(OpCodes.Ldloc, exceptionVar);
                                else if(instruction.opcode == OpCodes.Ldarg_S && (byte) instruction.operand == 4)
                                    yield return new CodeInstruction(OpCodes.Ldstr, "An error occurred while invoking a Prefix Patch ");
                                else if(instruction.operand is MethodInfo info && info.DeclaringType == typeof(JAEmitter)) {
                                    instruction.operand = harmonyAssembly.GetType("HarmonyLib.Emitter").Method(info.Name, info.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
                                    yield return instruction;
                                } else if(instruction.opcode == OpCodes.Ret) yield return new CodeInstruction(OpCodes.Nop).WithLabels(instruction.labels);
                                else yield return instruction;
                            }
                            state++;
                            continue;
                        }
                        break;
                }
                yield return code;
            }
        }
    }

    private static bool AddPostfixes(object _, Dictionary<string, LocalBuilder> variables, LocalBuilder runOriginalVariable, bool passthroughPatches, JAMethodPatcher patcher) {
        bool result = false;
        foreach(HarmonyLib.Patch patch in patcher.postfixes) AddPostfixes_b__0(patcher, patch, variables, runOriginalVariable, passthroughPatches, ref result);
        return result;
    }

    private static void AddPostfixes_b__0(JAMethodPatcher patcher, HarmonyLib.Patch patch, Dictionary<string, LocalBuilder> variables, LocalBuilder runOriginalVariable, bool passthroughPatches, ref bool result) {
        _ = Transpiler(null, null);
        throw new NotImplementedException();

        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            Assembly harmonyAssembly = typeof(Harmony).Assembly;
            Type emitterType = harmonyAssembly.GetType("HarmonyLib.Emitter");
            LocalBuilder fix = generator.DeclareLocal(typeof(MethodInfo));
            CodeInstruction getType = new(OpCodes.Call, typeof(Type).Method("GetTypeFromHandle"));
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(HarmonyLib.Patch), "patchMethod"));
            yield return new CodeInstruction(OpCodes.Stloc, fix);
            LocalBuilder emitter = generator.DeclareLocal(emitterType);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher"));
            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(harmonyAssembly.GetType("HarmonyLib.MethodPatcher"), "emitter"));
            yield return new CodeInstruction(OpCodes.Stloc, emitter);
            using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            LocalBuilder exceptionVar = generator.DeclareLocal(typeof(LocalBuilder));
            LocalBuilder notUsingLocal = generator.DeclareLocal(typeof(Label?));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "tryPostfixes"));
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, typeof(Enumerable).Methods().First(m => m.Name == "Contains").MakeGenericMethod(typeof(HarmonyLib.Patch)));
            Label falseLabel = generator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse, falseLabel);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(harmonyAssembly.GetType("HarmonyLib.MethodPatcher"), "il"));
            yield return new CodeInstruction(OpCodes.Ldtoken, typeof(Exception));
            yield return new CodeInstruction(OpCodes.Call, typeof(Type).Method("GetTypeFromHandle"));
            yield return new CodeInstruction(OpCodes.Callvirt, typeof(ILGenerator).Method("DeclareLocal", typeof(Type)));
            yield return new CodeInstruction(OpCodes.Stloc, exceptionVar);
            yield return new CodeInstruction(OpCodes.Ldloc, emitter);
            yield return new CodeInstruction(OpCodes.Ldc_I4_0);
            yield return new CodeInstruction(OpCodes.Ldnull);
            yield return new CodeInstruction(OpCodes.Newobj, typeof(ExceptionBlock).Constructor(typeof(ExceptionBlockType), typeof(Type)));
            yield return new CodeInstruction(OpCodes.Ldloca, notUsingLocal);
            yield return new CodeInstruction(OpCodes.Callvirt, emitterType.Method("MarkBlockBefore"));
            yield return new CodeInstruction(OpCodes.Nop).WithLabels(falseLabel);
            while(enumerator.MoveNext()) {
                CodeInstruction code = enumerator.Current;
                if(code.opcode == OpCodes.Ldarg_0 && enumerator.MoveNext()) {
                    CodeInstruction next = enumerator.Current;
                    List<CodeInstruction> queue = [];
                    Recheck:
                    if(next.opcode == OpCodes.Ldfld || next.opcode == OpCodes.Ldflda || next.opcode == OpCodes.Stfld) {
                        foreach(CodeInstruction instruction in queue) yield return instruction;
                        FieldInfo field = (FieldInfo) next.operand;
                        if(field == AddPostfixesSubArguments[0]) {
                            CodeInstruction next2 = enumerator.MoveNext() ? enumerator.Current : null;
                            if(next2 != null && next2.opcode == OpCodes.Ldfld && next2.operand is FieldInfo { Name: "emitter" })
                                code = new CodeInstruction(OpCodes.Ldloc, emitter).WithLabels(code.labels).WithBlocks(code.blocks);
                            else {
                                yield return code;
                                yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher"));
                                code = next2;
                            }
                        } else if(field == AddPostfixesSubArguments[1]) code = new CodeInstruction(OpCodes.Ldarg_2).WithLabels(code.labels).WithBlocks(code.blocks);
                        else if(field == AddPostfixesSubArguments[2]) code =new CodeInstruction(OpCodes.Ldarg_3).WithLabels(code.labels).WithBlocks(code.blocks);
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
                            yield return new CodeInstruction(OpCodes.Callvirt, typeof(List<KeyValuePair<LocalBuilder, Type>>).Method("GetEnumerator"));
                            yield return new CodeInstruction(OpCodes.Stloc, enumeratorVar);
                            Label loop = generator.DefineLabel();
                            Label end = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Ldloca, enumeratorVar).WithLabels(loop);
                            yield return new CodeInstruction(OpCodes.Call, typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator).Method("MoveNext"));
                            yield return new CodeInstruction(OpCodes.Brfalse, end);
                            yield return new CodeInstruction(OpCodes.Ldloca, enumeratorVar);
                            yield return new CodeInstruction(OpCodes.Call, typeof(List<KeyValuePair<LocalBuilder, Type>>.Enumerator).Method("get_Current"));
                            yield return new CodeInstruction(OpCodes.Stloc, tmpBoxVar);
                            IEnumerator<CodeInstruction> codes = PatchProcessor.GetCurrentInstructions(method, generator: generator).GetEnumerator();
                            while(codes.MoveNext()) {
                                CodeInstruction repeat = codes.Current;
                                if(repeat.opcode == OpCodes.Ret) continue;
                                if(repeat.opcode == OpCodes.Ldarg_0) {
                                    codes.MoveNext();
                                    codes.MoveNext();
                                    if(codes.Current.operand is FieldInfo { Name: "emitter" })
                                        yield return new CodeInstruction(OpCodes.Ldloc, emitter);
                                    else {
                                        yield return repeat;
                                        yield return new CodeInstruction(OpCodes.Ldfld, SimpleReflect.Field(typeof(JAMethodPatcher), "originalPatcher"));
                                        yield return codes.Current;
                                    }
                                }
                                if(repeat.opcode == OpCodes.Ldarg_S || repeat.opcode == OpCodes.Ldarg_1) repeat = new CodeInstruction(OpCodes.Ldloc, tmpBoxVar);
                                yield return repeat;
                            }
                            yield return new CodeInstruction(OpCodes.Br, loop);
                            yield return new CodeInstruction(OpCodes.Nop).WithLabels(end);
                            foreach(CodeInstruction instruction in PatchProcessor.GetCurrentInstructions(((Delegate) handleException).Method, generator: generator)) {
                                if(instruction.opcode == OpCodes.Ldloc_0 || instruction.opcode == OpCodes.Ldloc_2 ||
                                   instruction.opcode == OpCodes.Stloc_0 || instruction.opcode == OpCodes.Stloc_2) continue;
                                if(instruction.operand is LocalBuilder) {
                                    if(instruction.opcode == OpCodes.Ldloca_S) instruction.opcode = OpCodes.Ldloc;
                                    instruction.operand = notUsingLocal;
                                }
                                if(instruction.opcode == OpCodes.Ldarg_0) yield return new CodeInstruction(OpCodes.Ldloc, emitter).WithLabels(instruction.labels);
                                else if(instruction.opcode == OpCodes.Ldarg_2) yield return new CodeInstruction(OpCodes.Ldloc, exceptionVar);
                                else if(instruction.opcode == OpCodes.Ldarg_S && (byte) instruction.operand == 4)
                                    yield return new CodeInstruction(OpCodes.Ldstr, "An error occurred while invoking a Postfix Patch ");
                                else if(instruction.operand is MethodInfo info && info.DeclaringType == typeof(JAEmitter)) {
                                    instruction.operand = harmonyAssembly.GetType("HarmonyLib.Emitter").Method(info.Name, info.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
                                    yield return instruction;
                                } else if(instruction.opcode == OpCodes.Ret) yield return new CodeInstruction(OpCodes.Nop).WithLabels(instruction.labels);
                                else yield return instruction;
                            }
                            continue;
                        }
                    } else {
                        if(!enumerator.MoveNext()) throw new Exception("This Code Is Not field: " + next.opcode);
                        queue.Add(next);
                        next = enumerator.Current;
                        goto Recheck;
                    }
                } else if(code.opcode == OpCodes.Ldarg_1) code = new CodeInstruction(OpCodes.Ldloc, fix);
                else if(code.opcode == OpCodes.Ldsfld && code.operand is FieldInfo field && field.FieldType == typeof(Func<ParameterInfo, bool>)) {
                    while(enumerator.MoveNext()) if(enumerator.Current.opcode == OpCodes.Call) break;
                    yield return new CodeInstruction(OpCodes.Call, ((Delegate) CheckArgs).Method);
                    continue;
                }
                yield return code;
            }
        }
    }

    private static void handleException(JAEmitter emitter, HarmonyLib.Patch patch, LocalBuilder exceptionVar, LocalBuilder runOriginalVariable, string desc) {
        if(exceptionVar != null) {
            emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock), out _);
            emitter.Emit(OpCodes.Stloc, exceptionVar);
            emitter.Emit(OpCodes.Ldstr, ((TriedPatchData) patch).mod.Name);
            emitter.Emit(OpCodes.Call, typeof(JAMod).Method("GetMods", typeof(string)));
            emitter.Emit(OpCodes.Ldstr, desc + patch.owner);
            emitter.Emit(OpCodes.Ldloc, exceptionVar);
            emitter.Emit(OpCodes.Call, typeof(JAMod).Method("LogException", typeof(string), typeof(Exception)));
            if(patch.PatchMethod.ReturnType == typeof(bool)) {
                emitter.Emit(OpCodes.Ldc_I4_1);
                emitter.Emit(OpCodes.Stloc, runOriginalVariable);
            }
            emitter.MarkBlockAfter(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        }
    }

    private static bool CheckArgs(ParameterInfo[] parameters) {
        foreach(ParameterInfo p in parameters) if(p.Name == "__args") return true;
        return false;
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
            ParameterInfo parameter = originalParameter.FirstOrDefault(info => info.Name == parameterInfo.Name);
            if(parameter != null) {
                if(parameter.ParameterType != parameterInfo.ParameterType) throw new PatchParameterException("Parameter type mismatch: " + parameterInfo.Name);
                _parameterMap[parameterInfo.Position] = parameter.Position;
                continue;
            }
            if(!customReverse) throw new PatchParameterException("Unknown Parameter: " + parameterInfo.Name);
        }
    }

    private static IEnumerable<CodeInstruction> ChangeParameter(IEnumerable<CodeInstruction> instructions) {
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