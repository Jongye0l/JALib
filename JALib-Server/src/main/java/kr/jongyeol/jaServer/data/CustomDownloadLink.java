package kr.jongyeol.jaServer.data;

import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import lombok.AllArgsConstructor;

import java.util.Map;

@AllArgsConstructor
public class CustomDownloadLink extends DownloadLink {

    public Map<Version, String> links;

    @Override
    public String getLink(Version version) {
        return links.get(version);
    }

    @Override
    public void write(ByteArrayDataOutput output) {
        output.writeByte((byte) 1);
        output.writeInt(links.size());
        links.forEach((version, link) -> {
            output.writeUTF(version.toString());
            output.writeUTF(link);
        });
    }
}
