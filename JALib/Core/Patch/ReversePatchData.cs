﻿using System.Reflection;
using HarmonyLib;

namespace JALib.Core.Patch;

public class ReversePatchData {
    public MethodBase original;
    public HarmonyMethod patchMethod;
    public JAReversePatchAttribute attribute;
    public JAMod mod;
}