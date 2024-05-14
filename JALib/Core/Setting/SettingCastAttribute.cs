using System;

namespace JALib.Core.Setting;

[AttributeUsage(AttributeTargets.Field)]
public class SettingCastAttribute : Attribute {
    public Type CastType;

    public SettingCastAttribute(Type castType) {
        CastType = castType;
    }
}