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

    private string name;
    private Version version;
    private bool beta;
    internal bool Success;
    internal Version LatestVersion;
    internal bool ForceUpdate;
    internal SystemLanguage[] AvailableLanguages;
    internal string Homepage;
    internal string Discord;
    internal int Gid;

    public GetModInfo(JAModInfo modInfo) {
        name = modInfo.ModEntry.Info.Id;
        version = modInfo.ModEntry.Version;
        beta = modInfo.IsBetaBranch;
    }

    public override string UrlBehind => $"modInfo/{name}/{version}/{(beta ? 1 : 0)}";

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