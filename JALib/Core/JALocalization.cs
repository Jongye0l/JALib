﻿using System.Collections.Generic;
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
    internal SortedDictionary<string, string> _localizations;
    internal JAMod _jaMod;
    internal SystemLanguage? _curLang;

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
        if(File.Exists(localizationDataPath)) {
            _localizations?.Clear();
            File.ReadAllTextAsync(localizationDataPath).ContinueWith(LoadOnFile);
        }
        if(_jaMod.Gid == -1) return;
        _ = new LocalizationLoader(this, language);
    }

    private void LoadOnFile(Task<string> t) {
        try {
            _localizations = JsonConvert.DeserializeObject<SortedDictionary<string, string>>(t.Result);
            _jaMod.OnLocalizationUpdate0();
        } catch (Exception e) {
            _jaMod.LogReportException("Failed to load localization data.", e);
        }
    }

    private class LocalizationLoader {
        private readonly JALocalization localization;
        private SystemLanguage language;
        private HttpClient httpClient = new();

        public LocalizationLoader(JALocalization localization, SystemLanguage language) {
            this.localization = localization;
            this.language = language;
            httpClient.GetString(LOCALIZATION_URL + localization._jaMod.Gid).ContinueWith(Load);
        }

        public void Load(Task<string> task) {
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
                SortedDictionary<string, string> localizations = new();
                foreach(JToken token in array.Skip(1)) SetLocalization(localizations, token, index, subindex, languages.Count);
                localization._localizations = localizations;
                MainThread.Run(new JAction(mod, mod.OnLocalizationUpdate0));
                IDictionary<string, string>[] allLocalizations = new IDictionary<string, string>[languages.Count];
                for(int i = 0; i < languages.Count; i++) allLocalizations[i] = new Dictionary<string, string>();
                foreach(JToken token in array.Skip(1))
                    for(int i = 0; i < languages.Count; i++)
                        SetLocalization(allLocalizations[i], token, i + 1, subindex, languages.Count);
                for(int i = 0; i < languages.Count; i++) {
                    string path = Path.Combine(mod.Path, "localization", languages[i] + ".json");
                    File.WriteAllTextAsync(path, JsonConvert.SerializeObject(allLocalizations[i], Formatting.Indented));
                }
            } catch (Exception e) {
                mod.LogReportException("Failed to load localization data.", e);
            } finally {
                httpClient.Dispose();
            }
        }

        private static void SetLocalization(IDictionary<string, string> localizations, JToken token, int index, int subindex, int count) {
            JArray row = token["c"] as JArray;
            string key = row[0]["v"].ToString();
            JToken valueToken = GetGoogleJToken(row[index]) ?? GetGoogleJToken(row[subindex]);
            if(valueToken == null) for(int i = 0; i < count && valueToken == null; i++) valueToken = GetGoogleJToken(row[i + 1]);
            localizations[key] = valueToken?.ToString() ?? key;
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
                    __result = mod.Localization[key.Replace("jamod." + split[1] + ".", "").Replace("jalib." + split[1] + ".", "")];
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