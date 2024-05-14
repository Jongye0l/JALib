using System.Collections.Generic;
using System.IO;
using JALib.Core;
using JALib.Stream;
using Newtonsoft.Json;
using UnityEngine;

namespace JALib.API.Packets;

internal class GetLocalization : RequestPacket {

    private JALocalization localization;
    private byte language;
    public SortedDictionary<string, string> Localizations;
    
    public GetLocalization(JALocalization localization, SystemLanguage language) {
        this.localization = localization;
        this.language = (byte) language;
    }

    public override void ReceiveData(byte[] data) {
        using ByteArrayDataInput input = new(data, JALib.Instance);
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
    }

    public override byte[] GetBinary() {
        using ByteArrayDataOutput output = new(JALib.Instance);
        output.WriteUTF(localization._jaMod.Name);
        output.WriteByte(language);
        return output.ToByteArray();
    }
}