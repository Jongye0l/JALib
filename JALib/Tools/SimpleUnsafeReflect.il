.class public abstract sealed auto ansi beforefieldinit JALib.Tools.SimpleUnsafeReflect extends [mscorlib]System.Object {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

    .method public hidebysig static !!T GetValueUnsafe<class T>(class [mscorlib]System.Reflection.FieldInfo 'field', [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [2] = nullref
        .maxstack 2
        ldarg.0
        ldarg.1
        callvirt instance object [mscorlib]System.Reflection.FieldInfo::GetValue(object)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Reflection.FieldInfo 'field', [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [2] = nullref
        .maxstack 2
        ldarg.0
        ldarg.1
        callvirt instance object [mscorlib]System.Reflection.FieldInfo::GetValue(object)
        unbox !!T
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(class [mscorlib]System.Reflection.MethodInfo methodInfo, [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [2] = nullref
        .maxstack 3
        ldarg.0
        ldarg.1
        call object[] [mscorlib]System.Array::Empty<object>()
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Reflection.MethodInfo methodInfo, [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [2] = nullref
        .maxstack 3
        ldarg.0
        ldarg.1
        call object[] [mscorlib]System.Array::Empty<object>()
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        unbox !!T
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(class [mscorlib]System.Reflection.MethodInfo methodInfo, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [2]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldnull
        ldarg.1
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Reflection.MethodInfo methodInfo, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [2]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldnull
        ldarg.1
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        unbox !!T
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(class [mscorlib]System.Reflection.MethodInfo methodInfo, object obj, object[] parameters) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Reflection.MethodInfo methodInfo, object obj, object[] parameters) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        unbox !!T
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(class [mscorlib]System.Type 'type', string name) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string)
        ldnull
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Type 'type', string name) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string)
        ldnull
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(class [mscorlib]System.Type 'type', string name, object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string)
        ldarg.2
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Type 'type', string name, object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string)
        ldarg.2
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(class [mscorlib]System.Type 'type', string name, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string)
        ldnull
        ldarg.2
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Type 'type', string name, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string)
        ldnull
        ldarg.2
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        unbox !!T
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(class [mscorlib]System.Type 'type', class [mscorlib]System.Type[] types, string name, [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [4] = nullref
        .maxstack 3
        ldarg.0
        ldarg.2
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string, class [mscorlib]System.Type[])
        ldarg.3
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Type 'type', class [mscorlib]System.Type[] types, string name, [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [4] = nullref
        .maxstack 3
        ldarg.0
        ldarg.2
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string, class [mscorlib]System.Type[])
        ldarg.3
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(class [mscorlib]System.Type 'type', class [mscorlib]System.Type[] types, string name, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [4]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.2
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string, class [mscorlib]System.Type[])
        ldnull
        ldarg.3
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Type 'type', class [mscorlib]System.Type[] types, string name, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [4]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.2
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(class [mscorlib]System.Type, string, class [mscorlib]System.Type[])
        ldnull
        ldarg.3
        callvirt instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        unbox !!T
        ret
    }

    .method public hidebysig static !!T GetValueUnsafe<class T>(class [mscorlib]System.Type 'type', string name, [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3] = nullref
        .maxstack 2
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.FieldInfo [JALib]JALib.Tools.SimpleReflect::Field(class [mscorlib]System.Type, string)
        ldarg.2
        callvirt instance object [mscorlib]System.Reflection.FieldInfo::GetValue(object)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Type 'type', string name, [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3] = nullref
        .maxstack 2
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.FieldInfo [JALib]JALib.Tools.SimpleReflect::Field(class [mscorlib]System.Type, string)
        ldarg.2
        callvirt instance object [mscorlib]System.Reflection.FieldInfo::GetValue(object)
        unbox !!T
        ret
    }

    .method public hidebysig static !!T NewUnsafe<class T>(class [mscorlib]System.Type 'type') cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1
        ldarg.0
        call object JALib.Tools.SimpleUnsafeReflect::NewUnsafe(class [mscorlib]System.Type)
        ret
    }

    .method public hidebysig static !!T NewUnsafe<class T>(class [mscorlib]System.Type 'type', object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [2]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldarg.0
        ldarg.1
        call object JALib.Tools.SimpleUnsafeReflect::NewUnsafe(class [mscorlib]System.Type, object[])
        ret
    }

    .method public hidebysig static !!T GetValueUnsafe<class T>(object obj, string name) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldarg.0
        ldarg.1
        call object JALib.Tools.SimpleUnsafeReflect::GetValueUnsafe(object, string)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldarg.0
        ldarg.1
        call object JALib.Tools.SimpleUnsafeReflect::GetValueUnsafe(object, string)
        unbox !!T
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(object obj, string name) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        call !!0[] [mscorlib]System.Array::Empty<class [mscorlib]System.Type>()
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.0
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        call !!0[] [mscorlib]System.Array::Empty<class [mscorlib]System.Type>()
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.0
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(object obj, string name, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string)
        ldarg.0
        ldarg.2
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string)
        ldarg.0
        ldarg.2
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T InvokeUnsafe<class T>(object obj, string name, class [mscorlib]System.Type[] types, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [4]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.0
        ldarg.3
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T InvokeUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name, class [mscorlib]System.Type[] types, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [4]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.0
        ldarg.3
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T GetValueUnsafe<class T>(object obj, string name, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string)
        ldarg.0
        ldarg.2
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [3]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string)
        ldarg.0
        ldarg.2
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T GetValueUnsafe<class T>(object obj, string name, object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string)
        ldarg.2
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name, object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldarg.0
        ldarg.1
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string)
        ldarg.2
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafe<class T>(object obj, string name, class [mscorlib]System.Type[] types, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [4]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.0
        ldarg.3
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name, class [mscorlib]System.Type[] types, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [4]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.0
        ldarg.3
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T GetValueUnsafe<class T>(object obj, string name, class [mscorlib]System.Type[] types, object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.3
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name, class [mscorlib]System.Type[] types, object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.3
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafe<class T>(object obj, string name, class [mscorlib]System.Type[] types) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldnull
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name, class [mscorlib]System.Type[] types) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldnull
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafe<class T>(object obj, string name, class [mscorlib]System.Type[] types, object o, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [5]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.3
        ldarg.s objects
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafe<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object obj, string name, class [mscorlib]System.Type[] types, object o, object[] objects) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [5]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        call class [mscorlib]System.Reflection.MethodInfo [JALib]JALib.Tools.SimpleReflect::Method(object, string, class [mscorlib]System.Type[])
        ldarg.3
        ldarg.s objects
        call !!T JALib.Tools.SimpleUnsafeReflect::InvokeUnsafeValue<!!T>(class [mscorlib]System.Reflection.MethodInfo, object, object[])
        ret
    }

    .method public hidebysig static !!T GetValueUnsafe<class T>(class [mscorlib]System.Reflection.PropertyInfo 'property', [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [2] = nullref
        .maxstack 2
        ldarg.0
        ldarg.1
        callvirt instance object [mscorlib]System.Reflection.PropertyInfo::GetValue(object)
        ret
    }

    .method public hidebysig static !!T GetValueUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(class [mscorlib]System.Reflection.PropertyInfo 'property', [opt] object o) cil managed {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .param [2] = nullref
        .maxstack 2
        ldarg.0
        ldarg.1
        callvirt instance object [mscorlib]System.Reflection.PropertyInfo::GetValue(object)
        unbox !!T
        ret
    }

    .method public hidebysig static !!T NewUnsafe<class T>() cil managed {
        .maxstack 1
        ldtoken !!T
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        call !!T JALib.Tools.SimpleUnsafeReflect::NewUnsafe<!!T>(class [mscorlib]System.Type)
        ret
    }

    .method public hidebysig static !!T NewUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>() cil managed {
        .maxstack 1
        ldtoken !!T
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        call !!T JALib.Tools.SimpleUnsafeReflect::NewUnsafeValue<!!T>(class [mscorlib]System.Type)
        ret
    }

    .method public hidebysig static !!T NewUnsafe<class T>(object[] objects) cil managed {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldtoken !!T
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ldarg.0
        call !!T [JALib]JALib.Tools.SimpleUnsafeReflect::NewUnsafe<!!T>(class [mscorlib]System.Type, object[])
        ret
    }

    .method public hidebysig static !!T NewUnsafeValue<valuetype .ctor (class [mscorlib]System.ValueType) T>(object[] objects) cil managed {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2
        ldtoken !!T
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ldarg.0
        call !!T [JALib]JALib.Tools.SimpleUnsafeReflect::NewUnsafeValue<!!T>(class [mscorlib]System.Type, object[])
        ret
    }
}
