using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JALib.Core;
using JALib.Stream;
using JetBrains.Annotations;
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
               (long) (bytes[start + 1] & 255) << 48 |
               (long) (bytes[start + 2] & 255) << 40 |
               (long) (bytes[start + 3] & 255) << 32 |
               (long) (bytes[start + 4] & 255) << 24 |
               (uint) (bytes[start + 5] & 255 << 16) |
               (uint) (bytes[start + 6] & 255 << 8) |
               (uint) (bytes[start + 7] & 255 << 0);
    }

    public static ulong ToULong(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 8);
        return (ulong) bytes[start] << 56 |
               (ulong) (bytes[start + 1] & 255) << 48 |
               (ulong) (bytes[start + 2] & 255) << 40 |
               (ulong) (bytes[start + 3] & 255) << 32 |
               (ulong) (bytes[start + 4] & 255) << 24 |
               (uint) (bytes[start + 5] & 255 << 16) |
               (uint) (bytes[start + 6] & 255 << 8) |
               (uint) (bytes[start + 7] & 255 << 0);
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

    public static T ToObject<T>(this byte[] bytes, int start = 0, bool declearing = false, bool includeClass = false, uint? version = null) {
        return (T) ToObject(bytes, typeof(T), start, declearing, includeClass, version);
    }

    public static T ToObject<T>(this ByteArrayDataInput input, bool declearing = false, bool includeClass = false, uint? version = null) {
        return (T) ToObject(input, typeof(T), declearing, includeClass, version);
    }

    public static object ToObject(this byte[] bytes, Type type, int start = 0, bool declearing = false, bool includeClass = false, uint? version = null) {
        using ByteArrayDataInput input = new(bytes);
        while(start-- > 0) input.ReadByte();
        return ToObject(input, type, declearing, includeClass, version);
    }

    public static object ToObject(ByteArrayDataInput input, Type type, bool declearing = false, bool includeClass = false, uint? version = null) {
        {
            VersionAttribute ver = type.GetCustomAttribute<VersionAttribute>();
            if(ver != null) version = ver.Version;
            IncludeClassAttribute includeCl = type.GetCustomAttribute<IncludeClassAttribute>();
            DeclearingAttribute declear = type.GetCustomAttribute<DeclearingAttribute>();
            if(includeCl != null && includeCl.CheckCondition(version)) includeClass = true;
            if(declear != null && declear.CheckCondition(version)) declearing = true;
        }
        if(includeClass) type = Type.GetType(input.ReadUTF());
        if(CheckType(type, typeof(ICollection<>)) && type.GetCustomAttribute<IgnoreArrayAttribute>() == null) {
            int size = input.ReadInt();
            if(size == -1) return null;
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
        if(!type.IsValueType && type != typeof(string) && type.GetCustomAttribute<NotNullAttribute>() == null) {
            if(!input.ReadBoolean()) return null;
        }
        if(CheckType(type, typeof(Delegate))) {
            if(!includeClass) type = Type.GetType(input.ReadUTF());
            Type declaringType = Type.GetType(input.ReadUTF());
            MethodInfo method = declaringType.Method(input.ReadUTF());
            object target = null;
            if(input.ReadBoolean()) {
                Type targetType = Type.GetType(input.ReadUTF());
                target = ToObject(input, targetType, declearing);
            }
            return Delegate.CreateDelegate(type, target, method);
        }
        if(CheckType(type, typeof(JAMod))) {
            return JAMod.GetMods(input.ReadUTF());
        }
        if(CheckType(type, typeof(Feature))) {
            string modName = input.ReadUTF();
            string featureName = input.ReadUTF();
            return JAMod.GetMods(modName).Features.Find(feature => feature.Name == featureName);
        }
        object obj = Activator.CreateInstance(type);
        foreach(MemberInfo member in type.Members().Where(member => member is FieldInfo or PropertyInfo && (!declearing || member.DeclaringType == type))) {
            bool skip = false;
            bool memberDeclearing = false;
            foreach(DataAttribute dataAttribute in member.GetCustomAttributes<DataAttribute>()) {
                switch(dataAttribute) {
                    case DataIncludeAttribute include:
                        if(include.CheckCondition(version)) skip = false;
                        break;
                    case DataExcludeAttribute exclude:
                        if(exclude.CheckCondition(version)) skip = true;
                        break;
                    case DummyAttribute dummy:
                        if(dummy.CheckCondition(version))
                            for(int i = 0; i < dummy.Count; i++)
                                input.ReadByte();
                        break;
                    case DeclearingAttribute declea:
                        if(declea.CheckCondition(version)) memberDeclearing = true;
                        break;
                }
            }
            if(skip) continue;
            Type memberType = null;
            if(member is FieldInfo field) {
                if(field.IsStatic || field.IsInitOnly) continue;
                memberType = field.FieldType;
            } else if(member is PropertyInfo property) {
                if(!property.CanWrite || property.GetSetMethod(true).IsStatic || property.Name == "Item") continue;
                memberType = property.PropertyType;
            }
            Type castType = memberType;
            CastAttribute cast = member.GetCustomAttribute<CastAttribute>();
            if(cast != null && cast.CheckCondition(version)) {
                if(cast.Type == null) continue;
                castType = cast.Type;
            }
            {
                IncludeClassAttribute inc = member.GetCustomAttribute<IncludeClassAttribute>();
                if(inc != null && inc.CheckCondition(version)) memberType = Type.GetType(input.ReadUTF());
            }
            JALib.Instance.Log(castType.FullName);
            object value = castType switch {
                not null when castType == typeof(long) => input.ReadLong(),
                not null when castType == typeof(int) => input.ReadInt(),
                not null when castType == typeof(short) => input.ReadShort(),
                not null when castType == typeof(byte) => input.ReadByte(),
                not null when castType == typeof(bool) => input.ReadBoolean(),
                not null when castType == typeof(string) => input.ReadUTF(),
                not null when castType == typeof(byte[]) => input.ReadBytes(),
                not null when castType == typeof(decimal) => input.ReadDecimal(),
                not null when castType == typeof(float) => input.ReadFloat(),
                not null when castType == typeof(double) => input.ReadDouble(),
                not null when castType == typeof(ushort) => input.ReadUShort(),
                not null when castType == typeof(uint) => input.ReadUInt(),
                not null when castType == typeof(ulong) => input.ReadULong(),
                not null when castType == typeof(sbyte) => input.ReadSByte(),
                _ => ToObject(input, castType, memberDeclearing, false, version)
            };
            if(castType != memberType) {
                MethodInfo explicitCast = memberType.Method("op_Explicit", castType);
                MethodInfo implicitCast = castType.Method("op_Implicit", memberType);
                if(explicitCast != null && implicitCast != null) {
                    value = cast.FirstCast switch {
                        FirstCast.Implicit => explicitCast.Invoke(null, value),
                        FirstCast.Explicit => implicitCast.Invoke(null, value),
                        _ => value
                    };
                } else if(explicitCast != null) value = explicitCast.Invoke(null, value);
                else if(implicitCast != null) value = implicitCast.Invoke(null, value);
                else value = Convert.ChangeType(value, memberType);
            }
            if(member is FieldInfo field2) field2.SetValue(obj, value);
            else if(member is PropertyInfo property) property.SetValue(obj, value);
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

    public static byte[] ToBytes(this object value, bool declearing = false) {
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

    public static void ToBytes(this object value, ByteArrayDataOutput output, bool declearing = false, bool includeClass = false, uint? version = null) {
        ToBytes(value, output, value.GetType(), declearing, includeClass, version);
    }

    public static void ToBytes(this object value, ByteArrayDataOutput output, Type type, bool declearing = false, bool includeClass = false, uint? version = null) {
        {
            VersionAttribute ver = type.GetCustomAttribute<VersionAttribute>();
            if(ver != null) version = ver.Version;
            IncludeClassAttribute includeCl = type.GetCustomAttribute<IncludeClassAttribute>();
            DeclearingAttribute declear = type.GetCustomAttribute<DeclearingAttribute>();
            if(includeCl != null && includeCl.CheckCondition(version)) includeClass = true;
            if(declear != null && declear.CheckCondition(version)) declearing = true;
        }
        if(includeClass) output.WriteUTF(type.FullName);
        if(CheckType(type, typeof(ICollection<>)) && type.GetCustomAttribute<IgnoreArrayAttribute>() == null) {
            if(value == null) {
                output.WriteInt(-1);
                return;
            }
            output.WriteInt(value.GetValue<int>("Count"));
            foreach(object obj in (IEnumerable) value) ToBytes(obj, output, declearing);
            return;
        }
        if(!type.IsValueType && type != typeof(string) && type.GetCustomAttribute<NotNullAttribute>() == null) {
            if(value == null) {
                output.WriteBoolean(false);
                return;
            }
            output.WriteBoolean(true);
        }
        if(CheckType(type, typeof(Delegate))) {
            if(!includeClass) output.WriteUTF(type.FullName);
            Delegate del = (Delegate) value;
            output.WriteUTF(del.Method.DeclaringType.FullName);
            output.WriteUTF(del.Method.Name);
            if(del.Target == null) {
                output.WriteBoolean(false);
                return;
            }
            output.WriteBoolean(true);
            output.WriteUTF(del.Target.GetType().FullName);
            del.Target.ToBytes(output, declearing);
            return;
        }
        if(CheckType(type, typeof(JAMod))) {
            JAMod mod = (JAMod) value;
            output.WriteUTF(mod.Name);
            return;
        }
        if(CheckType(type, typeof(Feature))) {
            Feature feature = (Feature) value;
            output.WriteUTF(feature.Mod.Name);
            output.WriteUTF(feature.Name);
            return;
        }
        foreach(MemberInfo member in value.GetType().Members().Where(member => member is FieldInfo or PropertyInfo && (!declearing || member.DeclaringType == type))) {
            bool skip = false;
            bool memberDeclearing = false;
            foreach(DataAttribute dataAttribute in member.GetCustomAttributes<DataAttribute>()) {
                switch(dataAttribute) {
                    case DataIncludeAttribute include:
                        if(include.CheckCondition(version)) skip = false;
                        break;
                    case DataExcludeAttribute exclude:
                        if(exclude.CheckCondition(version)) skip = true;
                        break;
                    case DummyAttribute dummy:
                        if(dummy.CheckCondition(version))
                            for(int i = 0; i < dummy.Count; i++)
                                output.WriteByte(0);
                        break;
                    case DeclearingAttribute declea:
                        if(declea.CheckCondition(version)) memberDeclearing = true;
                        break;
                }
            }
            if(skip) continue;
            Type memberType = null;
            object memberValue = null;
            if(member is FieldInfo field) {
                if(field.IsStatic || field.IsInitOnly) continue;
                memberType = field.FieldType;
                memberValue = field.GetValue(value);
            } else if(member is PropertyInfo property) {
                if(!property.CanWrite || property.GetSetMethod(true).IsStatic || property.Name == "Item") continue;
                memberType = property.PropertyType;
                memberValue = property.GetValue(value);
            }
            Type castType = memberType;
            CastAttribute cast = member.GetCustomAttribute<CastAttribute>();
            if(cast != null && cast.CheckCondition(version)) {
                if(cast.Type == null) continue;
                castType = cast.Type;
            }
            {
                IncludeClassAttribute inc = member.GetCustomAttribute<IncludeClassAttribute>();
                if(inc != null && inc.CheckCondition(version)) output.WriteUTF(memberType.FullName);
            }
            if(castType != memberType) {
                MethodInfo explicitCast = castType.Method("op_Explicit", memberType);
                MethodInfo implicitCast = memberType.Method("op_Implicit", castType);
                if(explicitCast != null && implicitCast != null) {
                    memberValue = cast.FirstCast switch {
                        FirstCast.Implicit => implicitCast.Invoke(null, memberValue),
                        FirstCast.Explicit => explicitCast.Invoke(null, memberValue),
                        _ => memberValue
                    };
                } else if(explicitCast != null) memberValue = explicitCast.Invoke(null, memberValue);
                else if(implicitCast != null) memberValue = implicitCast.Invoke(null, memberValue);
                else memberValue = Convert.ChangeType(memberValue, castType);
            }
            if(castType == typeof(long)) output.WriteLong((long) memberValue);
            else if(castType == typeof(int)) output.WriteInt((int) memberValue);
            else if(castType == typeof(short)) output.WriteShort((short) memberValue);
            else if(castType == typeof(byte)) output.WriteByte((byte) memberValue);
            else if(castType == typeof(bool)) output.WriteBoolean((bool) memberValue);
            else if(castType == typeof(string)) output.WriteUTF((string) memberValue);
            else if(castType == typeof(byte[])) output.WriteBytes((byte[]) memberValue);
            else if(castType == typeof(decimal)) output.WriteDecimal((decimal) memberValue);
            else if(castType == typeof(float)) output.WriteFloat((float) memberValue);
            else if(castType == typeof(double)) output.WriteDouble((double) memberValue);
            else if(castType == typeof(ushort)) output.WriteUShort((ushort) memberValue);
            else if(castType == typeof(uint)) output.WriteUInt((uint) memberValue);
            else if(castType == typeof(ulong)) output.WriteULong((ulong) memberValue);
            else if(castType == typeof(sbyte)) output.WriteSByte((sbyte) memberValue);
            else ToBytes(memberValue, output, castType, memberDeclearing, false, version);
        }
    }

    private static bool CheckType(Type type, Type check) {
        return type == check || type.IsSubclassOf(check);
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