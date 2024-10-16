namespace JALib.Core.Patch;

public enum ReversePatchType {
    Original = 0,
    PrefixCombine = 1,
    PostfixCombine = 2,
    TranspilerCombine = 4,
    FinalizerCombine = 8,
    ReplaceCombine = 16,
    DontUpdate = 32
}
