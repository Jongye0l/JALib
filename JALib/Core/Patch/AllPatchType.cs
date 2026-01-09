namespace JALib.Core.Patch;

[Flags]
public enum AllPatchType {
    Prefix = 1,
    Postfix = 2,
    Transpiler = 4,
    Finalizer = 8,
    TryPrefix = 16,
    TryPostfix = 32,
    Remove = 64,
    Replace = 128,
    Reverse = 256,
    Override = 512,
    ReplaceTranspiler = 1024,
    AllPrefix = Prefix | TryPrefix | Remove | Override,
    AllPostfix = Postfix | TryPostfix,
    AllTranspiler = Transpiler | Replace | ReplaceTranspiler,
    All = AllPrefix | AllPostfix | AllTranspiler | Finalizer | Reverse
}