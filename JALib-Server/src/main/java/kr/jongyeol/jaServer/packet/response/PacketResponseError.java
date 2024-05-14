package kr.jongyeol.jaServer.packet.response;

import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.ResponsePacket;
import lombok.Cleanup;

public class PacketResponseError extends ResponsePacket {

    public long id;
    public Throwable exception;

    public PacketResponseError(long id, Throwable exception) {
        this.id = id;
        this.exception = exception;
    }

    @Override
    public byte[] getBinary() throws Exception {
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeLong(id);
        output.writeUTF(exception.getClass().getName() + ": " + exception.getMessage());
        return output.toByteArray();
    }
}
