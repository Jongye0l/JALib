using UnityEngine;

namespace JALib.Tools;

public static class VersionControl {
    public static int releaseNumber;
    public static Version version;

    static VersionControl() {
        releaseNumber = typeof(GCNS).Field("releaseNumber").GetValue<int>();
#if !TEST
        Version.TryParse(Application.version, out version);
#endif
    }
#if TEST
    internal static void SetupVersion() {
        Version.TryParse(Application.version, out version);
    }
#endif
}