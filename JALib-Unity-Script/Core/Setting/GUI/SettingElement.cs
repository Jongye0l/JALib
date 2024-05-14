using UnityEngine;

namespace JALib.Core.Setting.GUI;

public abstract class SettingElement : MonoBehaviour {
    public event ChangeValue OnChangeValue;
    public event ChangeValueFinal OnChangeValueFinal;
    
    public delegate void ChangeValue(object before, ref object after);
    
    public delegate void ChangeValueFinal(object before, object after);

    protected void ValueChange(object before, ref object after) {
        OnChangeValue?.Invoke(before, ref after);
    }
    
    protected void ValueChangeFinal(object before, object after) {
        OnChangeValueFinal?.Invoke(before, after);
    }
}