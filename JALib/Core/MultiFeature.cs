using System.Collections.Generic;
using System.Linq;
using JALib.Core.Patch;

namespace JALib.Core;

public abstract class MultiFeature {
    public readonly JAPatcher Patcher;
    public readonly JAMod Mod;
    private readonly HashSet<Feature> _enabledFeatures = [];
    
    protected MultiFeature(JAMod mod) {
        Patcher = new JAPatcher(mod);
        Patcher.OnFailPatch += OnFailPatch;
        Mod = mod;
    }

    internal static MultiFeature GetMultiFeaturePatch(JAMod mod, Type type) {
        if(mod.MultiFeatures.TryGetValue(type, out MultiFeature patch)) return patch;
        if(type.IsSubclassOf(typeof(MultiFeature))) patch = (MultiFeature) Activator.CreateInstance(type);
        else patch = new DefaultTypeMultiPatch(mod, type);
        mod.MultiFeatures[type] = patch;
        return patch;
    }

    protected virtual void OnEnable() {
    }

    protected virtual void OnDisable() {
    }

    public void ActiveFeature(Feature feature) {
        bool first = _enabledFeatures.Count == 0;
        _enabledFeatures.Add(feature);
        if(!first) return;
        Patcher.Patch();
        OnEnable();
    }

    public void InactiveFeature(Feature feature) {
        _enabledFeatures.Remove(feature);
        if(_enabledFeatures.Count != 0) return;
        Patcher.Unpatch();
        OnDisable();
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

    private class DefaultTypeMultiPatch : MultiFeature {
        public DefaultTypeMultiPatch(JAMod mod, Type type) : base(mod) {
            Patcher.AddPatch(type);
        }
    }
}