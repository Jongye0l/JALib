using System;
using System.Collections.Generic;
using System.IO;
using JALib.API;
using JALib.API.Packets;
using Newtonsoft.Json;
using UnityEngine;

namespace JALib.Core;

public class JALocalization {
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
        await JApi.Send(new GetLocalization(this, language));
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