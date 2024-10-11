namespace JALib.Core.Setting;

[AttributeUsage(AttributeTargets.Field)]
public class SettingNameAttribute : Attribute {
    public string Name;

    public SettingNameAttribute(string name) {
        Name = name;
    }
}