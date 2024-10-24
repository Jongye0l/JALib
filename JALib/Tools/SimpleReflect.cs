using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace JALib.Tools;

public static class SimpleReflect {
    public static object GetValue(this FieldInfo field) => field.GetValue(null);

    public static T GetValue<T>(this FieldInfo field, object o = null) => (T) field.GetValue(o) ?? default;

    public static void SetValue(this FieldInfo field, object o) => field.SetValue(null, o);

    public static FieldInfo Field(this Type type, [NotNull] string name) => type.GetField(name, AccessTools.all);

    public static FieldInfo[] Fields(this Type type) => type.GetFields(AccessTools.all);

    public static MethodInfo Method(this Type type, [NotNull] string name) => type.GetMethod(name, AccessTools.all);

    public static MethodInfo Method(this Type type, [NotNull] string name, [NotNull] params Type[] types) => type.GetMethod(name, AccessTools.all, null, types, null);

    public static MethodInfo[] Methods(this Type type) => type.GetMethods(AccessTools.all);

    public static object Invoke(this MethodInfo methodInfo, object o = null) => methodInfo.Invoke(o, Array.Empty<object>());

    public static object Invoke(this MethodInfo methodInfo, [NotNull] object[] objects) => methodInfo.Invoke(null, objects);

    public static object Invoke(this MethodInfo methodInfo, object o, [NotNull] object[] objects) => methodInfo.Invoke(o, objects);

    public static T Invoke<T>(this MethodInfo methodInfo, object o = null) => (T) methodInfo.Invoke(o, Array.Empty<object>()) ?? default;

    public static T Invoke<T>(this MethodInfo methodInfo, [NotNull] params object[] objects) => (T) methodInfo.Invoke(null, objects) ?? default;

    public static T Invoke<T>(this MethodInfo methodInfo, object obj, params object[] parameters) => (T) methodInfo.Invoke(obj, parameters) ?? default;

    public static MemberInfo[] Member(this Type type, [NotNull] string name) => type.GetMember(name, AccessTools.all);

    public static MemberInfo[] Members(this Type type) => type.GetMembers(AccessTools.all);

    public static object Invoke(this Type type, [NotNull] string name) => type.Method(name).Invoke();

    public static object Invoke(this Type type, [NotNull] string name, object o) => type.Method(name).Invoke(o);

    public static object Invoke(this Type type, [NotNull] string name, [NotNull] params object[] objects) => type.Method(name).Invoke(null, objects);

    public static object Invoke(this Type type, Type[] types, [NotNull] string name, object o = null) => type.Method(name, types).Invoke(o);

    public static object Invoke(this Type type, Type[] types, [NotNull] string name, [NotNull] params object[] objects) => type.Method(name, types).Invoke(null, objects);

    public static T Invoke<T>(this Type type, [NotNull] string name) => type.Method(name).Invoke<T>() ?? default;

    public static T Invoke<T>(this Type type, [NotNull] string name, object o) => type.Method(name).Invoke<T>(o) ?? default;

    public static T Invoke<T>(this Type type, [NotNull] string name, [NotNull] params object[] objects) => (T) type.Method(name).Invoke(null, objects) ?? default;

    public static T Invoke<T>(this Type type, Type[] types, [NotNull] string name, object o = null) => type.Method(name, types).Invoke<T>(o);

    public static T Invoke<T>(this Type type, Type[] types, [NotNull] string name, [NotNull] params object[] objects) => (T) type.Method(name, types).Invoke(null, objects) ?? default;

    public static object GetValue(this Type type, [NotNull] string name, object o = null) => type.Field(name).GetValue(o);

    public static T GetValue<T>(this Type type, [NotNull] string name, object o = null) => (T) type.Field(name).GetValue(o) ?? default;

    public static void SetValue(this Type type, [NotNull] string name, object value, object o = null) => type.Field(name).SetValue(o, value);

    public static ConstructorInfo Constructor(this Type type) {
        ConstructorInfo[] constructors = type.GetConstructors(AccessTools.all);
        if(constructors.Length != 1) throw new Exception("Constructor count is not 1");
        return constructors[0];
    }

    public static ConstructorInfo Constructor(this Type type, [NotNull] params Type[] types) => type.GetConstructor(AccessTools.all, null, types, null);

    public static ConstructorInfo GetConstructor(this Type type) => type.Constructor();

    public static object New(this Type type) => Activator.CreateInstance(type, true);

    public static object New(this Type type, params object[] objects) => Activator.CreateInstance(type, AccessTools.all, null, objects, null);

    public static T New<T>(this Type type) => (T) type.New() ?? default;

    public static T New<T>(this Type type, params object[] objects) => (T) type.New(objects) ?? default;

    public static object GetValue(this object obj, [NotNull] string name) {
        FieldInfo field = obj.GetType().Field(name);
        return field == null ? obj.GetType().Property(name).GetValue(obj) : field.GetValue(obj);
    }

    public static T GetValue<T>(this object obj, [NotNull] string name) => (T) obj.GetValue(name);

    public static void SetValue(this object obj, [NotNull] string name, object value) {
        FieldInfo fieldInfo = obj.GetType().Field(name);
        if(fieldInfo != null) fieldInfo.SetValue(obj, value);
        else obj.GetType().Property(name).SetValue(obj, value);
    }

    public static FieldInfo Field(this object obj, [NotNull] string name) => obj.GetType().Field(name);

    public static MethodInfo Method(this object obj, [NotNull] string name) => obj.GetType().Method(name);

    public static MethodInfo Method(this object obj, [NotNull] string name, [NotNull] params Type[] types) => obj.GetType().Method(name, types);

    public static object Invoke(this object obj, [NotNull] string name) => obj.Method(name, []).Invoke(obj);

    public static object Invoke(this object obj, [NotNull] string name, [NotNull] params object[] objects) => obj.Method(name).Invoke(obj, objects);

    public static object Invoke(this object obj, [NotNull] string name, [NotNull] Type[] types, [NotNull] params object[] objects) => obj.Method(name, types).Invoke(obj, objects);

    public static T Invoke<T>(this object obj, [NotNull] string name) => obj.Method(name, []).Invoke<T>(obj) ?? default;

    public static T Invoke<T>(this object obj, [NotNull] string name, [NotNull] params object[] objects) => obj.Method(name).Invoke<T>(obj, objects) ?? default;

    public static T Invoke<T>(this object obj, [NotNull] string name, [NotNull] Type[] types, [NotNull] params object[] objects) => obj.Method(name, types).Invoke<T>(obj, objects) ?? default;

    public static object GetValue(this object obj, [NotNull] string name, [NotNull] params object[] objects) => obj.Method(name).Invoke(obj, objects);

    public static object GetValue(this object obj, [NotNull] string name, object o) => obj.Method(name).Invoke(o);

    public static object GetValue(this object obj, [NotNull] string name, Type[] types, [NotNull] params object[] objects) => obj.Method(name, types).Invoke(obj, objects);

    public static object GetValue(this object obj, [NotNull] string name, Type[] types, object o) => obj.Method(name, types).Invoke(o);

    public static object GetValue(this object obj, [NotNull] string name, Type[] types) => obj.Method(name, types).Invoke();

    public static object GetValue(this object obj, [NotNull] string name, Type[] types, object o, [NotNull] params object[] objects) => obj.Method(name, types).Invoke(o, objects);

    public static T GetValue<T>(this object obj, [NotNull] string name, [NotNull] params object[] objects) => obj.Method(name).Invoke<T>(obj, objects) ?? default;

    public static T GetValue<T>(this object obj, [NotNull] string name, object o) => obj.Method(name).Invoke<T>(o) ?? default;

    public static T GetValue<T>(this object obj, [NotNull] string name, Type[] types, [NotNull] params object[] objects) => obj.Method(name, types).Invoke<T>(obj, objects) ?? default;

    public static T GetValue<T>(this object obj, [NotNull] string name, Type[] types, object o) => obj.Method(name, types).Invoke<T>(o) ?? default;

    public static T GetValue<T>(this object obj, [NotNull] string name, Type[] types) => obj.Method(name, types).Invoke<T>() ?? default;

    public static T GetValue<T>(this object obj, [NotNull] string name, Type[] types, object o, [NotNull] params object[] objects) => obj.Method(name, types).Invoke<T>(o, objects) ?? default;

    public static PropertyInfo Property(this Type type, [NotNull] string name) => type.GetProperty(name, AccessTools.all);

    public static PropertyInfo[] Properties(this Type type) => type.GetProperties(AccessTools.all);

    public static object GetValue(this PropertyInfo property, object o = null) => property.GetValue(o);

    public static T GetValue<T>(this PropertyInfo property, object o = null) => (T) property.GetValue(o) ?? default;

    public static void SetValue(this PropertyInfo property, object o, object value) => property.SetValue(o, value);

    public static MethodInfo Getter(this PropertyInfo property) => property.GetGetMethod(true);

    public static MethodInfo Setter(this PropertyInfo property) => property.GetSetMethod(true);

    public static MethodInfo[] Accessors(this PropertyInfo property) => property.GetAccessors(true);

    public static MethodInfo Getter(this Type type, [NotNull] string name) => type.Property(name).Getter();

    public static MethodInfo Setter(this Type type, [NotNull] string name) => type.Property(name).Setter();

    public static MethodInfo[] Accessors(this Type type, [NotNull] string name) => type.Property(name).Accessors();

    public static MethodInfo Getter(this object obj, [NotNull] string name) => obj.GetType().Property(name).Getter();

    public static MethodInfo Setter(this object obj, [NotNull] string name) => obj.GetType().Property(name).Setter();

    public static MethodInfo[] Accessors(this object obj, [NotNull] string name) => obj.GetType().Property(name).Accessors();

    public static T New<T>() => typeof(T).New<T>() ?? default;

    public static T New<T>(params object[] objects) => typeof(T).New<T>(objects) ?? default;

    public static bool IsNumeric(this Type type) => type.IsInteger() || type.IsFloat();

    public static bool IsInteger(this Type type) => type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                                                    || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);

    public static bool IsFloat(this Type type) => type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    public static bool IsContains(this Type type, Type target) => target.IsAssignableFrom(type);

    public static bool IsNullable(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
}