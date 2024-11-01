namespace JALib.Core.Patch.PatchAttribute;

public abstract class JAPatchBase : Attribute {
    internal static int GetCurrentVersion => GCNS.releaseNumber;
    public int MinVersion = GetCurrentVersion;
    public int MaxVersion = GetCurrentVersion;
}