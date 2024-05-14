using System;

namespace JALib.Core;

public struct Dependency {
    public readonly string Name;
    public readonly Version RequireVersion;

    public Dependency(string name, Version requireVersion) {
        Name = name;
        RequireVersion = requireVersion;
    }
}