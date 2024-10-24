﻿using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Tools;

namespace JALib.Core.Patch.ILTools.Calculate;

public class ILDiv : ILCalculate {

    public ILDiv(ILCode left, ILCode right) : base(left, right) {
        if(!left.ReturnType.IsNumeric()) throw new InvalidProgramException("left Type is not numeric");
        if(!right.ReturnType.IsNumeric()) throw new InvalidProgramException("right Type is not numeric");
    }

    public override IEnumerable<CodeInstruction> Load(ILGenerator generator) {
        foreach(CodeInstruction instruction in Left.Load(generator)) yield return instruction;
        foreach(CodeInstruction instruction in Right.Load(generator)) yield return instruction;
        yield return new CodeInstruction(ReturnType.IsUnsigned() ? OpCodes.Div_Un : OpCodes.Div);
    }

    public override string ToString() => $"{Left} / {Right}";
}