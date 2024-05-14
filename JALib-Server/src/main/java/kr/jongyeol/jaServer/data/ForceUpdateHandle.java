package kr.jongyeol.jaServer.data;

import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import lombok.AllArgsConstructor;
import lombok.Data;

@Data
@AllArgsConstructor
public class ForceUpdateHandle {
    public Version version1;
    public Version version2;
    public boolean forceUpdate;

    public ForceUpdateHandle(ByteArrayDataInput input) {
        version1 = new Version(input.readUTF());
        version2 = new Version(input.readUTF());
        forceUpdate = input.readBoolean();
    }

    public void write(ByteArrayDataOutput output) {
        output.writeUTF(version1.toString());
        output.writeUTF(version2.toString());
        output.writeBoolean(forceUpdate);
    }
}
