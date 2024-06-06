using System.Reflection;
using UnityEngine.UIElements;

namespace JALib.Core.Setting.GUI.Elements;

public class IntegerField : SettingElement {
    public TextField textField;
    public FieldInfo field;
    public object target;
    public int? minValue;
    public int? maxValue;
    public int defaultValue;
    private string _cache;

    public void OnChangeText() {
        if(textField.value == _cache) return;
        if(!int.TryParse(textField.value, out int value)) {
            textField.value = _cache;
            return;
        }
        object before = field.GetValue(target);
        object after = value;
        ValueChange(before, ref after);
        if(after is not int) after = value;
        if((int) after != value) {
            value = (int) after;
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