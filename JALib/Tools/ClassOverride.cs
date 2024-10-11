using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JALib.Core;
using JALib.Core.Patch;
using JALib.JAException;

namespace JALib.Tools;

public class ClassOverride {
    public static void Override(Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) {
        Type baseType = type.BaseType;
        if(Type.GetType($"JALib.ClassOverride.{type.FullName}") != null) throw new AlreadyWorkedException("This class has already been overridden.");
        List<(MethodInfo, MethodInfo)> overrides = [];
        overrides.AddRange(from method in type.GetMethods() where !method.IsStatic && method.GetBaseDefinition() == null && method.DeclaringType == type
                           let originalMethod = baseType.GetMethod(method.Name, flags, null, method.GetParameters().Select(p => p.ParameterType).ToArray(), null)
                           where originalMethod != null && originalMethod.IsPublic == method.IsPublic && originalMethod.ReturnType.IsAssignableFrom(method.ReturnType) select (originalMethod, method));
        if(overrides.Count == 0) return;
        TypeBuilder typeBuilder = JAMod.ModuleBuilder.DefineType($"JALib.ClassOverride.{type.FullName}", TypeAttributes.NotPublic);
        foreach((MethodInfo originalMethod, MethodInfo replaceMethod) in overrides) {
            FieldBuilder fieldBuilder = originalMethod.IsPublic ? null : typeBuilder.DefineField($"OriginalMethod_{originalMethod.GetHashCode()}",
                                            typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static);
            Type[] parameterTypes = new Type[originalMethod.GetParameters().Length + (originalMethod.ReturnType == typeof(void) ? 1 : 2)];
            parameterTypes[0] = originalMethod.DeclaringType;
            foreach(ParameterInfo parameter in originalMethod.GetParameters()) parameterTypes[parameter.Position + 1] = parameter.ParameterType;
            bool needsReturn = originalMethod.ReturnType != typeof(void);
            if(needsReturn) parameterTypes[parameterTypes.Length - 1] = originalMethod.ReturnType.MakeByRefType();
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(originalMethod.Name, MethodAttributes.Public | MethodAttributes.Static,
                typeof(bool), parameterTypes);
            methodBuilder.DefineParameter(1, ParameterAttributes.None, "__instance");
            foreach(ParameterInfo parameter in originalMethod.GetParameters()) methodBuilder.DefineParameter(parameter.Position + 2, parameter.Attributes, parameter.Name);
            if(originalMethod.ReturnType != typeof(void)) methodBuilder.DefineParameter(parameterTypes.Length, ParameterAttributes.None, "__result");
            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            LocalBuilder newObj = ilGenerator.DeclareLocal(type);
            Label falseLabel = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Isinst, type);
            ilGenerator.Emit(OpCodes.Stloc, newObj);
            ilGenerator.Emit(OpCodes.Ldloc, newObj);
            ilGenerator.Emit(OpCodes.Brfalse_S, falseLabel);
            if(needsReturn) ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Length - 1);
            if(fieldBuilder == null) {
                ilGenerator.Emit(OpCodes.Ldloc, newObj);
                for(int i = 0; i < originalMethod.GetParameters().Length; i++) ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                ilGenerator.Emit(OpCodes.Callvirt, replaceMethod);
            } else {
                ilGenerator.Emit(OpCodes.Ldsfld, fieldBuilder);
                ilGenerator.Emit(OpCodes.Ldloc, newObj);
                ilGenerator.Emit(OpCodes.Ldc_I4, originalMethod.GetParameters().Length);
                ilGenerator.Emit(OpCodes.Newarr, typeof(object));
                for(int i = 0; i < originalMethod.GetParameters().Length; i++) {
                    ilGenerator.Emit(OpCodes.Dup);
                    ilGenerator.Emit(OpCodes.Ldc_I4, i);
                    ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                    if(parameterTypes[i + 1].IsValueType) ilGenerator.Emit(OpCodes.Box, parameterTypes[i + 1]);
                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }
                ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodInfo).GetMethod("Invoke", [typeof(object), typeof(object[])]));
                if(!needsReturn) ilGenerator.Emit(OpCodes.Pop);
                else if(originalMethod.ReturnType.IsValueType) ilGenerator.Emit(OpCodes.Unbox_Any, originalMethod.ReturnType);
                else ilGenerator.Emit(OpCodes.Castclass, originalMethod.ReturnType);
            }
            if(needsReturn) ilGenerator.Emit(OpCodes.Stind_Ref);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Ret);
            ilGenerator.MarkLabel(falseLabel);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Ret);
            CustomAttributeBuilder attributeBuilder = new(typeof(JAPatchAttribute).Constructor(typeof(MethodInfo), typeof(PatchType), typeof(bool)),
                [ originalMethod, PatchType.Prefix, false ]);
            methodBuilder.SetCustomAttribute(attributeBuilder);
            Type patchType = typeBuilder.CreateType();
            JALib.Patcher.AddPatch(patchType);
        }
    }
}