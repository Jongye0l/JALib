.class public abstract sealed auto ansi JALib.Tools.VersionControl extends [mscorlib]System.Object {
    .field public static int32 releaseNumber
    .field public static class [mscorlib]System.Version version
    .method private hidebysig static specialname rtspecialname void .cctor() cil managed {
        .maxstack 2
        ldsfld int32 ['Assembly-CSharp']GCNS::releaseNumber
        stsfld int32 JALib.Tools.VersionControl::releaseNumber

        call string [UnityEngine.CoreModule]UnityEngine.Application::get_version()
        ldsflda class [mscorlib]System.Version JALib.Tools.VersionControl::version
        call bool [mscorlib]System.Version::TryParse(string, class [mscorlib]System.Version&)
        pop
        ret
    }
}
