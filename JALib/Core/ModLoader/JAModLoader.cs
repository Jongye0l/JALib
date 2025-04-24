using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JALib.Bootstrap;
using JALib.Tools;

namespace JALib.Core.ModLoader;

class JAModLoader {
    public static Dictionary<string, JAModLoader> ModLoadDataList = new();
    private static int count;
    public static bool LoadComplete;
    public string name;
    public RawModData RawModData;
    public DownloadModData DownloadModData;
    public ModLoadState LoadState = ModLoadState.None;
    public List<JAModLoader> OnComplete;
    public JAMod mod;

    static JAModLoader() {
        JAModLoader modLoadData = GetModLoadData("JALib");
        modLoadData.LoadState = ModLoadState.Loaded;
    }

    public static void AddMod(JAModInfo modInfo, int repeatCount) {
        JAModLoader modLoadData = GetModLoadData(modInfo.ModEntry.Info.Id);
        count++;
        modLoadData.RawModData = new RawModData(modLoadData, modInfo, repeatCount);
    }

    public static void CheckDependenciesLoadComplete() {
        if(LoadComplete) return;
        FieldInfo field = typeof(JABootstrap).Field("LoadCount");
        if(field != null && count < field.GetValue<int>()) return;
        if(ModLoadDataList.Values.Any(data => data.RawModData?.loadDependencies == false)) return;
        LoadComplete = true;
        foreach(JAModLoader data in ModLoadDataList.Values) {
            if(data.LoadState == ModLoadState.Loaded) continue;
            if(data.RawModData == null) data.DownloadModData?.Download();
            else data.RawModData.CheckFinishInit();
        }
    }

    public JAModLoader(string name) {
        this.name = name;
        ModLoadDataList.Add(name, this);
    }

    public void DownloadRequest(Version version) => (DownloadModData ??= new DownloadModData(this, version)).DownloadRequest(version);

    public static JAModLoader GetModLoadData(string name) => ModLoadDataList.TryGetValue(name, out JAModLoader data) ? data : new JAModLoader(name);

    public void AddCompleteHandle(JAModLoader data) {
        if(LoadState == ModLoadState.Loaded) return;
        OnComplete ??= [];
        OnComplete.Add(data);
    }

    public void Complete() {
        LoadState = ModLoadState.Loaded;
        OnComplete?.ForEach(data => data.RawModData.RecheckDependencies());
        OnComplete = null;
        RawModData = null;
    }
}