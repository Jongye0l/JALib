package kr.jongyeol.jaServer.packet;

import kr.jongyeol.jaServer.exception.ByteDataNotFound;

import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;

public class ByteArrayDataInput {
    private byte[] data;
    private ByteBuffer buffer;
    private int cur = 0;

    public ByteArrayDataInput(byte[] data) {
        this.data = data;
        if(data.length == 0) throw new ByteDataNotFound();
    }

    public ByteArrayDataInput(ByteBuffer buffer) {
        this.buffer = buffer;
    }

    public String readUTF() {
        byte[] buffer = readBytes();
        if(buffer == null) return null;
        return new String(buffer, StandardCharsets.UTF_8);
    }


    public int readInt() {
        return ((readByte() << 24) + ((readByte()&0xFF) << 16) + ((readByte()&0xFF) << 8) + (readByte()&0xFF));
    }

    public long readLong() {
        return (((long) readByte() << 56) +
            ((long) (readByte()&0xFF) << 48) +
            ((long) (readByte()&0xFF) << 40) +
            ((long) (readByte()&0xFF) << 32) +
            ((long) (readByte()&0xFF) << 24) +
            ((readByte()&0xFF) << 16) +
            ((readByte()&0xFF) << 8) +
            (readByte()&0xFF));
    }

    public boolean readBoolean() {
        return readByte() != 0;
    }

    public float readFloat() {
        return Float.intBitsToFloat(readInt());
    }

    public double readDouble() {
        return Double.longBitsToDouble(readLong());
    }

    public byte readByte() {
        return data == null ? buffer.get() : data[cur++];
    }

    public short readShort() {
        return (short) ((readByte() << 8) + (readByte()&0xFF));
    }

    public byte[] readBytes() {
        int size = readInt();
        if(size == -1) return null;
        byte[] data = new byte[size];
        for(int i = 0; i < size; i++) data[i] = readByte();
        return data;
    }

    public void close() {
        data = null;
    }
}
