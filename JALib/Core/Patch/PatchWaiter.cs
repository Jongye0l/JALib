using System.Collections.Generic;
using System.Reflection;

namespace JALib.Core.Patch;

class PatchWaiter {
    public readonly HashSet<MethodBase> NormalPatches = [];
    public readonly HashSet<ReversePatchData> ReversePatches = [];
    public readonly HashSet<JAPatcher> PendingPatcher = [];

    public void AddNormalPatch(MethodBase method) {
        NormalPatches.Add(method);
    }

    public void AddReversePatch(ReversePatchData patchData) {
        ReversePatches.Add(patchData);
    }
    
    public void AddPatcher(JAPatcher patcher) {
        PendingPatcher.Add(patcher);
    }
}