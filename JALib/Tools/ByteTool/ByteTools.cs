using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JALib.Stream;
using UnityEngine;

namespace JALib.Tools.ByteTool;

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
        CheckArgument(bytes.Length - start, 16);
        return new decimal(new[] {
            bytes.ToInt(start),
            bytes.ToInt(start + 4),
            bytes.ToInt(start + 8),
            bytes.ToInt(start + 12)
        });
    }

    public static int GetMinCount(Type type, bool declearing) {
        int count = 0;
        foreach(MemberInfo member in type.Members().Where(member => !declearing || member.DeclaringType == type)) {
            if(member.GetCustomAttribute<DataExcludeAttribute>() != null) continue;
            switch(member) {
                case FieldInfo field:
                    if(field.IsStatic || field.IsInitOnly) continue;
                    count += field.FieldType switch {
                        { } t when t == typeof(long) => 8,
                        { } t when t == typeof(int) => 4,
                        { } t when t == typeof(short) => 2,
                        { } t when t == typeof(byte) => 1,
                        { } t when t == typeof(bool) => 1,
                        { } t when t == typeof(string) => 4,
                        { } t when t == typeof(byte[]) => 4,
                        { } t when t == typeof(decimal) => 16,
                        { } t when t == typeof(float) => 4,
                        { } t when t == typeof(double) => 8,
                        { } t when t == typeof(ushort) => 2,
                        { } t when t == typeof(uint) => 4,
                        { } t when t == typeof(ulong) => 8,
                        { } t when t == typeof(sbyte) => 1,
                        { } t when t == typeof(Vector2) => 8,
                        { } t when t == typeof(Vector3) => 12,
                        { } t when t == typeof(Vector4) => 16,
                        { } t when t == typeof(Quaternion) => 16,
                        { } t when t == typeof(Color) => 4,
                        _ => 1
                    };
                    break;
                case PropertyInfo property:
                    if(!property.CanWrite || property.GetSetMethod(true).IsStatic || property.Name == "Item") continue;
                    count += property.PropertyType switch {
                        { } t when t == typeof(long) => 8,
                        { } t when t == typeof(int) => 4,
                        { } t when t == typeof(short) => 2,
                        { } t when t == typeof(byte) => 1,
                        { } t when t == typeof(bool) => 1,
                        { } t when t == typeof(string) => 4,
                        { } t when t == typeof(byte[]) => 4,
                        { } t when t == typeof(decimal) => 16,
                        { } t when t == typeof(float) => 4,
                        { } t when t == typeof(double) => 8,
                        { } t when t == typeof(ushort) => 2,
                        { } t when t == typeof(uint) => 4,
                        { } t when t == typeof(ulong) => 8,
                        { } t when t == typeof(sbyte) => 1,
                        { } t when t == typeof(Vector2) => 8,
                        { } t when t == typeof(Vector3) => 12,
                        { } t when t == typeof(Vector4) => 16,
                        { } t when t == typeof(Quaternion) => 16,
                        { } t when t == typeof(Color) => 4,
                        _ => 1
                    };
                    break;
            }
        }
        return count;
    }

    public static T ToObject<T>(this byte[] bytes, int start = 0, bool declearing = false) {
        return (T) ToObject(bytes, typeof(T), start, declearing);
    }

    public static object ToObject(this byte[] bytes, Type type, int start = 0, bool declearing = true) {
        CheckArgument(bytes.Length - start, GetMinCount(type, declearing));
        using ByteArrayDataInput input = new(bytes);
        while(start-- <= 0) input.ReadByte();
        return ToObject(input, type, declearing);
    }

    public static object ToObject(ByteArrayDataInput input, Type type, bool declearing = true) {
        if(type == typeof(ICollection<>)) {
            int size = input.ReadInt();
            Type elementType = type.GetGenericArguments()[0];
            if(type.IsArray) {
                Array array = Array.CreateInstance(elementType, size);
                for(int i = 0; i < size; i++) array.SetValue(ToObject(input, elementType), i);
                return array;
            }
            ConstructorInfo constructorInfo;
            object collection = (constructorInfo = type.Constructor(typeof(int))) != null ? constructorInfo.Invoke(new object[] { size }) : type.New();
            MethodInfo addMethod = type.Method("Add");
            for(int i = 0; i < size; i++) addMethod.Invoke(collection, new[] { ToObject(input, elementType) });
            return collection;
        }
        object obj = Activator.CreateInstance(type);
        foreach(MemberInfo member in type.Members().Where(member => !declearing || member.DeclaringType == type)) {
            if(member.GetCustomAttribute<DataExcludeAttribute>() != null) continue;
            if(member is FieldInfo field) {
                if(field.IsStatic || field.IsInitOnly) continue;
                field.SetValue(obj, field.FieldType switch {
                    { } t when t == typeof(long) => input.ReadLong(),
                    { } t when t == typeof(int) => input.ReadInt(),
                    { } t when t == typeof(short) => input.ReadShort(),
                    { } t when t == typeof(byte) => input.ReadByte(),
                    { } t when t == typeof(bool) => input.ReadBoolean(),
                    { } t when t == typeof(string) => input.ReadUTF(),
                    { } t when t == typeof(byte[]) => input.ReadBytes(),
                    { } t when t == typeof(decimal) => input.ReadDecimal(),
                    { } t when t == typeof(float) => input.ReadFloat(),
                    { } t when t == typeof(double) => input.ReadDouble(),
                    { } t when t == typeof(ushort) => input.ReadUShort(),
                    { } t when t == typeof(uint) => input.ReadUInt(),
                    { } t when t == typeof(ulong) => input.ReadULong(),
                    { } t when t == typeof(sbyte) => input.ReadSByte(),
                    _ => ToObject(input, field.FieldType)
                });
            } else if(member is PropertyInfo property) {
                if(!property.CanWrite || property.GetSetMethod(true).IsStatic || property.Name == "Item") continue;
                property.SetValue(obj, property.PropertyType switch {
                    { } t when t == typeof(long) => input.ReadLong(),
                    { } t when t == typeof(int) => input.ReadInt(),
                    { } t when t == typeof(short) => input.ReadShort(),
                    { } t when t == typeof(byte) => input.ReadByte(),
                    { } t when t == typeof(bool) => input.ReadBoolean(),
                    { } t when t == typeof(string) => input.ReadUTF(),
                    { } t when t == typeof(byte[]) => input.ReadBytes(),
                    { } t when t == typeof(decimal) => input.ReadDecimal(),
                    { } t when t == typeof(float) => input.ReadFloat(),
                    { } t when t == typeof(double) => input.ReadDouble(),
                    { } t when t == typeof(ushort) => input.ReadUShort(),
                    { } t when t == typeof(uint) => input.ReadUInt(),
                    { } t when t == typeof(ulong) => input.ReadULong(),
                    { } t when t == typeof(sbyte) => input.ReadSByte(),
                    _ => ToObject(input, property.PropertyType)
                });
            }
        }
        return obj;
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

    public static byte[] ToBytes(this object value, bool declearing = true) {
        using ByteArrayDataOutput output = new();
        ToBytes(value, output, declearing);
        return output.ToByteArray();
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

    public static void ToBytes(this object value, ByteArrayDataOutput output, bool declearing = true) {
        if(value.GetType() == typeof(ICollection<>)) {
            output.WriteInt(value.GetValue<int>("Count"));
            foreach(object obj in (IEnumerable) value) ToBytes(obj, output, declearing);
            return;
        }
        foreach(MemberInfo member in value.GetType().Members().Where(member => !declearing || member.DeclaringType == value.GetType())) {
            if(member.GetCustomAttribute<DataExcludeAttribute>() != null) continue;
            if(member is FieldInfo field) {
                if(field.IsStatic || field.IsInitOnly) continue;
                switch(field.GetValue(value)) {
                    case long l:
                        output.WriteLong(l);
                        break;
                    case int i:
                        output.WriteInt(i);
                        break;
                    case short s:
                        output.WriteShort(s);
                        break;
                    case byte b:
                        output.WriteByte(b);
                        break;
                    case bool b:
                        output.WriteBoolean(b);
                        break;
                    case string s:
                        output.WriteUTF(s);
                        break;
                    case byte[] b:
                        output.WriteBytes(b);
                        break;
                    case decimal d:
                        output.WriteDecimal(d);
                        break;
                    case float f:
                        output.WriteFloat(f);
                        break;
                    case double d:
                        output.WriteDouble(d);
                        break;
                    case ushort u:
                        output.WriteUShort(u);
                        break;
                    case uint u:
                        output.WriteUInt(u);
                        break;
                    case ulong u:
                        output.WriteULong(u);
                        break;
                    case sbyte s:
                        output.WriteSByte(s);
                        break;
                    default:
                        ToBytes(field.GetValue(value), output, declearing);
                        break;
                }
            } else if(member is PropertyInfo property) {
                if(!property.CanWrite || property.GetSetMethod(true).IsStatic || property.Name == "Item") continue;
                switch(property.GetValue(value)) {
                    case long l:
                        output.WriteLong(l);
                        break;
                    case int i:
                        output.WriteInt(i);
                        break;
                    case short s:
                        output.WriteShort(s);
                        break;
                    case byte b:
                        output.WriteByte(b);
                        break;
                    case bool b:
                        output.WriteBoolean(b);
                        break;
                    case string s:
                        output.WriteUTF(s);
                        break;
                    case byte[] b:
                        output.WriteBytes(b);
                        break;
                    case decimal d:
                        output.WriteDecimal(d);
                        break;
                    case float f:
                        output.WriteFloat(f);
                        break;
                    case double d:
                        output.WriteDouble(d);
                        break;
                    case ushort u:
                        output.WriteUShort(u);
                        break;
                    case uint u:
                        output.WriteUInt(u);
                        break;
                    case ulong u:
                        output.WriteULong(u);
                        break;
                    case sbyte s:
                        output.WriteSByte(s);
                        break;
                    default:
                        ToBytes(property.GetValue(value), output, declearing);
                        break;
                }
            }
        }
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