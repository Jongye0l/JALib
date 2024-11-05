﻿using System.Reflection;

namespace JALib.Core.Patch;

public class OverridePatchData {
    public Type targetType;
    public MethodInfo patchMethod;
    public bool IgnoreBasePatch;
    public bool debug;
    public bool tryCatch;
    public string id;
    public JAMod mod;
}