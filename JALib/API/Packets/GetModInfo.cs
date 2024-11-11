using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.Bootstrap;
using JALib.Tools.ByteTool;
using UnityEngine;

namespace JALib.API.Packets;

class GetModInfo : GetRequest {

    private string name;
    private Version version;
    private bool? beta;
    internal bool Success;
    internal Version LatestVersion;
    internal Version LatestBetaVersion;
    internal bool ForceUpdate;
    internal bool ForceBetaUpdate;
    internal Version RequestedVersion;
    internal SystemLanguage[] AvailableLanguages;
    internal string Homepage;
    internal string Discord;
    internal int Gid;

    public GetModInfo(JAModInfo modInfo, bool? beta) {
        name = modInfo.ModEntry.Info.Id;
        version = modInfo.ModEntry.Version;
        this.beta = beta;
    }

    public override string UrlBehind => $"modInfoV2/{name}/{version}" + (beta.HasValue ? "/" + (beta.Value ? 1 : 0) : "");

    public override async Task Run(HttpResponseMessage message) {
        await using Stream input = await message.Content.ReadAsStreamAsync();
        if(!(Success = input.ReadBoolean())) return;
        string ver = input.ReadUTF();
        if(ver != null) LatestVersion = Version.Parse(ver);
        LatestBetaVersion = Version.Parse(input.ReadUTF());
        ForceUpdate = input.ReadBoolean();
        ForceBetaUpdate = input.ReadBoolean();
        ver = input.ReadUTF();
        if(ver != null) RequestedVersion = Version.Parse(ver);
        SystemLanguage[] languages = new SystemLanguage[input.ReadByte()];
        for(int i = 0; i < languages.Length; i++) languages[i] = (SystemLanguage) input.ReadByte();
        AvailableLanguages = languages;
        Homepage = input.ReadUTF();
        Discord = input.ReadUTF();
        Gid = input.ReadInt();
    }
}