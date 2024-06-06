using System.Reflection;
using UnityEngine.UIElements;

namespace JALib.Core.Setting.GUI.Elements;

public class LongField : SettingElement {
    public TextField textField;
    public FieldInfo field;
    public object target;
    public long? minValue;
    public long? maxValue;
    public long defaultValue;
    private string _cache;

    public void OnChangeText() {
        if(textField.value == _cache) return;
        if(!long.TryParse(textField.value, out long value)) {
            textField.value = _cache;
            return;
        }
        object before = field.GetValue(target);
        object after = value;
        ValueChange(before, ref after);
        if(after is not long) after = value;
        if((long) after != value) {
            value = (long) after;
            textField.value = value.ToString();
        }
        if(value < minValue) {
            value = defaultValue;
            textField.value = value.ToString();
        }
        if (value > maxValue) {
            value = defaultValue;
            textField.value = value.ToString();
        }
        field.SetValue(target, value);
        _cache = textField.value;
        ValueChangeFinal(before, value);
    }
}