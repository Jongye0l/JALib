using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JALib.Tools;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace JALib.Core.Setting;

public class JASetting : IDisposable {

    private static readonly JValue NullValue = JValue.CreateNull();
    protected JAMod Mod;
    protected JObject JsonObject;
    private IEnumerable<FieldInfo> jsonFields;
    private Dictionary<string, string> fieldValueCache = new();

    public JASetting(JAMod mod, JObject jsonObject = null) {
        Mod = mod;
        JsonObject = jsonObject ?? new JObject();
        jsonFields = GetType().Fields().Where(field => {
            if(field.IsStatic) return false;
            SettingIncludeAttribute include = field.GetCustomAttribute<SettingIncludeAttribute>();
            SettingIgnoreAttribute ignore = field.GetCustomAttribute<SettingIgnoreAttribute>();
            return ignore == null && (field.IsPublic || include != null);
        });
        if(jsonObject != null) LoadJson();
        else
            foreach(FieldInfo field in jsonFields)
                if(IsSettingType(field.FieldType))
                    field.SetValue(this, SetupJASetting(field.FieldType, null));
    }

    private void LoadJson() {
        try {
            foreach(FieldInfo field in jsonFields) {
                SettingNameAttribute nameAttribute = field.GetCustomAttribute<SettingNameAttribute>();
                string name = nameAttribute?.Name ?? field.Name;
                try {
                    if(JsonObject.TryGetValue(name, out JToken token)) {
                        field.SetValue(this, IsSettingType(field.FieldType)     ? SetupJASetting(field.FieldType, token) :
                                             field.FieldType == typeof(Version) ? ToVersion(token) : token.ToObject(field.FieldType));
                        JsonObject.Remove(name);
                    } else if(IsSettingType(field.FieldType)) field.SetValue(this, SetupJASetting(field.FieldType, null));
                } catch (Exception e) {
                    JAMod mod = Mod ?? JALib.Instance;
                    string key = "Failed To Load Field: " + name;
                    if(mod != null) mod.LogException(key, e);
                    else JALogger.LogExceptionInternal(key, e);
                }
            }
        } catch (Exception e) {
            JAMod mod = Mod ?? JALib.Instance;
            const string key = "Failed To Load Setting";
            if(mod != null) mod.LogReportException(key, e);
            else JALogger.LogExceptionInternal(key, e);
        }
    }

    private static Version ToVersion(JToken token) {
        try {
            return token.ToObject<Version>();
        } catch (Exception) {
            JObject obj = token as JObject;
            int major = obj["Major"].Value<int>();
            int minor = obj["Minor"].Value<int>();
            int build = obj["Build"].Value<int>();
            int revision = obj["Revision"].Value<int>();
            return build == -1 ? new Version(major, minor) : revision == -1 ? new Version(major, minor, build) : new Version(major, minor, build, revision);
        }
    }

    private static bool IsSettingType(Type type) {
        return type.IsSubclassOf(typeof(JASetting)) || type == typeof(JASetting);
    }

    internal JASetting SetupJASetting(Type type, JToken token) {
        return type.New<JASetting>(Mod, token as JObject);
    }

    [CanBeNull]
    public JToken this[string key] {
        get => JsonObject[key];
        set => JsonObject[key] = value;
    }

    public void Remove(string key) {
        JsonObject.Remove(key);
    }

    public bool Get<T>(string key, out T value) {
        JToken token = JsonObject[key];
        if(token == null) {
            value = default;
            return false;
        }
        value = token.ToObject<T>();
        return true;
    }

    public void Set(string key, object value) {
        JsonObject[key] = JToken.FromObject(value);
    }

    public virtual void PutFieldData() {
        try {
            foreach(FieldInfo field in jsonFields) {
                SettingNameAttribute nameAttribute = field.GetCustomAttribute<SettingNameAttribute>();
                SettingCastAttribute castAttribute = field.GetCustomAttribute<SettingCastAttribute>();
                SettingRoundAttribute roundAttribute = field.GetCustomAttribute<SettingRoundAttribute>();
                string name = nameAttribute?.Name ?? field.Name;
                object o = field.GetValue(this);
                if(o is JASetting setting) {
                    setting.PutFieldData();
                    JsonObject[name] = setting.JsonObject;
                    continue;
                }
                if(castAttribute != null) o = Convert.ChangeType(o, castAttribute.CastType);
                if(roundAttribute != null) o = Convert.ChangeType(Math.Round((double) o!, roundAttribute.Round), o.GetType());
                JsonObject[name] = o switch {
                    null => NullValue,
                    Color color => ColorToJson(color),
                    _ => JToken.FromObject(o)
                };
            }
        } catch (Exception e) {
            JALib.Instance.LogReportException("Failed To Put Field Data", e, [Mod, JALib.Instance]);
        }
    }

    private static JToken ColorToJson(Color color) {
        return new JObject {
            ["R"] = color.r,
            ["G"] = color.g,
            ["B"] = color.b,
            ["A"] = color.a
        };
    }

    public virtual void RemoveFieldData() {
        try {
            foreach(FieldInfo field in jsonFields) {
                SettingNameAttribute nameAttribute = field.GetCustomAttribute<SettingNameAttribute>();
                string name = nameAttribute?.Name ?? field.Name;
                JsonObject.Remove(name);
                if(field.GetValue(this) is JASetting setting) setting.RemoveFieldData();
            }
        } catch (Exception e) {
            JALib.Instance.LogReportException("Failed To Remove Field Data", e, [Mod, JALib.Instance]);
        }
    }

    protected virtual void Dispose0() {
        try {
            GC.SuppressFinalize(fieldValueCache);
            GC.SuppressFinalize(JsonObject);
            foreach(FieldInfo field in jsonFields)
                if(field.GetValue(this) is JASetting setting)
                    setting.Dispose();
            GC.SuppressFinalize(jsonFields);
            GC.SuppressFinalize(this);
        } catch (Exception e) {
            JALib.Instance.LogReportException("Failed To Setting Dispose", e, [Mod, JALib.Instance]);
        }
    }

    public void Dispose() {
        try {
            Dispose0();
        } catch (Exception e) {
            JALib.Instance.LogReportException("Failed To Setting Dispose", e);
        }
    }
}