using System.Collections.Generic;
using System.Linq;
using JALib.Core.Patch;

namespace JALib.Core;

public abstract class MultiFeaturePatch {
    public readonly JAPatcher Patcher;
    public readonly JAMod Mod;
    private HashSet<Feature> _enabledFeatures = [];
    
    protected MultiFeaturePatch(JAMod mod) {
        Patcher = new JAPatcher(mod);
        Patcher.OnFailPatch += OnFailPatch;
        Mod = mod;
    }

    internal static MultiFeaturePatch GetMultiFeaturePatch(JAMod mod, Type type) {
        if(mod._multiFeaturePatches.TryGetValue(type, out MultiFeaturePatch patch)) return patch;
        if(type.IsSubclassOf(typeof(MultiFeaturePatch))) patch = (MultiFeaturePatch) Activator.CreateInstance(type);
        else patch = new DefaultTypeMultiPatch(mod, type);
        mod._multiFeaturePatches[type] = patch;
        return patch;
    }

    public void Patch(Feature feature) {
        _enabledFeatures.Add(feature);
        Patcher.Patch();
    }

    public void Unpatch(Feature feature) {
        _enabledFeatures.Remove(feature);
        if(_enabledFeatures.Count == 0) Patcher.Unpatch();
    }

    private void OnFailPatch(string name, bool disabled) {
        foreach(Feature feature in _enabledFeatures.ToArray()) {
            try {
                feature.OnFailPatch(name, disabled);
            } catch (Exception e) {
                Mod.LogReportException("OnFailPatch Error for feature '" + feature.Name + "' in patch '" + name + "'", e);
            }
        }
    }

    private class DefaultTypeMultiPatch : MultiFeaturePatch {
        public DefaultTypeMultiPatch(JAMod mod, Type type) : base(mod) {
            Patcher.AddPatch(type);
        }
    }
}