using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch;

class JAMethodCopier {
    private readonly object original;
    private List<MethodInfo> transpilers;

    public JAMethodCopier(MethodBase fromMethod, ILGenerator toILGenerator, LocalBuilder[] existingVariables = null) {
        original = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodCopier").New(fromMethod, toILGenerator, existingVariables);
        transpilers = original.GetValue<List<MethodInfo>>("transpilers");
    }

    public void SetArgumentShift(bool useShift) => original.Invoke("SetArgumentShift", useShift);
    public void SetDebugging(bool debug) => original.Invoke("SetDebugging", debug);
    public void AddTranspiler(List<MethodInfo> transpiler) => transpilers.AddRange(transpiler);
    public void Finalize(JAEmitter emitter, List<Label> endLabels, out bool hasReturnCode) {
        object[] args = [emitter.GetOriginal(), endLabels, false];
        original.Invoke("Finalize", [typeof(Harmony).Assembly.GetType("HarmonyLib.Emitter"), typeof(List<Label>), typeof(bool).MakeByRefType()], args);
        hasReturnCode = (bool) args[2];
    }
}