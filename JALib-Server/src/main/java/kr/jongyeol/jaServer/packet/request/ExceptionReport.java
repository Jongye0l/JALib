package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.CSharpException;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;
import lombok.Cleanup;

public class ExceptionReport extends RequestPacket {

    public String name;
    public String versionString;
    public String version;
    public String deviceModel;
    public String operatingSystem;
    public String scene;
    public int hashCode;
    public CSharpException exception;


    @Override
    public void getData(Connection connection, ByteArrayDataInput input) throws Exception {
        name = input.readUTF();
        versionString = input.readUTF();
        version = input.readUTF();
        deviceModel = input.readUTF();
        operatingSystem = input.readUTF();
        scene = input.readUTF();
        hashCode = input.readInt();
        exception = new CSharpException(input);
    }

    @Override
    public void getBinary(ByteArrayDataOutput output) throws Exception {
        // TODO: Implement this method
        throw new UnsupportedOperationException("Not implemented yet.");
    }
}
