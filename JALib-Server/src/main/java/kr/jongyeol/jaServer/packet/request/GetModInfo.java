package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.Language;
import kr.jongyeol.jaServer.data.ModData;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;
import lombok.Cleanup;

public class GetModInfo extends RequestPacket {

    public String name;
    public String version;
    public boolean beta;

    @Override
    public void getData(Connection connection, byte[] data) throws Exception {
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(data);
        name = input.readUTF();
        version = input.readUTF();
        beta = input.readBoolean();
        connection.logger.info("GetModInfo: " + name + " " + version);
    }

    @Override
    public byte[] getBinary() throws Exception {
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        ModData modData = ModData.getModData(name);
        output.writeUTF((beta ? modData.getBetaVersion() : modData.getVersion()).toString());
        output.writeBoolean(modData.isForceUpdate());
        Language[] languages = modData.getAvailableLanguages();
        output.writeByte((byte) languages.length);
        for(Language language : languages) output.writeByte((byte) language.ordinal());
        String homepage = modData.getHomepage();
        output.writeBoolean(homepage != null);
        if(homepage != null) output.writeUTF(homepage);
        String discord = modData.getDiscord();
        output.writeBoolean(discord != null);
        if(discord != null) output.writeUTF(discord);
        return output.toByteArray();
    }
}
