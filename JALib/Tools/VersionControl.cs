using UnityEngine;

namespace JALib.Tools;

public static class VersionControl {
    public static int releaseNumber;
    public static Version version;

    static VersionControl() {
        releaseNumber = SimpleUnsafeReflect.GetValueUnsafeValue<int>(typeof(GCNS).Field("releaseNumber"));
        Version.TryParse(Application.version, out version);
    }
}