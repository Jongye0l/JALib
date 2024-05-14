package kr.jongyeol.jaServer.packet;

import kr.jongyeol.jaServer.exception.ByteDataNotFound;

import java.nio.charset.StandardCharsets;

public class ByteArrayDataInput {
    private byte[] data;
    private int cur = 0;

    public ByteArrayDataInput(byte[] data) {
        this.data = data;
        if(data.length == 0) throw new ByteDataNotFound();
    }

    public String readUTF() {
        return new String(readBytes(), StandardCharsets.UTF_8);
    }

    public int readInt() {
        return ((data[cur++] << 24) + ((data[cur++]&0xFF) << 16) + ((data[cur++]&0xFF) << 8) + (data[cur++]&0xFF));
    }

    public long readLong() {
        return (((long) data[cur++] << 56) +
            ((long) (data[cur++]&0xFF) << 48) +
            ((long) (data[cur++]&0xFF) << 40) +
            ((long) (data[cur++]&0xFF) << 32) +
            ((long) (data[cur++]&0xFF) << 24) +
            ((data[cur++]&0xFF) << 16) +
            ((data[cur++]&0xFF) << 8) +
            (data[cur++]&0xFF));
    }

    public boolean readBoolean() {
        return data[cur++] != 0;
    }

    public float readFloat() {
        return Float.intBitsToFloat(readInt());
    }

    public double readDouble() {
        return Double.longBitsToDouble(readLong());
    }

    public byte readByte() {
        return data[cur++];
    }

    public short readShort() {
        return (short) ((data[cur++] << 8) + (data[cur++]&0xFF));
    }

    public byte[] readBytes() {
        int size = readInt();
        byte[] data = new byte[size];
        for(int i = 0; i < size; i++) data[i] = this.data[cur++];
        return data;
    }

    public void close() {
        data = null;
    }
}
