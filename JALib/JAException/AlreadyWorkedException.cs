using System;

namespace JALib.JAException;

public class AlreadyWorkedException : Exception {
    public AlreadyWorkedException() {}
    public AlreadyWorkedException(string message) : base(message) {}

}