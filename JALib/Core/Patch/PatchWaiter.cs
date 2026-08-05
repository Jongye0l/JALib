using System.Collections.Generic;
using System.Reflection;

namespace JALib.Core.Patch;

class PatchWaiter {
    public readonly HashSet<MethodBase> NormalPatches = [];
    public readonly HashSet<ReversePatchData> ReversePatches = [];
    public readonly HashSet<JAPatcher> PendingPatcher = [];
    public int State = 0;

    public void AddNormalPatch(MethodBase method) {
        NormalPatches.Add(method);
    }

    public void AddReversePatch(ReversePatchData patchData) {
        ReversePatches.Add(patchData);
    }
    
    public void AddPatcher(JAPatcher patcher) {
        PendingPatcher.Add(patcher);
    }

    public void RemoveFrom(PatchWaiter waiter) {
        foreach(MethodBase method in waiter.NormalPatches) NormalPatches.Remove(method);
        foreach(ReversePatchData patchData in waiter.ReversePatches) waiter.ReversePatches.Remove(patchData);
    }

    public void RunWaiterPatchForce() {
        JAPatcher.RunWaiterPatchForce0(this);
    }
}