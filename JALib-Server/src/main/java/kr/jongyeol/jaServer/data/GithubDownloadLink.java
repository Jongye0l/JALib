package kr.jongyeol.jaServer.data;

import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import lombok.AllArgsConstructor;

@AllArgsConstructor
public class GithubDownloadLink extends DownloadLink {
    public String modName;

    @Override
    public String getLink(Version version) {
        return "https://github.com/Jongye0l/" + modName + "/releases/download/v" + version + "/" + modName +".zip";
    }

    public String getSourceLink(Version version) {
        return "https://github.com/Jongye0l/" + modName + "/tree/v" + version;
    }

    @Override
    public void write(ByteArrayDataOutput output) {
        output.writeByte((byte) 0);
    }
}
