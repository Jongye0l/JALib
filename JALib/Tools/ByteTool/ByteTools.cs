using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using JALib.Core;
using JetBrains.Annotations;

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
        // Input bytes are in big-endian (network byte order)
        // BitConverter expects native byte order
        if(!BitConverter.IsLittleEndian) {
            // On big-endian systems, no conversion needed
            return BitConverter.ToSingle(bytes, start);
        }
        // On little-endian systems, reverse bytes using stackalloc for zero allocation
        Span<byte> reversed = stackalloc byte[4] { bytes[start + 3], bytes[start + 2], bytes[start + 1], bytes[start] };
        return BitConverter.ToSingle(reversed);
    }

    public static double ToDouble(this byte[] bytes, int start = 0) {
        CheckArgument(bytes.Length - start, 8);
        // Input bytes are in big-endian (network byte order)
        // BitConverter expects native byte order
        if(!BitConverter.IsLittleEndian) {
            // On big-endian systems, no conversion needed
            return BitConverter.ToDouble(bytes, start);
        }
        // On little-endian systems, reverse bytes using stackalloc for zero allocation
        Span<byte> reversed = stackalloc byte[8] { 
            bytes[start + 7], bytes[start + 6], bytes[start + 5], bytes[start + 4],
            bytes[start + 3], bytes[start + 2], bytes[start + 1], bytes[start] 
        };
        return BitConverter.ToDouble(reversed);
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

    public static T ToObject<T>(this byte[] bytes, int start = 0, bool declaring = false, bool includeClass = false, uint? version = null, bool nullable = true) =>
        (T) ToObject(bytes, typeof(T), start, declaring, includeClass, version, nullable);

    public static T ToObject<T>(Stream input, bool declaring = false, bool includeClass = false, uint? version = null, bool nullable = true) =>
        (T) ToObject(input, typeof(T), declaring, includeClass, version, nullable);

    public static T ToObject<T>(this byte[] bytes, Type type, int start = 0, bool declaring = false, bool includeClass = false, uint? version = null, bool nullable = true) =>
        (T) ToObject(bytes, type, start, declaring, includeClass, version, nullable);

    public static T ToObject<T>(Stream input, Type type, bool declaring = false, bool includeClass = false, uint? version = null, bool nullable = true) =>
        (T) ToObject(input, type, declaring, includeClass, version, nullable);

    public static T ChangeData<T>(this T obj, byte[] bytes, int start = 0, bool declaring = false, uint? version = null) => ChangeData(obj, bytes, typeof(T), start, declaring, version);

    public static T ChangeData<T>(this T obj, Stream input, bool declaring = false, uint? version = null) => ChangeData(obj, input, typeof(T), declaring, version);

    public static T ChangeData<T>(this T obj, byte[] bytes, Type type, int start = 0, bool declaring = false, uint? version = null) => (T) ChangeData((object) obj, bytes, type, start, declaring, version);

    public static T ChangeData<T>(this T obj, Stream input, Type type, bool declaring = false, uint? version = null) => (T) ChangeData((object) obj, input, type, declaring, version);

    public static object ToObject(this byte[] bytes, Type type, int start = 0, bool declaring = false, bool includeClass = false, uint? version = null, bool nullable = true) {
        using MemoryStream input = new(bytes);
        while(start-- > 0) input.ReadByte();
        return ToObject(input, type, declaring, includeClass, version, nullable);
    }

    public static object ToObject(Stream input, Type type, bool declaring = false, bool includeClass = false, uint? version = null, bool nullable = true) {
        if(type == typeof(byte)) return (byte) input.ReadByte();
        if(type == typeof(sbyte)) return input.ReadSByte();
        if(type == typeof(short)) return input.ReadShort();
        if(type == typeof(ushort)) return input.ReadUShort();
        if(type == typeof(int)) return input.ReadInt();
        if(type == typeof(uint)) return input.ReadUInt();
        if(type == typeof(long)) return input.ReadLong();
        if(type == typeof(ulong)) return input.ReadULong();
        if(type == typeof(float)) return input.ReadFloat();
        if(type == typeof(double)) return input.ReadDouble();
        if(type == typeof(decimal)) return input.ReadDecimal();
        if(type == typeof(bool)) return input.ReadBoolean();
        if(type == typeof(char)) return input.ReadChar();
        if(type == typeof(string)) return input.ReadUTF();
        if(type == typeof(byte[])) return input.ReadBytes();
        VersionAttribute ver = type.GetCustomAttribute<VersionAttribute>();
        if(ver != null && version == null) version = ver.Version;
        IncludeClassAttribute includeCl = type.GetCustomAttribute<IncludeClassAttribute>();
        DeclearingAttribute declear = type.GetCustomAttribute<DeclearingAttribute>();
        if(includeCl != null && includeCl.CheckCondition(version)) includeClass = true;
        if(declear != null && declear.CheckCondition(version)) declaring = true;
        if(includeClass) type = Type.GetType(input.ReadUTF());
        if(CheckType(type, typeof(ICollection)) && type.GetCustomAttribute<IgnoreArrayAttribute>() == null) {
            int size = input.ReadInt();
            if(size == -1) return null;
            Type elementType = type.GetGenericArguments()[0];
            if(type.IsArray) {
                Array array = Array.CreateInstance(elementType, size);
                for(int i = 0; i < size; i++) array.SetValue(ToObject(input, elementType), i);
                return array;
            }
            ConstructorInfo constructorInfo;
            object collection = (constructorInfo = type.Constructor(typeof(int))) != null ? constructorInfo.Invoke([size]) : type.New();
            MethodInfo addMethod = type.Method("Add");
            if(CheckType(type, typeof(IDictionary)))
                for(int i = 0; i < size; i++)
                    addMethod.Invoke(collection, [ToObject(input, elementType), ToObject(input, type.GetGenericArguments()[1])]);
            else
                for(int i = 0; i < size; i++)
                    addMethod.Invoke(collection, [ToObject(input, elementType)]);
            return collection;
        }
        if(type.GetCustomAttribute<NotNullAttribute>() == null && nullable && !input.ReadBoolean()) return null;
        if(CheckType(type, typeof(Delegate))) {
            if(!includeClass) type = Type.GetType(input.ReadUTF());
            Type declaringType = Type.GetType(input.ReadUTF());
            MethodInfo method = declaringType.Method(input.ReadUTF());
            object target = null;
            if(input.ReadBoolean()) {
                Type targetType = Type.GetType(input.ReadUTF());
                target = ToObject(input, targetType, declaring);
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
        if(CheckType(type, typeof(Type))) {
            string typeName = input.ReadUTF();
            return Type.GetType(typeName);
        }
        if(CheckType(type, typeof(MethodInfo))) {
            string typeName = input.ReadUTF();
            string methodName = input.ReadUTF();
            Type[] types = input.ReadObject<Type[]>();
            return Type.GetType(typeName).Method(methodName, types);
        }
        if(CheckType(type, typeof(ConstructorInfo))) {
            string typeName = input.ReadUTF();
            Type[] types = input.ReadObject<Type[]>();
            Type declaringType = Type.GetType(typeName);
            return types == null ? declaringType.TypeInitializer : declaringType.Constructor(types);
        }
        if(CheckType(type, typeof(MethodBase))) {
            string typeName = input.ReadUTF();
            bool isConstructor = input.ReadBoolean();
            Type declaringType = Type.GetType(typeName);
            if(isConstructor) {
                Type[] types = input.ReadObject<Type[]>();
                return types == null ? declaringType.TypeInitializer : declaringType.Constructor(types);
            } else {
                string methodName = input.ReadUTF();
                Type[] types = input.ReadObject<Type[]>();
                return declaringType.Method(methodName, types);
            }
        }
        object obj = Activator.CreateInstance(type);
        return ChangeData(obj, input, type, declaring, version);
    }

    public static object ChangeData(this object obj, byte[] bytes, int start = 0, bool declaring = false, uint? version = null) => ChangeData(obj, bytes, obj.GetType(), start, declaring, version);

    public static object ChangeData(this object obj, Stream input, bool declaring = false, uint? version = null) => ChangeData(obj, input, obj.GetType(), declaring, version);

    public static object ChangeData(this object obj, byte[] bytes, Type type, int start = 0, bool declaring = false, uint? version = null) {
        using MemoryStream input = new(bytes);
        while(start-- > 0) input.ReadByte();
        return ChangeData(obj, input, type, declaring, version);
    }

    public static object ChangeData(this object obj, Stream input, Type type, bool declaring = false, uint? version = null) {
        foreach(MemberInfo member in type.Members().Where(member => member is FieldInfo or PropertyInfo && (!declaring || member.DeclaringType == type))) {
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
            object value = ToObject(input, castType, memberDeclearing, false, version);
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

    public static byte[] ToBytes(this float value) => BitConverter.GetBytes(value).Reverse();

    public static byte[] ToBytes(this double value) => BitConverter.GetBytes(value).Reverse();

    public static byte[] ToBytes(this decimal value) {
        byte[] buffer = new byte[16];
        value.ToBytes(buffer);
        return buffer;
    }

    public static byte[] ToBytes(this object value, bool declaring = false, bool includeClass = false, uint? version = null, bool nullable = true) {
        using MemoryStream output = new();
        ToBytes(value, output, declaring, includeClass, version, nullable);
        return output.ToArray();
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

    public static void ToBytes(this object value, Stream output, bool declaring = false, bool includeClass = false, uint? version = null, bool nullable = true) {
        ToBytes(value, output, value.GetType(), declaring, includeClass, version, nullable);
    }

    public static void ToBytes(this object value, Stream output, Type type, bool declaring = false, bool includeClass = false, uint? version = null, bool nullable = true) {
        bool front = true;
        if(type == typeof(byte)) output.WriteByte((byte) value);
        else if(type == typeof(sbyte)) output.WriteByte((byte) value);
        else if(type == typeof(short)) output.WriteShort((short) value);
        else if(type == typeof(ushort)) output.WriteUShort((ushort) value);
        else if(type == typeof(int)) output.WriteInt((int) value);
        else if(type == typeof(uint)) output.WriteUInt((uint) value);
        else if(type == typeof(long)) output.WriteLong((long) value);
        else if(type == typeof(ulong)) output.WriteULong((ulong) value);
        else if(type == typeof(float)) output.WriteFloat((float) value);
        else if(type == typeof(double)) output.WriteDouble((double) value);
        else if(type == typeof(decimal)) output.WriteDecimal((decimal) value);
        else if(type == typeof(bool)) output.WriteBoolean((bool) value);
        else if(type == typeof(char)) output.WriteChar((char) value);
        else if(type == typeof(string)) output.WriteUTF((string) value);
        else if(type == typeof(byte[])) output.WriteBytes((byte[]) value);
        else front = false;
        if(front) return;
        VersionAttribute ver = type.GetCustomAttribute<VersionAttribute>();
        if(ver != null) version = ver.Version;
        IncludeClassAttribute includeCl = type.GetCustomAttribute<IncludeClassAttribute>();
        DeclearingAttribute declear = type.GetCustomAttribute<DeclearingAttribute>();
        if(includeCl != null && includeCl.CheckCondition(version)) includeClass = true;
        if(declear != null && declear.CheckCondition(version)) declaring = true;
        if(includeClass) output.WriteUTF(type.FullName);
        if(CheckType(type, typeof(ICollection)) && type.GetCustomAttribute<IgnoreArrayAttribute>() == null) {
            if(value == null) {
                output.WriteInt(-1);
                return;
            }
            if(type.IsArray) {
                Array array = (Array) value;
                output.WriteInt(array.Length);
                foreach(object obj in array) ToBytes(obj, output, type.GetElementType(), declaring);
                return;
            }
            output.WriteInt(value.GetValue<int>("Count"));
            Type elementType = type.GetGenericArguments()[0];
            if(CheckType(type, typeof(IDictionary))) {
                Type valueType = type.GetGenericArguments()[1];
                IDictionary dictionary = (IDictionary) value;
                foreach(object key in dictionary.Keys) {
                    ToBytes(key, output, elementType, declaring);
                    ToBytes(dictionary[key], output, valueType, declaring);
                }
                return;
            }
            foreach(object obj in (IEnumerable) value) ToBytes(obj, output, elementType, declaring);
            return;
        }
        if(type.GetCustomAttribute<NotNullAttribute>() == null && nullable) {
            if(value == null) {
                output.WriteBoolean(false);
                return;
            }
            output.WriteBoolean(true);
        }
        if(CheckType(type, typeof(Delegate))) {
            if(!includeClass) output.WriteUTF(type.FullName);
            Delegate del = (Delegate) value;
            output.WriteObject(del.Method, typeof(MethodInfo));
            output.WriteObject(del.Target, includeClass: true);
            del.Target.ToBytes(output, declaring, nullable: true);
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
        if(CheckType(type, typeof(Type))) {
            Type typeValue = (Type) value;
            output.WriteUTF(typeValue.FullName);
            return;
        }
        if(CheckType(type, typeof(MethodInfo))) {
            MethodInfo method = (MethodInfo) value;
            output.WriteUTF(method.DeclaringType.FullName);
            output.WriteUTF(method.Name);
            output.WriteObject(method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            return;
        }
        if(CheckType(type, typeof(ConstructorInfo))) {
            ConstructorInfo constructor = (ConstructorInfo) value;
            output.WriteUTF(constructor.DeclaringType.FullName);
            output.WriteObject(constructor.IsStatic ? null : constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray(), typeof(Type[]));
            return;
        }
        if(CheckType(type, typeof(MethodBase))) {
            MethodBase method = (MethodBase) value;
            output.WriteUTF(method.DeclaringType.FullName);
            if(method is ConstructorInfo info) {
                output.WriteBoolean(true);
                output.WriteObject(info.IsStatic ? null : info.GetParameters().Select(parameter => parameter.ParameterType).ToArray(), typeof(Type[]));
            } else {
                output.WriteBoolean(false);
                output.WriteUTF(method.Name);
                output.WriteObject(method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            }
            return;
        }
        foreach(MemberInfo member in value.GetType().Members().Where(member => member is FieldInfo or PropertyInfo && (!declaring || member.DeclaringType == type))) {
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
            ToBytes(memberValue, output, castType, memberDeclearing, false, version);
        }
    }

    private static bool CheckType(Type type, Type check) => check.IsAssignableFrom(type);

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