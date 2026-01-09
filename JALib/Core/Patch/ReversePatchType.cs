namespace JALib.Core.Patch;

[Flags]
public enum ReversePatchType {
    Original = 0,
    PrefixCombine = 1,
    PostfixCombine = 2,
    TranspilerCombine = 4,
    FinalizerCombine = 8,
    ReplaceCombine = 16,
    OverrideCombine = 32,
    ReplaceTranspilerCombine = 64,
    AllCombine = PrefixCombine | PostfixCombine | TranspilerCombine | FinalizerCombine | ReplaceCombine | OverrideCombine | ReplaceTranspilerCombine,
    DontUpdate = 0x40000000
}
