using System;
using System.IO;
using System.Text;

namespace JALib.Tools.ByteTool;

public static class StreamTool {
    public static byte[] ReadBytes(this Stream stream, int count) {
        if(count == -1) return null;
        byte[] buffer = new byte[count];
        for(int i = 0; i < count; i++) buffer[i] = (byte) stream.ReadByte();
        return buffer;
    }

    public static byte[] ReadBytes(this Stream stream) {
        int count = stream.ReadInt();
        return ReadBytes(stream, count);
    }

    public static sbyte ReadSByte(this Stream stream) {
        return (sbyte) stream.ReadByte();
    }

    public static short ReadShort(this Stream stream) {
        return (short) ((stream.ReadByte() << 8) + stream.ReadByte());
    }

    public static ushort ReadUShort(this Stream stream) {
        return (ushort) ((stream.ReadByte() << 8) + stream.ReadByte());
    }

    public static int ReadInt(this Stream stream) {
        return (stream.ReadByte() << 24) + (stream.ReadByte() << 16) + (stream.ReadByte() << 8) + stream.ReadByte();
    }

    public static uint ReadUInt(this Stream stream) {
        return (uint) ((stream.ReadByte() << 24) + (stream.ReadByte() << 16) + (stream.ReadByte() << 8) + stream.ReadByte());
    }

    public static long ReadLong(this Stream stream) {
        return ((long) stream.ReadByte() << 56) +
               ((long) (stream.ReadByte() & 255) << 48) +
               ((long) (stream.ReadByte() & 255) << 40) +
               ((long) (stream.ReadByte() & 255) << 32) +
               ((long) (stream.ReadByte() & 255) << 24) +
               ((long) (stream.ReadByte() & 255) << 16) +
               ((long) (stream.ReadByte() & 255) << 8) +
               ((long) (stream.ReadByte() & 255) << 0);
    }

    public static ulong ReadULong(this Stream stream) {
        return ((ulong) stream.ReadByte() << 56) +
               ((ulong) (stream.ReadByte() & 255) << 48) +
               ((ulong) (stream.ReadByte() & 255) << 40) +
               ((ulong) (stream.ReadByte() & 255) << 32) +
               ((ulong) (stream.ReadByte() & 255) << 24) +
               ((ulong) (stream.ReadByte() & 255) << 16) +
               ((ulong) (stream.ReadByte() & 255) << 8) +
               ((ulong) (stream.ReadByte() & 255) << 0);
    }

    public static float ReadFloat(this Stream stream) {
        byte[] data = new byte[4];
        for(int i = 0; i < 4; i++) data[i] = (byte) stream.ReadByte();
        Array.Reverse(data);
        return BitConverter.ToSingle(data, 0);
    }

    public static double ReadDouble(this Stream stream) {
        byte[] data = new byte[8];
        for(int i = 0; i < 8; i++) data[i] = (byte) stream.ReadByte();
        Array.Reverse(data);
        return BitConverter.ToDouble(data, 0);
    }

    public static decimal ReadDecimal(this Stream stream) {
        int[] bits = new int[4];
        for(int i = 0; i < 4; i++) bits[i] = ReadInt(stream);
        return new decimal(bits);
    }

    public static bool ReadBoolean(this Stream stream) {
        return stream.ReadByte() != 0;
    }

    public static char ReadChar(this Stream stream) {
        return (char) stream.ReadShort();
    }

    public static string ReadUTF(this Stream stream) {
        return Encoding.UTF8.GetString(ReadBytes(stream));
    }

    public static object ReadObject(this Stream stream, Type type, bool declearing = false, bool includeClass = false, uint? version = null) {
        return ByteTools.ToObject(stream, type, declearing, includeClass, version);
    }

    public static T ReadObject<T>(this Stream stream, bool declearing = false, bool includeClass = false, uint? version = null) {
        return ByteTools.ToObject<T>(stream, declearing, includeClass, version);
    }

    public static object ReadObject(this Stream stream, object obj, Type type, bool declearing = false, uint? version = null) {
        return obj.ChangeData(stream, type, declearing, version);
    }

    public static T ReadObject<T>(this Stream stream, T obj, bool declearing = false, uint? version = null) {
        return obj.ChangeData(stream, declearing, version);
    }

    public static void WriteBytes(this Stream stream, byte[] buffer) {
        stream.WriteInt(buffer.Length);
        stream.Write(buffer);
    }

    public static void WriteSByte(this Stream stream, sbyte value) {
        stream.WriteByte((byte) value);
    }

    public static void WriteShort(this Stream stream, short value) {
        stream.WriteByte((byte) (value >> 8));
        stream.WriteByte((byte) value);
    }

    public static void WriteUShort(this Stream stream, ushort value) {
        stream.WriteByte((byte) (value >> 8));
        stream.WriteByte((byte) value);
    }

    public static void WriteInt(this Stream stream, int value) {
        stream.WriteByte((byte) (value >> 24));
        stream.WriteByte((byte) (value >> 16));
        stream.WriteByte((byte) (value >> 8));
        stream.WriteByte((byte) value);
    }

    public static void WriteUInt(this Stream stream, uint value) {
        stream.WriteByte((byte) (value >> 24));
        stream.WriteByte((byte) (value >> 16));
        stream.WriteByte((byte) (value >> 8));
        stream.WriteByte((byte) value);
    }

    public static void WriteLong(this Stream stream, long value) {
        stream.WriteByte((byte) (value >> 56));
        stream.WriteByte((byte) (value >> 48));
        stream.WriteByte((byte) (value >> 40));
        stream.WriteByte((byte) (value >> 32));
        stream.WriteByte((byte) (value >> 24));
        stream.WriteByte((byte) (value >> 16));
        stream.WriteByte((byte) (value >> 8));
        stream.WriteByte((byte) value);
    }

    public static void WriteULong(this Stream stream, ulong value) {
        stream.WriteByte((byte) (value >> 56));
        stream.WriteByte((byte) (value >> 48));
        stream.WriteByte((byte) (value >> 40));
        stream.WriteByte((byte) (value >> 32));
        stream.WriteByte((byte) (value >> 24));
        stream.WriteByte((byte) (value >> 16));
        stream.WriteByte((byte) (value >> 8));
        stream.WriteByte((byte) value);
    }

    public static void WriteFloat(this Stream stream, float value) {
        byte[] data = BitConverter.GetBytes(value);
        Array.Reverse(data);
        stream.WriteBytes(data);
    }

    public static void WriteDouble(this Stream stream, double value) {
        byte[] data = BitConverter.GetBytes(value);
        Array.Reverse(data);
        stream.WriteBytes(data);
    }

    public static void WriteDecimal(this Stream stream, decimal value) {
        int[] bits = decimal.GetBits(value);
        for(int i = 0; i < 4; i++) WriteInt(stream, bits[i]);
    }

    public static void WriteBoolean(this Stream stream, bool value) {
        stream.WriteByte(value ? (byte) 1 : (byte) 0);
    }

    public static void WriteChar(this Stream stream, char value) {
        stream.WriteShort((short) value);
    }

    public static void WriteUTF(this Stream stream, string value) {
        stream.WriteBytes(Encoding.UTF8.GetBytes(value));
    }

    public static void WriteObject(this Stream stream, object obj, bool declearing = false, bool includeClass = false, uint? version = null) {
        obj.ToBytes(stream, declearing, includeClass, version);
    }

    public static void WriteObject(this Stream stream, object obj, Type type, bool declearing = false, bool includeClass = false, uint? version = null) {
        obj.ToBytes(stream, type, declearing, includeClass, version);
    }
}