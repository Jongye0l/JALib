package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.CSharpException;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;

public class ExceptionInfo extends RequestPacket {

    public String name;
    public String version;
    public int hashCode;
    public CSharpException exception;

    @Override
    public void getData(Connection connection, ByteArrayDataInput input) throws Exception {
        name = input.readUTF();
        version = input.readUTF();
        hashCode = input.readInt();
        exception = new CSharpException(input);
        connection.logger.error("ExceptionInfo: " + name + " " + version + " " + hashCode + " " + exception);
    }

    @Override
    public void getBinary(ByteArrayDataOutput output) throws Exception {
    }
}
