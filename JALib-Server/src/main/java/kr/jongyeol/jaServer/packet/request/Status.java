package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.RequestPacket;

import java.util.Arrays;

public class Status extends RequestPacket {
    @Override
    public void getData(Connection connection, byte[] data) throws Exception {
        ByteArrayDataInput input = new ByteArrayDataInput(data);
        int ping = input.readInt();
        long[] notComplete = new long[input.readInt()];
        for(int i = 0; i < notComplete.length; i++) notComplete[i] = input.readLong();
        connection.logger.info("ping:" + ping + ",notComplete:" + Arrays.toString(notComplete));
    }

    @Override
    public byte[] getBinary() throws Exception {
        return new byte[0];
    }
}
