package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.Language;
import kr.jongyeol.jaServer.data.ModData;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;
import lombok.Cleanup;

import java.util.Arrays;
import java.util.Map;

public class GetLocalization extends RequestPacket {

    public String name;
    public Language language;

    @Override
    public void getData(Connection connection, byte[] data) throws Exception {
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(data);
        name = input.readUTF();
        language = Language.values()[input.readByte()];
        connection.logger.info("GetLocalization: " + name + " " + language);
    }

    @Override
    public byte[] getBinary() throws Exception {
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        ModData modData = ModData.getModData(name);
        if(Arrays.stream(modData.getAvailableLanguages()).noneMatch(l -> l == language))
            language = Arrays.stream(modData.getAvailableLanguages()).anyMatch(l -> l == Language.English) ? Language.English : modData.getAvailableLanguages()[0];
        output.writeByte((byte) language.ordinal());
        Map<String, Map<Language, String>> localization = modData.getLocalizations();
        output.writeInt(localization.size());
        for(Map.Entry<String, Map<Language, String>> entry : localization.entrySet()) {
            output.writeUTF(entry.getKey());
            if(entry.getValue().containsKey(language)) output.writeUTF(entry.getValue().get(language));
            else if(entry.getValue().containsKey(Language.English)) output.writeUTF(entry.getValue().get(Language.English));
            else output.writeUTF(entry.getValue().values().stream().findFirst().get());
        }
        return output.toByteArray();
    }
}
