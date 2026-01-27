using System.IO;
using System.Linq;
using TinyJson;
using UnityEngine;
using UnityModManagerNet;

namespace JALib.Tools;

[Obsolete("Deprecated. Use ModTools.ApplyMod instead.", true)]
public static class ForceApplyMod {
    [Obsolete("Deprecated. Use ModTools.ApplyMod instead.", true)]
    public static void ApplyMod(string path) => ModTools.ApplyMod(JALib.Instance, path);
}