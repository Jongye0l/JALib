using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using JALib.Core;
using JALib.Stream;
using JALib.Tools;
using Newtonsoft.Json;
using UnityEngine;

namespace JALib.API.Packets;

internal class GetLocalization : RequestAPI {

    private JALocalization localization;
    private byte language;
    public SortedDictionary<string, string> Localizations;
    
    public GetLocalization(JALocalization localization, SystemLanguage language) {
        this.localization = localization;
        this.language = (byte) language;
    }

    public override void ReceiveData(ByteArrayDataInput input) {
        SystemLanguage language = (SystemLanguage) input.ReadByte();
        Localizations = new SortedDictionary<string, string>();
        for(int i = 0; i < input.ReadInt(); i++) Localizations.Add(input.ReadUTF(), input.ReadUTF());
        localization._localizations = Localizations;
        if(localization._jaMod == null) localization._localizations = null;
        if(localization._localizations == null) return;
        string path = Path.Combine(localization._jaMod.Path, "localization", language + ".json");
        if(!File.Exists(path)) File.Create(path);
        File.WriteAllTextAsync(Path.Combine(localization._jaMod.Path, "localization", language + ".json"), 
            JsonConvert.SerializeObject(localization._localizations, Formatting.Indented));
        MainThread.Run(new JAction(localization._jaMod, () => localization._jaMod.OnLocalizationUpdate0()));
    }

    public override async void Run(HttpClient client, string url) {
        try {
            System.IO.Stream stream = await client.GetStreamAsync(url + $"/localization/{localization._jaMod.Name}/{language}");
            using ByteArrayDataInput input = new(stream, JALib.Instance);
            ReceiveData(input);
        } catch (Exception e) {
            JALib.Instance.Log("Failed to connect to the server: " + url);
            JALib.Instance.LogException(e);
        }
    }
}