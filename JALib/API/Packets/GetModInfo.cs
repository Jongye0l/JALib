using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.Bootstrap;
using JALib.Core;
using JALib.Tools;
using JALib.Tools.ByteTool;
using UnityEngine;

namespace JALib.API.Packets;

class GetModInfo : RequestAPI {

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

    public override void ReceiveData(Stream input) {
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

    public override async Task Run(HttpClient client, string url) {
        try {
            await using Stream stream = await client.GetStreamAsync(url + $"modInfo/{modInfo.ModName}/{modInfo.ModVersion}/{(modInfo.IsBetaBranch ? 1 : 0)}");
            ReceiveData(stream);
        } catch (Exception e) {
            JALib.Instance.Log("Failed to connect to the server: " + url);
            JALib.Instance.LogException(e);
        }
    }
}