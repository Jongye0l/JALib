package kr.jongyeol.jaServer.data;

import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;

import java.util.HashMap;
import java.util.Map;

public abstract class DownloadLink {
    public static DownloadLink createDownloadLink(ModData modData, ByteArrayDataInput input) {
        switch(input.readByte()) {
            case 0:
                return new GithubDownloadLink(modData.getName());
            case 1:
                Map<Version, String> map = new HashMap<>();
                for(int i = 0; i < input.readInt(); i++) map.put(new Version(input.readUTF()), input.readUTF());
                return new CustomDownloadLink(map);
            default:
                return null;
        }
    }

    public abstract String getLink(Version version);

    public abstract void write(ByteArrayDataOutput output);
}
