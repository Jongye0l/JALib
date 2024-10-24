using System;

namespace JALib.JAException;

public class PacketRunningException(string message, Exception e) : Exception(message, e);