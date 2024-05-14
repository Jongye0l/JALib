package kr.jongyeol.jaServer.exception;

public class PacketNotFound extends RuntimeException {
    public PacketNotFound(String packetName) {
        super("Unknown Packet : " + packetName);
    }
}
