using System;
using System.Text;
using System.Threading.Tasks;
using JALib.Core;
using JALib.Tools;

namespace JALib.Stream;

public class ByteArrayDataOutput : IDisposable {
    private byte[] buf;
    private int count;
    private JAMod mod;
    
    public ByteArrayDataOutput(JAMod mod = null) {
        buf = new byte[32];
        this.mod = mod;
    }

    public void Dispose() {
        buf = null;
        mod = null;
    }

    private void EnsureCapacity(int minCapacity) {
        int oldCapacity = buf.Length;
        int minGrowth = minCapacity - oldCapacity;
        if(minGrowth <= 0) return;
        byte[] bytes = new byte[Math.Max(buf.Length + 16, minCapacity)];
        Array.Copy(buf, bytes, count);
        buf = bytes;
    }
    
    public void WriteUTF(string value) {
        WriteBytes(Encoding.UTF8.GetBytes(value));
    }

    public void WriteInt(int value) {
        EnsureCapacity(count + 4);
        WriteIntBypass(value);
    }

    private void WriteIntBypass(int value) {
        buf[count++] = (byte) (value >> 24);
        buf[count++] = (byte) (value >> 16);
        buf[count++] = (byte) (value >> 8);
        buf[count++] = (byte) value;
    }

    public void WriteULong(ulong value) {
        EnsureCapacity(count + 8);
        buf[count++] = (byte) (value >> 56);
        buf[count++] = (byte) (value >> 48);
        buf[count++] = (byte) (value >> 40);
        buf[count++] = (byte) (value >> 32);
        buf[count++] = (byte) (value >> 24);
        buf[count++] = (byte) (value >> 16);
        buf[count++] = (byte) (value >> 8);
        buf[count++] = (byte) value;
    }

    public void WriteLong(long value) {
        EnsureCapacity(count + 8);
        buf[count++] = (byte) (value >> 56);
        buf[count++] = (byte) (value >> 48);
        buf[count++] = (byte) (value >> 40);
        buf[count++] = (byte) (value >> 32);
        buf[count++] = (byte) (value >> 24);
        buf[count++] = (byte) (value >> 16);
        buf[count++] = (byte) (value >> 8);
        buf[count++] = (byte) value;
    }

    public void WriteBoolean(bool value) {
        EnsureCapacity(count + 1);
        buf[count++] = value ? (byte) 1 : (byte) 0;
    }

    public void WriteFloat(float value) {
        EnsureCapacity(count + 4);
        byte[] buffer = BitConverter.GetBytes(value);
        buf[count++] = buffer[3];
        buf[count++] = buffer[2];
        buf[count++] = buffer[1];
        buf[count++] = buffer[0];
    }

    public void WriteDouble(double value) {
        EnsureCapacity(count + 8);
        byte[] buffer = BitConverter.GetBytes(value);
        buf[count++] = buffer[7];
        buf[count++] = buffer[6];
        buf[count++] = buffer[5];
        buf[count++] = buffer[4];
        buf[count++] = buffer[3];
        buf[count++] = buffer[2];
        buf[count++] = buffer[1];
        buf[count++] = buffer[0];
    }

    public void WriteByte(byte value) {
        EnsureCapacity(count + 1);
        buf[count++] = value;
    }

    public void WriteShort(short value) {
        EnsureCapacity(count + 2);
        buf[count++] = (byte) (value >> 8);
        buf[count++] = (byte) value;
    }

    public void WriteBytes(byte[] value) {
        EnsureCapacity(count + value.Length + 4);
        WriteIntBypass(value.Length);
        foreach(byte b in value) buf[count++] = b;
    }
    
    public void WriteUShort(ushort value) {
        EnsureCapacity(count + 2);
        buf[count++] = (byte) (value >> 8);
        buf[count++] = (byte) value;
    }
    
    public void WriteUInt(uint value) {
        EnsureCapacity(count + 4);
        buf[count++] = (byte) (value >> 24);
        buf[count++] = (byte) (value >> 16);
        buf[count++] = (byte) (value >> 8);
        buf[count++] = (byte) value;
    }
    
    public void WriteSByte(sbyte value) {
        EnsureCapacity(count + 1);
        buf[count++] = (byte) value;
    }
    
    public void WriteChar(char value) {
        EnsureCapacity(count + 2);
        buf[count++] = (byte) (value >> 8);
        buf[count++] = (byte) value;
    }
    
    public void WriteDecimal(decimal value) {
        int[] bits = decimal.GetBits(value);
        EnsureCapacity(count + bits.Length * 4);
        foreach(int i in bits) WriteIntBypass(i);
    }

    public byte[] ToByteArray() {
        byte[] data = new byte[count];
        Array.Copy(buf, data, count);
        Dispose();
        return data;
    }
}