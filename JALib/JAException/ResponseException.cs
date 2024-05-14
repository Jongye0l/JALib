using System;

namespace JALib.JAException;

internal class ResponseException : Exception {
    public readonly string Packet;
    public readonly string ResponseMessage;
    public ResponseException(string packet, string responseMessage) : base($"Response error in {nameof(packet)} {packet}: {responseMessage}") {
        Packet = packet;
        ResponseMessage = responseMessage;
    }
}