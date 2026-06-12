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
    ILManipulateCombine = 128,
    AllInsidePatchCombine = TranspilerCombine | ReplaceCombine | ReplaceTranspilerCombine | ILManipulateCombine,
    AllCombine = PrefixCombine | PostfixCombine | FinalizerCombine | OverrideCombine | AllInsidePatchCombine,
    DontUpdate = 0x40000000
}
