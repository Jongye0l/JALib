using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using JALib.API;
using JALib.API.Packets;
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

    internal async void Load() {
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
            _localizations = JsonConvert.DeserializeObject<SortedDictionary<string, string>>(await File.ReadAllTextAsync(localizationDataPath));
            _jaMod.OnLocalizationUpdate0();
        }
        LoadLocalization(language);
    }

    internal void LoadLocalization(SystemLanguage language) {
        JATask.Run(_jaMod, async () => {
            using HttpClient httpClient = new();
            if(_jaMod.Gid == -1) return;
            string data = await httpClient.GetString(LOCALIZATION_URL + _jaMod.Gid);
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
            _localizations = localizations;
            MainThread.Run(new JAction(_jaMod, () => _jaMod.OnLocalizationUpdate0()));
            IDictionary<string, string>[] allLocalizations = new IDictionary<string, string>[languages.Count];
            for(int i = 0; i < languages.Count; i++) allLocalizations[i] = new Dictionary<string, string>();
            foreach(JToken token in array.Skip(1)) for(int i = 0; i < languages.Count; i++) SetLocalization(allLocalizations[i], token, i + 1, subindex, languages.Count);
            for(int i = 0; i < languages.Count; i++) {
                string path = Path.Combine(_jaMod.Path, "localization", languages[i] + ".json");
                _ = File.WriteAllTextAsync(path,
                    JsonConvert.SerializeObject(allLocalizations[i], Formatting.Indented));
            }
        });
    }

    private void SetLocalization(IDictionary<string, string> localizations, JToken token, int index, int subindex, int count) {
        JArray row = token["c"] as JArray;
        string key = row[0]["v"].ToString();
        JToken valueToken = GetGoogleJToken(row[index]) ?? GetGoogleJToken(row[subindex]);
        if(valueToken == null) for(int i = 0; i < count && valueToken == null; i++) valueToken = GetGoogleJToken(row[i + 1]);
        localizations[key] = valueToken?.ToString() ?? key;
    }

    private JToken GetGoogleJToken(JToken token) {
        if(token.Type != JTokenType.Object) return null;
        token = token["v"];
        return token is not { Type: JTokenType.String } ? null : token;
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
        GC.SuppressFinalize(_localizations);
        GC.SuppressFinalize(this);
    }
}