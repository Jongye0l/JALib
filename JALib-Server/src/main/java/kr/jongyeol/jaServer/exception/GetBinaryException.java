package kr.jongyeol.jaServer.exception;

public class GetBinaryException extends Exception {
    public GetBinaryException(String packet, Throwable cause) {
        super("Failed to get binary data from " + packet, cause);
    }
}
