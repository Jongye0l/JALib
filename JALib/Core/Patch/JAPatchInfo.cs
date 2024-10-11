using System.Linq;
using HarmonyLib;

namespace JALib.Core.Patch;

class JAPatchInfo {
    public HarmonyLib.Patch[] replaces = [];
    public HarmonyLib.Patch[] removes = [];

    public void AddReplaces(string owner, HarmonyMethod methods) {
        replaces = Add(owner, methods, replaces);
    }

    public void AddRemoves(string owner, HarmonyMethod methods) {
        removes = Add(owner, methods, removes);
    }

    public static HarmonyLib.Patch[] Add(string owner, HarmonyMethod add, HarmonyLib.Patch[] current) {
        int initialIndex = current.Length;
        return current.Concat([new HarmonyLib.Patch(add, initialIndex, owner)]).ToArray();
    }

    public bool HasData() {
        return replaces.Length > 0 || removes.Length > 0;
    }
}