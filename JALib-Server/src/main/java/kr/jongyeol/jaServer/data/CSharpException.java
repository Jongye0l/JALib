package kr.jongyeol.jaServer.data;

import kr.jongyeol.jaServer.packet.ByteArrayDataInput;

public class CSharpException {
    public String name;
    public String message;
    public String stackTrace;
    public CSharpException innerException;

    public CSharpException(String name, String message, String stackTrace, CSharpException innerException) {
        this.name = name;
        this.message = message;
        this.stackTrace = stackTrace;
        this.innerException = innerException;
    }

    public CSharpException(ByteArrayDataInput input) {
        name = input.readUTF();
        message = input.readUTF();
        stackTrace = input.readUTF();
        if(input.readBoolean()) innerException = new CSharpException(input);
    }

    public String toString() {
        return name + " " + message + " " + stackTrace + " " + innerException;
    }
}
