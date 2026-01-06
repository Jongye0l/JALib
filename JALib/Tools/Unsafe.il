.class public abstract sealed auto ansi beforefieldinit JALib.Tools.Unsafe extends [mscorlib]System.Object {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

    .method public hidebysig static !!T AsUnsafe<class T>(object o) cil managed aggressiveinlining {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1
        ldarg.0
        ret
    }

    .method public hidebysig static !!TTo AsUnsafe<valuetype .ctor (class [mscorlib]System.ValueType) TFrom, valuetype .ctor (class [mscorlib]System.ValueType) TTo>(!!TFrom o) cil managed aggressiveinlining {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1
        ldarg.0
        ret
    }

    .method public hidebysig static !!T& UnboxUnsafe<valuetype .ctor (class [mscorlib]System.ValueType) T> (object 'box') cil managed aggressiveinlining {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1
        ldarg.0
        unbox !!T
        ret
    }

    .method public hidebysig static native int AsPointer(object o) cil managed aggressiveinlining {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1
        ldarg.0
        conv.i
        ret
    }

    .method public hidebysig static native unsigned int AsUnsignedPointer(object o) cil managed aggressiveinlining {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1
        ldarg.0
        conv.u
        ret
    }
}