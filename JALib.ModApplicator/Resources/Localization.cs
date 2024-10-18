﻿using System.Globalization;

namespace JALib.ModApplicator.Resources;

public class Localization {
    public static Localization Korean = new() {
        Error_ArgumentNotSet = "인수가 설정되지 않았습니다.",
        Error_Title = "모드 적용 오류",
        Error_VersionNotSet = "버전이 설정되지 않았습니다.",
        Error_FailConnectServer = "서버에 연결하지 못했습니다.",
        Error_LoadAdofaiPath = "얼불춤 폴더를 찾는 중 오류가 발생했습니다: ",
        Error_LoadModInfo = "모드 정보를 불러오는 중 오류가 발생했습니다.",
        AdofaiRestart = "모드를 적용하기 위해서는 얼불춤을 재시작 해야합니다. 재시작 하시겠습니까?",
        AdofaiRestartTitle = "JALib 모드 적용기",
        AdofaiStart = "얼불춤을 시작하시겠습니까?",
        ModAnnounceTitle = "모드 적용 안내",
        StartModApply = "{0} 모드 적용을 시작합니다.",
        FinishModApply = "모드 적용이 완료되었습니다."
    };

    public static Localization English = new() {
        Error_ArgumentNotSet = "Argument is not set.",
        Error_Title = "Mod Applicator Error",
        Error_VersionNotSet = "Version is not set.",
        Error_FailConnectServer = "Failed to connect to the server",
        Error_LoadAdofaiPath = "An error occurred while finding the A Dance of Fire and Ice folder: ",
        Error_LoadModInfo = "An error occurred while loading mod information.",
        AdofaiRestart = "To apply the mod, you need to restart A Dance of Fire and Ice. Do you want to restart?",
        AdofaiRestartTitle = "JALib Mod Applicator",
        AdofaiStart = "Do you want to start A Dance of Fire and Ice?",
        ModAnnounceTitle = "Mod Application Announcement",
        StartModApply = "Start applying the {0} mod.",
        FinishModApply = "mod application is complete."
    };

    public static Localization Current => CultureInfo.CurrentCulture.Name == "ko-KR" ? Korean : English;

    public string Error_ArgumentNotSet { get; private set; }
    public string Error_Title { get; private set; }
    public string Error_VersionNotSet { get; private set; }
    public string Error_FailConnectServer { get; private set; }
    public string Error_LoadAdofaiPath { get; private set; }
    public string Error_LoadModInfo { get; private set; }
    public string AdofaiRestart { get; private set; }
    public string AdofaiRestartTitle { get; private set; }
    public string AdofaiStart { get; private set; }
    public string ModAnnounceTitle { get; private set; }
    public string StartModApply{ get; private set; }
    public string FinishModApply { get; private set; }
}