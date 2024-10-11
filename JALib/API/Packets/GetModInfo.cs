using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.Bootstrap;
using JALib.Core;
using JALib.Tools;
using JALib.Tools.ByteTool;
using UnityEngine;

namespace JALib.API.Packets;

class GetModInfo : GetRequest {

    private JAMod mod;
    private JAModInfo modInfo;
    internal bool Success;
    internal Version LatestVersion;
    internal bool ForceUpdate;
    internal SystemLanguage[] AvailableLanguages;
    internal string Homepage;
    internal string Discord;
    internal int Gid;

    public GetModInfo(JAMod mod) {
        this.mod = mod;
    }

    public GetModInfo(JAModInfo modInfo) {
        this.modInfo = modInfo;
    }

    public override string UrlBehind => $"modInfo/{modInfo.ModEntry.Info.Id}/{modInfo.ModEntry.Version}/{(modInfo.IsBetaBranch ? 1 : 0)}";

    public override async Task Run(HttpResponseMessage message) {
        await using Stream input = await message.Content.ReadAsStreamAsync();
        if(!(Success = input.ReadBoolean())) return;
        LatestVersion = Version.Parse(input.ReadUTF());
        ForceUpdate = input.ReadBoolean();
        SystemLanguage[] languages = new SystemLanguage[input.ReadByte()];
        for(int i = 0; i < languages.Length; i++) languages[i] = (SystemLanguage) input.ReadByte();
        AvailableLanguages = languages;
        Homepage = input.ReadUTF();
        Discord = input.ReadUTF();
        Gid = input.ReadInt();
    }
}