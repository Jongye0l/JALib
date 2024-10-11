namespace JALib.JAException;

class ResponseException(string packet, string responseMessage) : Exception($"Response error in {nameof(packet)} {packet}: {responseMessage}") {
    public readonly string Packet = packet;
    public readonly string ResponseMessage = responseMessage;
}