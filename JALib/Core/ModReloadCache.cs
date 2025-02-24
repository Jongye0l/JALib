using System.Collections.Generic;
using System.Reflection;
using JALib.Tools;

namespace JALib.Core;

public class ModReloadCache {
    public Dictionary<(Type, int), object> CachedObjects = new();
    public Assembly NewAssembly;
    public Assembly OldAssembly;

    internal ModReloadCache(Assembly oldAssembly, Assembly assembly) {
        OldAssembly = oldAssembly;
        NewAssembly = assembly;
    }

    public object GetCachedObject(object oldValue) {
        if(oldValue == null) return null;
        Type oldType = oldValue.GetType();
        if(oldType.Assembly != OldAssembly) return oldValue;
        if(CachedObjects.TryGetValue((oldType, oldValue.GetHashCode()), out object value)) return value;
        Type newType = NewAssembly.GetType(oldType.FullName);
        if(newType == null) return null;
        try {
            object newValue = newType.New();
            CachedObjects[(oldType, oldValue.GetHashCode())] = newValue;
            foreach(FieldInfo field in oldType.Fields()) {
                try {
                    newValue.SetValue(field.Name, GetCachedObject(field.GetValue(oldValue)));
                } catch (Exception e) {
                    string key = "Failed to reload field " + field.Name + " of type " + oldType.FullName;
                    JALib.Instance.LogException(key, e);
                    JALib.Instance.ReportException(key, e);
                }
            }
            return newValue;
        } catch (Exception e) {
            string key = "Failed to reload object of type " + oldType.FullName;
            JALib.Instance.LogException(key, e);
            JALib.Instance.ReportException(key, e);
        }
        return null;
    }
}