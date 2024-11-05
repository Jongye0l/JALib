namespace JALib.Core.Patch;

[Flags]
public enum PatchBinding {
    None = 0,
    Prefix = 1,
    Postfix = 2,
    Transpiler = 4,
    Finalizer = 8,
    Replace = 16,
    AllNormalPatch = Prefix | Postfix | Transpiler | Finalizer | Replace,
    Reverse = 32,
    Override = 64,
    AllPatch = AllNormalPatch | Reverse | Override
}