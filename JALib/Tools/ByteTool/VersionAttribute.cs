using System;

namespace JALib.Tools.ByteTool;

[AttributeUsage(AttributeTargets.Class)]
public class VersionAttribute : Attribute {
    public int Version;

    public VersionAttribute(int version) {
        Version = version;
    }
}