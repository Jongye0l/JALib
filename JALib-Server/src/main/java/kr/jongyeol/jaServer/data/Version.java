package kr.jongyeol.jaServer.data;

public class Version {
    public final int major;
    public final int minor;
    public final int build;
    public final int revision;

    public Version(int major, int minor, int build, int revision) {
        if(major < 0) throw new IllegalArgumentException("Version's parameters must be greater than or equal to zero.");
        this.major = major;
        if(minor < 0) throw new IllegalArgumentException("Version's parameters must be greater than or equal to zero.");
        this.minor = minor;
        if(build < 0) throw new IllegalArgumentException("Version's parameters must be greater than or equal to zero.");
        this.build = build;
        if(revision < 0)
            throw new IllegalArgumentException("Version's parameters must be greater than or equal to zero.");
        this.revision = revision;
    }

    public Version(int major, int minor, int build) {
        if(major < 0) throw new IllegalArgumentException("Version's parameters must be greater than or equal to zero.");
        this.major = major;
        if(minor < 0) throw new IllegalArgumentException("Version's parameters must be greater than or equal to zero.");
        this.minor = minor;
        if(build < 0) throw new IllegalArgumentException("Version's parameters must be greater than or equal to zero.");
        this.build = build;
        this.revision = -1;
    }

    public Version(int major, int minor) {
        if(major < 0) throw new IllegalArgumentException("Version's parameters must be greater than or equal to zero.");
        this.major = major;
        if(minor < 0) throw new IllegalArgumentException("Version's parameters must be greater than or equal to zero.");
        this.minor = minor;
        this.build = -1;
        this.revision = -1;
    }

    public Version(String version) {
        String[] split = version.split("\\.");
        this.major = Integer.parseInt(split[0]);
        this.minor = Integer.parseInt(split[1]);
        this.build = split.length < 3 ? -1 : Integer.parseInt(split[2]);
        this.revision = split.length < 4 ? -1 : Integer.parseInt(split[3]);
    }

    public String toString() {
        return major + "." + minor + (build == -1 ? "" : "." + build) + (revision == -1 ? "" : "." + revision);
    }

    public boolean isUpper(Version version) {
        if(major < version.major) return true;
        if(major > version.major) return false;
        if(minor < version.minor) return true;
        if(minor > version.minor) return false;
        if(build < version.build) return true;
        if(build > version.build) return false;
        return revision < version.revision;
    }

    @Override
    public boolean equals(Object obj) {
        return obj instanceof Version version && version.major == major && version.minor == minor && version.build == build && version.revision == revision;
    }

    @Override
    public int hashCode() {
        return (major * 1000000000 + minor * 1000000 + build * 1000 + revision) * "Version".hashCode();
    }
}
