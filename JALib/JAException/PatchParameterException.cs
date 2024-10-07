using System;

namespace JALib.JAException;

public class PatchParameterException(string message) : Exception(message);