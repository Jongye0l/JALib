using System;
using System.Collections.Generic;
using UnityModManagerNet;

namespace JALib.Bootstrap;

public class JAModInfo {
    public string AssemblyPath;
    public string ClassName;
    public bool AssemblyRequireModPath;
    public string DependencyPath;
    public bool DependencyRequireModPath;
    public int BootstrapVersion;
    public UnityModManager.ModEntry ModEntry;
    public bool IsBetaBranch;
    public Dictionary<string, string> Dependencies;
}