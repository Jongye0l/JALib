using UnityEngine;

namespace JALib.Tools;

public static class VersionControl {
    public static int releaseNumber;
    public static Version version;

    static VersionControl() {
        releaseNumber = typeof(GCNS).Field("releaseNumber").GetValueUnsafeValue<int>();
        Version.TryParse(Application.version, out version);
    }
}