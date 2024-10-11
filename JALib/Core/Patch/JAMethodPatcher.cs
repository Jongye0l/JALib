using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;
using MonoMod.Utils;

namespace JALib.Core.Patch;

class JAMethodPatcher {
    private readonly bool debug;
    private readonly JAEmitter emitter;
    private readonly List<MethodInfo> finalizers;
    private readonly int idx;
    private readonly ILGenerator il;
    private readonly MethodBase original;
    private readonly object originalPatcher;
    private readonly List<MethodInfo> postfixes;
    private List<MethodInfo> prefixes;
    private readonly List<MethodInfo> removes;
    private readonly List<MethodInfo> replaces;
    private readonly Type returnType;
    private readonly MethodBase source;
    private readonly List<MethodInfo> transpilers;
    private readonly bool useStructReturnBuffer;

    public JAMethodPatcher(MethodBase original, MethodBase source, PatchInfo patchInfo, JAPatchInfo jaPatchInfo) {
        this.original = original;
        this.source = source;
        debug = patchInfo.Debugging || Harmony.DEBUG;
        prefixes = SortPatchMethods(original, patchInfo.prefixes, debug);
        postfixes = SortPatchMethods(original, patchInfo.postfixes, debug);
        transpilers = SortPatchMethods(original, patchInfo.transpilers, debug);
        finalizers = SortPatchMethods(original, patchInfo.finalizers, debug);
        replaces = SortPatchMethods(original, jaPatchInfo.replaces, debug);
        removes = SortPatchMethods(original, jaPatchInfo.removes, debug);
        SetupPrefixRemove();
        originalPatcher = Type.GetType("HarmonyLib.MethodPatcher").New(original, source, prefixes, postfixes, transpilers, finalizers, debug);
        il = originalPatcher.GetValue<ILGenerator>("il");
        idx = prefixes.Count + postfixes.Count + finalizers.Count;
        emitter = new JAEmitter(originalPatcher.GetValue("emitter"));
        useStructReturnBuffer = originalPatcher.GetValue<bool>("useStructReturnBuffer");
        returnType = original is MethodInfo info ? info.ReturnType : typeof(void);
    }

    private void SetupPrefixRemove() {
        bool a = false;
        prefixes = prefixes.Where(pre => {
            if(a) return false;
            if(removes.Contains(pre)) a = true;
            return true;
        }).ToList();
    }

    private static List<MethodInfo> SortPatchMethods(MethodBase original, HarmonyLib.Patch[] patches, bool debug) =>
        Type.GetType("HarmonyLib.PatchSorter").New(patches, debug).Invoke<List<MethodInfo>>("Sort", original);

    internal MethodInfo CreateReplacement(out Dictionary<int, CodeInstruction> finalInstructions) {
        Type methodPatcher = Type.GetType("HarmonyLib.MethodPatcher");
        LocalBuilder[] originalVariables = removes.Count > 0 ? [] : methodPatcher.Invoke<LocalBuilder[]>("DeclareLocalVariables", il, replaces.Count > 0 ? replaces.Last() : source ?? original);
        Dictionary<string, LocalBuilder> privateVars = new();
        List<MethodInfo> fixes = removes.Count == 0 ? prefixes.Union(postfixes).Union(finalizers).ToList() : prefixes;
        LocalBuilder resultVariable = null;
        if(idx > 0) {
            resultVariable = DeclareLocalVariable(returnType);
            privateVars["__result"] = resultVariable;
        }
        if(fixes.Any(fix => fix.GetParameters().Any(p => p.Name == "__args"))) {
            originalPatcher.Invoke("PrepareArgumentArray");
            LocalBuilder local2 = il.DeclareLocal(typeof(object[]));
            emitter.Emit(OpCodes.Stloc, local2);
            privateVars["__args"] = local2;
        }
        Label? skipOriginalLabel = null;
        LocalBuilder runOriginalVariable = null;
        bool prefixAffectsOriginal = prefixes.Any(fix => methodPatcher.Invoke<bool>("PrefixAffectsOriginal", fix));
        bool anyFixHasRunOriginalVar = fixes.Any(fix => fix.GetParameters().Any(p => p.Name == "__runOriginal"));
        if(prefixAffectsOriginal || anyFixHasRunOriginalVar) {
            runOriginalVariable = DeclareLocalVariable(typeof(bool));
            emitter.Emit(OpCodes.Ldc_I4_1);
            emitter.Emit(OpCodes.Stloc, runOriginalVariable);
            if(prefixAffectsOriginal) skipOriginalLabel = il.DefineLabel();
        }
        fixes.ForEach(fix => {
            if(fix.DeclaringType == null || privateVars.ContainsKey(fix.DeclaringType.AssemblyQualifiedName)) return;
            fix.GetParameters().Where(patchParam => patchParam.Name == "__state")
                .Do(patchParam => privateVars[fix.DeclaringType.AssemblyQualifiedName] =
                                      DeclareLocalVariable(patchParam.ParameterType));
        });
        LocalBuilder finalizedVariable = null;
        if(finalizers.Count > 0) {
            finalizedVariable = DeclareLocalVariable(typeof(bool));
            privateVars["__exception"] = DeclareLocalVariable(typeof(Exception));
            emitter.MarkBlockBefore(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        }
        originalPatcher.Invoke("AddPrefixes", privateVars, runOriginalVariable);
        if(skipOriginalLabel.HasValue) {
            emitter.Emit(OpCodes.Ldloc, runOriginalVariable);
            emitter.Emit(OpCodes.Brfalse, skipOriginalLabel.Value);
        }
        bool needsToStorePassthroughResult = false;
        bool hasReturnCode = false;
        if(removes.Count == 0) {
            JAMethodCopier copier = new(replaces.Count > 0 ? replaces.Last() : source ?? original, il, originalVariables);
            copier.SetArgumentShift(useStructReturnBuffer);
            copier.SetDebugging(debug);
            copier.AddTranspiler(transpilers);
            List<Label> endLabels = [];
            copier.Finalize(emitter, endLabels, out hasReturnCode);
            foreach(Label label in endLabels) emitter.MarkLabel(label);
            if(resultVariable != null & hasReturnCode) emitter.Emit(OpCodes.Stloc, resultVariable);
            if(skipOriginalLabel.HasValue) emitter.MarkLabel(skipOriginalLabel.Value);
            originalPatcher.Invoke("AddPostfixes", privateVars, runOriginalVariable, false);
            if(resultVariable != null & hasReturnCode)
                emitter.Emit(OpCodes.Ldloc, resultVariable);
            needsToStorePassthroughResult = originalPatcher.Invoke<bool>("AddPostfixes", privateVars, runOriginalVariable, true);
        }
        bool hasFinalizers = finalizers.Any();
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
        }
        if(resultVariable != null) emitter.Emit(OpCodes.Ldloc, resultVariable);
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
        Type.GetType("HarmonyLib.DetourHelper").Method("Pin").MakeGenericMethod(typeof(MethodInfo)).Invoke([replacement]);
        return replacement;
    }

    private LocalBuilder DeclareLocalVariable(Type type) {
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
        LocalBuilder local1 = il.DeclareLocal(type);
        if(type == typeof(float)) emitter.Emit(OpCodes.Ldc_R4, 0.0f);
        else if(type == typeof(double)) emitter.Emit(OpCodes.Ldc_R8, 0.0);
        else if(type == typeof(long) || type == typeof(ulong)) emitter.Emit(OpCodes.Ldc_I8, 0L);
        else emitter.Emit(OpCodes.Ldc_I4, 0);
        emitter.Emit(OpCodes.Stloc, local1);
        return local1;
    }
}