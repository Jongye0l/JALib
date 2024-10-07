using System;

namespace JALib.JAException;

public class AlreadyWorkedException(string message) : Exception(message);