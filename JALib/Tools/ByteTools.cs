using System;

namespace JALib.Tools;

public static class ByteTools {
    public static short ToShort(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 2);
        return (short) (bytes[start] << 8 | bytes[start + 1]);
    }
    
    public static ushort ToUShort(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 2);
        return (ushort) (bytes[start] << 8 | bytes[start + 1]);
    }
    
    public static int ToInt(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 4);
        return bytes[start] << 24 | bytes[start + 1] << 16 | bytes[start + 2] << 8 | bytes[start + 3];
    }
    
    public static uint ToUInt(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 4);
        return (uint) (bytes[start] << 24 | bytes[start + 1] << 16 | bytes[start + 2] << 8 | bytes[start + 3]);
    }
    
    public static long ToLong(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 8);
        return (long) bytes[start] << 56 | 
               (long) (bytes[start + 1]&255) << 48 | 
               (long) (bytes[start + 2]&255) << 40 | 
               (long) (bytes[start + 3]&255) << 32 | 
               (long) (bytes[start + 4]&255) << 24 | 
               (uint) (bytes[start + 5]&255 << 16) | 
               (uint) (bytes[start + 6]&255 << 8) | 
               (uint) (bytes[start + 7]&255 << 0);
    }
    
    public static ulong ToULong(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 8);
        return (ulong) bytes[start] << 56 | 
               (ulong) (bytes[start + 1]&255) << 48 | 
               (ulong) (bytes[start + 2]&255) << 40 | 
               (ulong) (bytes[start + 3]&255) << 32 | 
               (ulong) (bytes[start + 4]&255) << 24 | 
               (uint) (bytes[start + 5]&255 << 16) | 
               (uint) (bytes[start + 6]&255 << 8) | 
               (uint) (bytes[start + 7]&255 << 0);
    }
    
    public static float ToFloat(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 4);
        byte[] buffer = new byte[4];
        Array.Copy(bytes, start, buffer, 0, 4);
        return BitConverter.ToSingle(buffer.Reverse(), 0);
    }
    
    public static double ToDouble(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 8);
        byte[] buffer = new byte[8];
        Array.Copy(bytes, start, buffer, 0, 8);
        return BitConverter.ToDouble(buffer.Reverse(), 0);
    }
    
    public static decimal ToDecimal(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 8);
        return new decimal(new int[] {
            bytes.ToInt(start),
            bytes.ToInt(start + 4),
            bytes.ToInt(start + 8),
            bytes.ToInt(start + 12)
        });
    }

    private static void CheckArgument(int length, int require) {
        if(length < require) throw new ArgumentException("Byte array must be at least " + length + " bytes long");
    }
    
    public static byte[] ToBytes(this short value) {
        byte[] buffer = new byte[2];
        value.ToBytes(buffer);
        return buffer;
    }
    
    public static byte[] ToBytes(this ushort value) {
        byte[] buffer = new byte[2];
        value.ToBytes(buffer);
        return buffer;
    }
    
    public static byte[] ToBytes(this int value) {
        byte[] buffer = new byte[4];
        value.ToBytes(buffer);
        return buffer;
    }
    
    public static byte[] ToBytes(this uint value) {
        byte[] buffer = new byte[4];
        value.ToBytes(buffer);
        return buffer;
    }
    
    public static byte[] ToBytes(this long value) {
        byte[] buffer = new byte[8];
        value.ToBytes(buffer);
        return buffer;
    }
    
    public static byte[] ToBytes(this ulong value) {
        byte[] buffer = new byte[8];
        value.ToBytes(buffer);
        return buffer;
    }
    
    public static byte[] ToBytes(this float value) {
        return BitConverter.GetBytes(value).Reverse();
    }
    
    public static byte[] ToBytes(this double value) {
        return BitConverter.GetBytes(value).Reverse();
    }

    public static byte[] ToBytes(this decimal value) {
        byte[] buffer = new byte[16];
        value.ToBytes(buffer);
        return buffer;
    }
    
    public static byte[] Reverse(this byte[] bytes, int start = 0) {
        CheckStart(start, bytes.Length);
        Array.Reverse(bytes, start, bytes.Length - start);
        return bytes;
    }
    
    public static byte[] Reverse(this byte[] bytes, int start, int length) {
        CheckStart(start, bytes.Length);
        Array.Reverse(bytes, start, length);
        return bytes;
    }
    
    public static void ToBytes(this byte value, byte[] buffer, int start = 0) {
        CheckStart(start, buffer.Length);
        buffer[start] = value;
    }
    
    public static void ToBytes(this short value, byte[] buffer, int start = 0) {
        CheckStart(start, buffer.Length);
        buffer[start++] = (byte) (value >> 8);
        buffer[start] = (byte) value;
    }
    
    public static void ToBytes(this ushort value, byte[] buffer, int start = 0) {
        CheckStart(start, buffer.Length);
        buffer[start++] = (byte) (value >> 8);
        buffer[start] = (byte) value;
    }
    
    public static void ToBytes(this int value, byte[] buffer, int start = 0) {
        CheckStart(start, buffer.Length);
        buffer[start++] = (byte) (value >> 24);
        buffer[start++] = (byte) (value >> 16);
        buffer[start++] = (byte) (value >> 8);
        buffer[start] = (byte) value;
    }
    
    public static void ToBytes(this uint value, byte[] buffer, int start = 0) {
        CheckStart(start, buffer.Length);
        buffer[start++] = (byte) (value >> 24);
        buffer[start++] = (byte) (value >> 16);
        buffer[start++] = (byte) (value >> 8);
        buffer[start] = (byte) value;
    }
    
    public static void ToBytes(this long value, byte[] buffer, int start = 0) {
        CheckStart(start, buffer.Length);
        buffer[start++] = (byte) (value >> 56);
        buffer[start++] = (byte) (value >> 48);
        buffer[start++] = (byte) (value >> 40);
        buffer[start++] = (byte) (value >> 32);
        buffer[start++] = (byte) (value >> 24);
        buffer[start++] = (byte) (value >> 16);
        buffer[start++] = (byte) (value >> 8);
        buffer[start] = (byte) value;
    }
    
    public static void ToBytes(this ulong value, byte[] buffer, int start = 0) {
        CheckStart(start, buffer.Length);
        buffer[start++] = (byte) (value >> 56);
        buffer[start++] = (byte) (value >> 48);
        buffer[start++] = (byte) (value >> 40);
        buffer[start++] = (byte) (value >> 32);
        buffer[start++] = (byte) (value >> 24);
        buffer[start++] = (byte) (value >> 16);
        buffer[start++] = (byte) (value >> 8);
        buffer[start] = (byte) value;
    }
    
    private static void CheckStart(int start, int length) {
        if(start < 0 || start >= length) throw new ArgumentException("Start must be within the byte array");
    }
    
    public static void ToBytes(this float value, byte[] buffer, int start = 0) {
        Array.Copy(value.ToBytes(), 0, buffer, start, 4);
    }
    
    public static void ToBytes(this double value, byte[] buffer, int start = 0) {
        Array.Copy(value.ToBytes(), 0, buffer, start, 8);
    }
    
    public static void ToBytes(this decimal value, byte[] buffer, int start = 0) {
        int[] bits = decimal.GetBits(value);
        foreach(int bit in bits) {
            bit.ToBytes(buffer, start);
            start += 4;
        }
    }
}