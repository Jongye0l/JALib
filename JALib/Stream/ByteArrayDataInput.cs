using System;
using System.Text;
using System.Threading.Tasks;
using JALib.Core;
using JALib.Tools;

namespace JALib.Stream;

public class ByteArrayDataInput : IDisposable {
    private byte[] data;
    private System.IO.Stream stream;
    private int cur;
    private JAMod mod;

    public ByteArrayDataInput(byte[] data, JAMod mod = null) {
        this.data = data;
        this.mod = mod;
    }

    public ByteArrayDataInput(System.IO.Stream stream, JAMod mod = null) {
        this.stream = stream;
        this.mod = mod;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
    
    public string ReadUTF() {
        byte[] buffer = ReadBytes();
        return buffer == null ? null : Encoding.UTF8.GetString(buffer);
    }

    public int ReadInt() {
        return (ReadByte() << 24) + (ReadByte() << 16) + (ReadByte() << 8) + (ReadByte() << 0);
    }

    public long ReadLong() {
        return ((long) ReadByte() << 56) +
               ((long) (ReadByte()&255) << 48) +
               ((long) (ReadByte()&255) << 40) +
               ((long) (ReadByte()&255) << 32) +
               ((long) (ReadByte()&255) << 24) +
               (ReadByte()&255 << 16) +
               (ReadByte()&255 << 8) +
               (ReadByte()&255 << 0);
    }

    public bool ReadBoolean() {
        return ReadByte() != 0;
    }

    public float ReadFloat() {
        byte[] data = this.data ?? ReadBytes(4);
        Array.Reverse(data, cur, 4);
        float f = BitConverter.ToSingle(data, cur);
        if(this.data != null) cur += 4;
        return f;
    }

    public double ReadDouble() {
        byte[] data = this.data ?? ReadBytes(8);
        Array.Reverse(data, cur, 8);
        double d = BitConverter.ToDouble(data, cur);
        if(this.data != null) cur += 8;
        return d;
    }

    public byte ReadByte() {
        return data == null ? (byte) stream.ReadByte() : data[cur++];
    }

    public short ReadShort() {
        return (short) ((ReadByte() << 8) + ReadByte());
    }

    public decimal ReadDecimal() {
        int[] bits = new int[4];
        for(int i = 0; i < 4; i++) bits[i] = ReadInt();
        return new decimal(bits);
    }
    
    public ushort ReadUShort() {
        return (ushort) ((ReadByte() << 8) + ReadByte());
    }
    
    public uint ReadUInt() {
        return (uint) ((ReadByte() << 24) + (ReadByte() << 16) + (ReadByte() << 8) + (ReadByte() << 0));
    }
    
    public ulong ReadULong() {
        return ((ulong) ReadByte() << 56) +
               ((ulong) (ReadByte()&255) << 48) +
               ((ulong) (ReadByte()&255) << 40) +
               ((ulong) (ReadByte()&255) << 32) +
               ((ulong) (ReadByte()&255) << 24) +
               ((ulong) (ReadByte()&255) << 16) +
               ((ulong) (ReadByte()&255) << 8) +
               ((ulong) (ReadByte()&255) << 0);
    }
    
    public sbyte ReadSByte() {
        return (sbyte) ReadByte();
    }
    
    public char ReadChar() {
        return (char) ((ReadByte() << 8) + ReadByte());
    }
    
    public byte[] ReadBytes() {
        byte[] buffer = new byte[ReadInt()];
        for(int i = 0; i < buffer.Length; i++) buffer[i] = ReadByte();
        return buffer;
    }

    public byte[] ReadBytes(int length) {
        byte[] buffer = new byte[length];
        for(int i = 0; i < buffer.Length; i++) buffer[i] = ReadByte();
        return buffer;
    }
}