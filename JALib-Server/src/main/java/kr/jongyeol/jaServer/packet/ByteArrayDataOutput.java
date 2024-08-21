package kr.jongyeol.jaServer.packet;

import lombok.SneakyThrows;

import java.nio.charset.StandardCharsets;
import java.util.Arrays;

public class ByteArrayDataOutput {
    private byte[] buf;
    private int count = 0;

    public ByteArrayDataOutput() {
        buf = new byte[32];
    }

    private void ensureCapacity(int minCapacity) {
        int oldCapacity = buf.length;
        int minGrowth = minCapacity - oldCapacity;
        if(minGrowth > 0) buf = Arrays.copyOf(buf, Math.max(buf.length + 16, minCapacity));
    }

    public void writeUTF(String value) {
        writeBytes(value == null ? null : value.getBytes(StandardCharsets.UTF_8));
    }

    public void writeInt(int value) {
        ensureCapacity(count + 4);
        writeIntBypass(value);
    }

    private void writeIntBypass(int value) {
        buf[count++] = (byte) (value >>> 24);
        buf[count++] = (byte) (value >>> 16);
        buf[count++] = (byte) (value >>> 8);
        buf[count++] = (byte) (value);
    }

    public void writeLong(long value) {
        ensureCapacity(count + 8);
        buf[count++] = (byte) (value >>> 56);
        buf[count++] = (byte) (value >>> 48);
        buf[count++] = (byte) (value >>> 40);
        buf[count++] = (byte) (value >>> 32);
        buf[count++] = (byte) (value >>> 24);
        buf[count++] = (byte) (value >>> 16);
        buf[count++] = (byte) (value >>> 8);
        buf[count++] = (byte) (value);
    }

    public void writeBoolean(boolean value) {
        ensureCapacity(count + 1);
        buf[count++] = value ? (byte) 1 : 0;
    }

    public void writeFloat(float value) {
        writeInt(Float.floatToIntBits(value));
    }

    public void writeDouble(double value) {
        writeLong(Double.doubleToLongBits(value));
    }

    @SneakyThrows
    public void writeByte(byte value) {
        ensureCapacity(count + 1);
        buf[count++] = value;
    }

    public void writeShort(short value) {
        ensureCapacity(count + 2);
        buf[count++] = (byte) (value >>> 8);
        buf[count++] = (byte) (value);
    }

    @SneakyThrows
    public void writeBytes(byte[] value) {
        if(value == null) {
            writeInt(-1);
            return;
        }
        ensureCapacity(count + value.length + 4);
        writeIntBypass(value.length);
        for(byte v : value) buf[count++] = v;
    }

    public void writeBytesOnly(byte[] value) {
        ensureCapacity(count + value.length);
        for(byte v : value) buf[count++] = v;
    }

    public void writeBytes(byte[] value, int offset, int length) {
        ensureCapacity(count + length + 4);
        writeIntBypass(length);
        for(int i = offset; i < offset + length; i++) buf[count++] = value[i];
    }

    public byte[] toByteArray() {
        return Arrays.copyOf(buf, count);
    }

    public void close() {
        buf = null;
    }
}
