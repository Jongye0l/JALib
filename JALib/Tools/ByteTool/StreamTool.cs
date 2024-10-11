using System.IO;
using System.Text;

namespace JALib.Tools.ByteTool;

public static class StreamTool {
    public static byte[] ReadBytes(this Stream stream, int count) {
        if(count == -1) return null;
        byte[] buffer = new byte[count];
        for(int i = 0; i < count; i++) buffer[i] = stream.ReadByteSafe();
        return buffer;
    }

    public static byte[] ReadBytes(this Stream stream) {
        int count = stream.ReadInt();
        return ReadBytes(stream, count);
    }

    public static byte ReadByteSafe(this Stream stream) {
        int value = stream.ReadByte();
        if(value == -1) throw new EndOfStreamException();
        return (byte) value;
    }

    public static sbyte ReadSByte(this Stream stream) => (sbyte) stream.ReadByteSafe();

    public static short ReadShort(this Stream stream) => (short) ((stream.ReadByteSafe() << 8) + stream.ReadByteSafe());

    public static ushort ReadUShort(this Stream stream) => (ushort) ((stream.ReadByteSafe() << 8) + stream.ReadByteSafe());

    public static int ReadInt(this Stream stream) => (stream.ReadByteSafe() << 24) + (stream.ReadByteSafe() << 16) + (stream.ReadByteSafe() << 8) + stream.ReadByteSafe();

    public static uint ReadUInt(this Stream stream) => (uint) ((stream.ReadByteSafe() << 24) + (stream.ReadByteSafe() << 16) + (stream.ReadByteSafe() << 8) + stream.ReadByteSafe());

    public static long ReadLong(this Stream stream) =>
        ((long) stream.ReadByteSafe() << 56) +
        ((long) (stream.ReadByteSafe() & 255) << 48) +
        ((long) (stream.ReadByteSafe() & 255) << 40) +
        ((long) (stream.ReadByteSafe() & 255) << 32) +
        ((long) (stream.ReadByteSafe() & 255) << 24) +
        ((long) (stream.ReadByteSafe() & 255) << 16) +
        ((long) (stream.ReadByteSafe() & 255) << 8) +
        ((long) (stream.ReadByteSafe() & 255) << 0);

    public static ulong ReadULong(this Stream stream) =>
        ((ulong) stream.ReadByteSafe() << 56) +
        ((ulong) (stream.ReadByteSafe() & 255) << 48) +
        ((ulong) (stream.ReadByteSafe() & 255) << 40) +
        ((ulong) (stream.ReadByteSafe() & 255) << 32) +
        ((ulong) (stream.ReadByteSafe() & 255) << 24) +
        ((ulong) (stream.ReadByteSafe() & 255) << 16) +
        ((ulong) (stream.ReadByteSafe() & 255) << 8) +
        ((ulong) (stream.ReadByteSafe() & 255) << 0);

    public static float ReadFloat(this Stream stream) {
        byte[] data = new byte[4];
        for(int i = 0; i < 4; i++) data[i] = stream.ReadByteSafe();
        Array.Reverse(data);
        return BitConverter.ToSingle(data, 0);
    }

    public static double ReadDouble(this Stream stream) {
        byte[] data = new byte[8];
        for(int i = 0; i < 8; i++) data[i] = stream.ReadByteSafe();
        Array.Reverse(data);
        return BitConverter.ToDouble(data, 0);
    }

    public static decimal ReadDecimal(this Stream stream) {
        int[] bits = new int[4];
        for(int i = 0; i < 4; i++) bits[i] = ReadInt(stream);
        return new decimal(bits);
    }

    public static bool ReadBoolean(this Stream stream) => stream.ReadByteSafe() != 0;

    public static char ReadChar(this Stream stream) => (char) stream.ReadShort();

    public static string ReadUTF(this Stream stream) {
        byte[] buffer = ReadBytes(stream);
        return buffer == null ? null : Encoding.UTF8.GetString(buffer);
    }

    public static object ReadObject(this Stream stream, Type type, bool declearing = false, bool includeClass = false, uint? version = null, bool nullable = true) =>
        ByteTools.ToObject(stream, type, declearing, includeClass, version, nullable);

    public static T ReadObject<T>(this Stream stream, bool declearing = false, bool includeClass = false, uint? version = null, bool nullable = true) =>
        ByteTools.ToObject<T>(stream, declearing, includeClass, version, nullable);

    public static object ReadObject(this Stream stream, object obj, Type type, bool declearing = false, uint? version = null) => obj.ChangeData(stream, type, declearing, version);

    public static T ReadObject<T>(this Stream stream, T obj, bool declearing = false, uint? version = null) => obj.ChangeData(stream, declearing, version);

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
        stream.Write(data);
    }

    public static void WriteDouble(this Stream stream, double value) {
        byte[] data = BitConverter.GetBytes(value);
        Array.Reverse(data);
        stream.Write(data);
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

    public static void WriteObject(this Stream stream, object obj, bool declearing = false, bool includeClass = false, uint? version = null, bool nullable = true) {
        obj.ToBytes(stream, declearing, includeClass, version, nullable);
    }

    public static void WriteObject(this Stream stream, object obj, Type type, bool declearing = false, bool includeClass = false, uint? version = null, bool nullable = true) {
        obj.ToBytes(stream, type, declearing, includeClass, version, nullable);
    }
}