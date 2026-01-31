using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.Core.Patch;
using JALib.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JALib.Core;

public class JALocalization {
    private const string LOCALIZATION_URL = "https://docs.google.com/spreadsheets/d/1kx12GMqK9lgpiZimBSAMdj51xY4IuQUSLXzmQFZ6Sk4/gviz/tq?tqx=out:json&tq&gid=";
    private FrozenDictionary<string, string> _localizations;
    private JAMod _jaMod;
    private SystemLanguage? _curLang;

    internal JALocalization(JAMod jaMod) {
        _jaMod = jaMod;
        Load();
    }

    internal void Load() {
        if(ADOBase.platform == Platform.None) {
            MainThread.WaitForMainThread().GetAwaiter().OnCompleted(Load);
            return;
        }
        SystemLanguage language = _jaMod.CustomLanguage ?? RDString.language;
        if(_curLang == language) return;
        _curLang = language;
        string localizationPath = Path.Combine(_jaMod.Path, "localization");
        if(!Directory.Exists(localizationPath)) Directory.CreateDirectory(localizationPath);
        string localizationDataPath = Path.Combine(localizationPath, language + ".json");
        if(!File.Exists(localizationDataPath)) localizationDataPath = Path.Combine(localizationPath, SystemLanguage.English + ".json");
        if(!File.Exists(localizationDataPath)) localizationDataPath = Path.Combine(localizationPath, SystemLanguage.Korean + ".json");
        if(File.Exists(localizationDataPath)) File.ReadAllTextAsync(localizationDataPath).OnCompleted(LoadOnFile);
        if(_jaMod.Gid == -1) return;
        _ = new WebLoader(this, language);
    }
        
    public void LoadOnFile(Task<string> task) {
        try {
            _localizations = JsonConvert.DeserializeObject<Dictionary<string, string>>(task.Result).ToFrozenDictionary();
            _jaMod.OnLocalizationUpdate0();
        } catch (Exception e) {
            _jaMod.LogReportException("Failed to load localization data.", e);
        }
    }

    private class WebLoader {
        private readonly JALocalization localization;
        private SystemLanguage language;
        private HttpClient httpClient = new();
        private Task<string> task;

        public WebLoader(JALocalization localization, SystemLanguage language) {
            this.localization = localization;
            this.language = language;
            (task = httpClient.GetString(LOCALIZATION_URL + localization._jaMod.Gid)).OnCompleted(Load);
        }

        public void Load() {
            JAMod mod = localization._jaMod;
            try {
                string data = task.Result;
                data = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];
                JArray array = JObject.Parse(data)["table"]["rows"] as JArray;
                List<SystemLanguage> languages = (from token in array[0]["c"].Skip(1)
                                                  where token.Type != JTokenType.Null
                                                  select token["v"]
                                                  into value
                                                  where value is { Type: JTokenType.String }
                                                  select (SystemLanguage) Enum.Parse(typeof(SystemLanguage), value.ToString())).ToList();
                if(languages.Count == 0) return;
                if(!languages.Contains(language)) {
                    language = SystemLanguage.English;
                    if(!languages.Contains(language)) language = languages[0];
                }
                int index = languages.IndexOf(language) + 1;
                int subindex = languages.Contains(SystemLanguage.English) ? languages.IndexOf(SystemLanguage.English) + 1 : 1;
                Dictionary<string, string> localizations = new();
                foreach(JToken token in array.Skip(1)) {
                    KeyValuePair<string, string> v = SetLocalization(token, index, subindex, languages.Count);
                    localizations[v.Key] = v.Value;
                }
                localization._localizations = localizations.ToFrozenDictionary();
                MainThread.Run(new JAction(mod, mod.OnLocalizationUpdate0));
                JObject[] allLocalizations = new JObject[languages.Count];
                for(int i = 0; i < languages.Count; i++) allLocalizations[i] = new JObject();
                foreach(JToken token in array.Skip(1))
                    for(int i = 0; i < languages.Count; i++) {
                        KeyValuePair<string, string> v = SetLocalization(token, i + 1, subindex, languages.Count);
                        allLocalizations[i][v.Key] = v.Value;
                    }
                for(int i = 0; i < languages.Count; i++) {
                    string path = Path.Combine(mod.Path, "localization", languages[i] + ".json");
                    File.WriteAllTextAsync(path, allLocalizations[i].ToString()).CatchException(JALib.Instance);
                }
            } catch (Exception e) {
                mod.LogReportException("Failed to load localization data.", e);
            } finally {
                httpClient.Dispose();
            }
        }

        private static KeyValuePair<string, string> SetLocalization(JToken token, int index, int subindex, int count) {
            JArray row = token["c"] as JArray;
            string key = row[0]["v"].ToString();
            JToken valueToken = GetGoogleJToken(row[index]) ?? GetGoogleJToken(row[subindex]);
            if(valueToken == null) for(int i = 0; i < count && valueToken == null; i++) valueToken = GetGoogleJToken(row[i + 1]);
            return new KeyValuePair<string, string>(key, valueToken?.ToString() ?? key);
        }

        private static JToken GetGoogleJToken(JToken token) {
            if(token.Type != JTokenType.Object) return null;
            token = token["v"];
            return token is not { Type: JTokenType.String } ? null : token;
        }
    }

    public string Get(string key) {
        TryGet(key, out string value);
        return value;
    }

    public string this[string key] => Get(key);

    public bool TryGet(string key, out string value) {
        if(_localizations != null && _localizations.TryGetValue(key, out value)) return true;
        value = key;
        return false;
    }

    internal void Dispose() {
        if(_localizations != null) GC.SuppressFinalize(_localizations);
        GC.SuppressFinalize(this);
    }

    [JAPatch(typeof(RDString), "GetWithCheck", PatchType.Prefix, false)]
    internal static bool RDStringPatch(string key, ref bool exists, ref string __result) {
        if(key.StartsWith("jamod.") || key.StartsWith("jalib.")) {
            string[] split = key.Split('.');
            if(split.Length > 3) {
                JAMod mod = JAMod.GetMods(split[1]);
                if(mod != null) {
                    exists = true;
                    // Optimize string operations - use Substring instead of multiple Replace calls
                    string prefix = key.StartsWith("jamod.") ? "jamod." : "jalib.";
                    int prefixLength = prefix.Length + split[1].Length + 1; // +1 for the dot
                    __result = mod.Localization[key.Substring(prefixLength)];
                    return false;
                }
            }
        }
        return true;
    }

    [JAPatch(typeof(RDString), "Setup", PatchType.Postfix, false)]
    internal static void RDStringSetup() {
        foreach(JAMod mod in JAMod.GetMods()) mod.Localization?.Load();
    }
}