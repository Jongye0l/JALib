using System.Reflection;

namespace JALib.Core.Setting.GUI.Elements;

public class Select : SettingElement {
    public FieldInfo field;
    public object target;
    public string[] options;
    public string defaultValue;

    public void OnValueChange() {
        
    }
}