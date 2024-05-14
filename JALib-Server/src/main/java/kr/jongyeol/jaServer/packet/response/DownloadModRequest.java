package kr.jongyeol.jaServer.packet.response;

import kr.jongyeol.jaServer.data.RawMod;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.ResponsePacket;
import lombok.AllArgsConstructor;
import lombok.Cleanup;

@AllArgsConstructor
public class DownloadModRequest extends ResponsePacket {

    public RawMod mod;

    @Override
    public byte[] getBinary() {
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(mod.mod.getName());
        output.writeUTF(mod.version.toString());
        return output.toByteArray();
    }
}
