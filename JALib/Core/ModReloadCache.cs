using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JALib.Core.Patch;
using JALib.Tools;
using UnityEngine;

namespace JALib.Core;

public class ModReloadCache {
    private static List<ModReloadCache> Caches = [];
    public Dictionary<(Type, int), object> CachedObjects = new();
    public Assembly OldAssembly;
    public Assembly NewAssembly;

    static ModReloadCache() {
        JALib.Patcher.AddPatch(typeof(ModReloadCache));
    }

    internal ModReloadCache(Assembly oldAssembly, Assembly assembly) {
        OldAssembly = oldAssembly;
        NewAssembly = assembly;
        Caches.Add(this);
    }

    [JAPatch(typeof(GameObject), "AddComponent", PatchType.Postfix, false, ArgumentTypesType = [ typeof(Type) ])]
    private static void AddComponentPatch(Type componentType, ref Component __result) {
        foreach(ModReloadCache cache in Caches) {
            if(cache.OldAssembly != componentType.Assembly) continue;
            __result = cache.GetCachedObject(__result) as Component;
            break;
        }
    }

    [JAPatch(typeof(GameObject), "GetComponentAtIndex", PatchType.Postfix, false, ArgumentTypesType = [ typeof(int) ])]
    private static void GetComponentAtIndexPatch(ref Component __result) {
        foreach(ModReloadCache cache in Caches) {
            if(cache.OldAssembly != __result.GetType().Assembly) continue;
            __result = cache.GetCachedObject(__result) as Component;
            break;
        }
    }

    public object GetCachedObject(object oldValue) {
        if(oldValue == null) return null;
        Type oldType = oldValue.GetType();
        if(oldType.Assembly != OldAssembly) return oldValue;
        if(CachedObjects.TryGetValue((oldType, oldValue.GetHashCode()), out object value)) return value;
        if(oldValue is JAMod mod) return JAMod.GetMods(mod.Name);
        Type newType = NewAssembly.GetType(oldType.FullName);
        if(newType == null) return null;
        try {
            object newValue = CreateInstance(newType, oldValue);
            CachedObjects[(oldType, oldValue.GetHashCode())] = newValue;
            foreach(FieldInfo field in oldType.Fields()) {
                try {
                    newValue.SetValue(field.Name, GetCachedObject(field.GetValue(oldValue)));
                } catch (Exception e) {
                    JALib.Instance.Log("Failed to reload field " + field.Name + " of type " + oldType.FullName);
                    JALib.Instance.LogException(e);
                }
            }
            return newValue;
        } catch (Exception e) {
            JALib.Instance.Log("Failed to reload object of type " + oldType.FullName);
            JALib.Instance.LogException(e);
        }
        return null;
    }

    private object CreateInstance(Type type, object original) {
        ConstructorInfo constructor = type.Constructor([]);
        if(constructor != null) return constructor.Invoke([]);
        foreach(ConstructorInfo info in type.Constructors()) {
            FieldInfo[] fields = new FieldInfo[info.GetParameters().Length];
            for(int i = 0; i < fields.Length; i++) {
                string name = info.GetParameters()[i].Name;
                fields[i] = type.Field(name);
                if(fields[i] != null) continue;
                name = name.ToLower();
                foreach(FieldInfo field in type.Fields()) {
                    if(!string.Equals(field.Name, name, StringComparison.CurrentCultureIgnoreCase) && !string.Equals(field.Name, '_' + name, StringComparison.CurrentCultureIgnoreCase)) continue;
                    fields[i] = field;
                    break;
                }
                if(fields[i] != null) continue;
                fields = null;
                break;
            }
            if(fields == null) continue;
            try {
                object[] parameters = new object[fields.Length];
                for(int i = 0; i < fields.Length; i++) parameters[i] = GetCachedObject(fields[i].GetValue(original));
                return info.Invoke(parameters);
            } catch (Exception e) {
                JALib.Instance.Log(type.FullName + " Constructor founded but failed to invoke");
                JALib.Instance.LogException(e);
            }
        }
        throw new Exception("No available constructor found for type " + type.FullName);
    }
}