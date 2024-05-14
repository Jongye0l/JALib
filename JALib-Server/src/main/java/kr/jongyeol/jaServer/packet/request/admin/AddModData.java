package kr.jongyeol.jaServer.packet.request.admin;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.*;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;
import lombok.AllArgsConstructor;
import lombok.Cleanup;
import lombok.NoArgsConstructor;

@NoArgsConstructor
@AllArgsConstructor
public class AddModData extends RequestPacket {

    private ModData modData;

    @Override
    public void getData(Connection connection, byte[] data) throws Exception {
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(data);
        String name = input.readUTF();
        ModData modData = ModData.clazz.getConstructor().newInstance();
        modData.setName(name);
        modData.setVersion(new Version(input.readUTF()));
        modData.setBetaVersion(new Version(input.readUTF()));
        modData.setForceUpdate(input.readBoolean());
        ForceUpdateHandle[] handles = new ForceUpdateHandle[input.readInt()];
        for(int i = 0; i < handles.length; i++) handles[i] = new ForceUpdateHandle(input);
        modData.setForceUpdateHandles(handles);
        if(input.readBoolean()) modData.setHomepage(input.readUTF());
        modData.setDownloadLink(DownloadLink.createDownloadLink(modData, input));
        modData.setGid(input.readInt());
    }

    @Override
    public byte[] getBinary() throws Exception {
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeUTF(modData.getVersion().toString());
        output.writeUTF(modData.getBetaVersion().toString());
        output.writeBoolean(modData.isForceUpdate());
        ForceUpdateHandle[] handles = modData.getForceUpdateHandles();
        output.writeInt(handles.length);
        for(ForceUpdateHandle handle : handles) handle.write(output);
        output.writeBoolean(modData.getHomepage() != null);
        if(modData.getHomepage() != null) output.writeUTF(modData.getHomepage());
        modData.getDownloadLink().write(output);
        output.writeInt(modData.getGid());
        return output.toByteArray();
    }
}
