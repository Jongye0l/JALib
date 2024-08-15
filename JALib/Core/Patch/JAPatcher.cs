using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ADOFAI;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch;

public class JAPatcher : IDisposable {

    private List<JAPatchAttribute> patchData;
    private JAMod mod;
    public event FailPatch OnFailPatch;
    public delegate void FailPatch(string patchId);
    
    public JAPatcher(JAMod mod) {
        this.mod = mod;
        patchData = new List<JAPatchAttribute>();
    }
    
    public void Patch() {
        foreach(JAPatchAttribute attribute in patchData) {
            if(attribute.Patch != null) {
                mod.Error($"Mod {mod.Name} Id {attribute.PatchId} Patch Failed : Already Patched");
                continue;
            }
            try {
                if(attribute.MinVersion > GCNS.releaseNumber || attribute.MaxVersion < GCNS.releaseNumber) continue;
                if(attribute.MethodBase == null) {
                    attribute.ClassType ??= Type.GetType(attribute.Class);
                    if(attribute.ArgumentTypesType == null && attribute.ArgumentTypes != null) {
                        attribute.ArgumentTypesType = new Type[attribute.ArgumentTypes.Length];
                        for(int i = 0; i < attribute.ArgumentTypes.Length; i++)
                            attribute.ArgumentTypesType[i] = Type.GetType(attribute.ArgumentTypes[i]);
                    }
                    if(attribute.MethodName == ".ctor")
                        attribute.MethodBase = attribute.ArgumentTypesType == null ?
                            attribute.ClassType.Constructor() : attribute.ClassType.Constructor(attribute.ArgumentTypesType);
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
                    else attribute.MethodBase = attribute.ArgumentTypesType == null ?
                            attribute.ClassType.Method(attribute.MethodName) : attribute.ClassType.Method(attribute.MethodName, attribute.ArgumentTypesType);
                    if(attribute.GenericType != null || attribute.GenericName != null) {
                        attribute.GenericType ??= Type.GetType(attribute.GenericName);
                        attribute.MethodBase = ((MethodInfo) attribute.MethodBase).MakeGenericMethod(attribute.GenericType);
                    }
                }
                attribute.HarmonyMethod ??= new HarmonyMethod(attribute.Method);
                attribute.Patch = JALib.Harmony.Patch(attribute.MethodBase,
                    attribute.PatchType == PatchType.Prefix ? attribute.HarmonyMethod : null,
                    attribute.PatchType == PatchType.Postfix ? attribute.HarmonyMethod : null,
                    attribute.PatchType == PatchType.Transpiler ? attribute.HarmonyMethod : null,
                    attribute.PatchType == PatchType.Finalizer ? attribute.HarmonyMethod : null);
            } catch (Exception e) {
                mod.Error($"Mod {mod.Name} Id {attribute.PatchId} Patch Failed");
                mod.LogException(e);
                OnFailPatch?.Invoke(attribute.PatchId);
                if(!attribute.Disable) continue;
                mod.Error($"Mod {mod.Name} is Disabled.");
                Unpatch();
                break;
            }
        }
    }
    
    public void Unpatch() {
        foreach(JAPatchAttribute patchData in patchData.Where(patchData => patchData.Patch != null)) {
            JALib.Harmony.Unpatch(patchData.MethodBase, patchData.Patch);
            patchData.Patch = null;
        }
    }
    
    public void Unpatch(string patchId) {
        foreach(JAPatchAttribute patchData in patchData.Where(patchData => patchData.Patch != null && patchData.PatchId == patchId)) {
            JALib.Harmony.Unpatch(patchData.MethodBase, patchData.Patch);
            patchData.Patch = null;
        }
    }

    public JAPatcher AddPatch(Type type) {
        foreach(MethodInfo method in type.Methods()) AddPatch(method);
        return this;
    }

    public JAPatcher AddPatch(MethodInfo method) {
        foreach(JAPatchAttribute attribute in method.GetCustomAttributes<JAPatchAttribute>()) {
            attribute.Method = method;
            patchData.Add(attribute);
        }
        return this;
    }

    public JAPatcher AddPatch(Delegate @delegate) {
        return AddPatch(@delegate.Method);
    }

    public void Dispose() {
        Unpatch();
        GC.SuppressFinalize(patchData);
        GC.SuppressFinalize(this);
    }
}