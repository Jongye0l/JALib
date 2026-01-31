namespace JALib.JAException;

public class PacketRunningException : Exception {
    public PacketRunningException(string message) : base(message) {
    }
    
    public PacketRunningException(string message, Exception innerException) : base(message, innerException) {
    }
}