using System;
using JALib.Core;
using UnityEngine;

namespace JALib.Tools;

public class SettingGUI {
    private JAMod mod;
    
    public SettingGUI(JAMod mod) {
        this.mod = mod;
    }
    
    public void AddSettingToggle(ref bool value, string text, Action onChanged = null) {
        bool result = GUILayout.Toggle(value, text);
        if(value == result) return;
        value = result;
        onChanged?.Invoke();
        mod.ModSetting.Save();
    }
    
    public void AddSettingToggleInt(ref int value, int defaultValue, ref bool value2, ref string valueString, string text, int min = int.MinValue, int max = int.MaxValue, Action onChanged = null) {
        GUILayout.BeginHorizontal();
        if(GUILayout.Toggle(value2, text)) {
            if(!value2) {
                value2 = true;
                onChanged?.Invoke();
                mod.ModSetting.Save();
            }
            GUILayout.Space(4f);
            valueString = GUILayout.TextField(valueString ?? value.ToString());
            int resultInt;
            try {
                resultInt = valueString.IsNullOrEmpty() ? defaultValue : int.Parse(valueString);
                if(resultInt < min) {
                    resultInt = min;
                    valueString = min.ToString();
                } else if(resultInt > max) {
                    resultInt = max;
                    valueString = max.ToString();
                }
            } catch (FormatException) {
                resultInt = defaultValue;
                valueString = defaultValue.ToString();
            }
            if(resultInt != value) {
                value = resultInt;
                onChanged?.Invoke();
                mod.ModSetting.Save();
            }
        } else if(value2) {
            value2 = false;
            onChanged?.Invoke();
            mod.ModSetting.Save();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    public void AddSettingInt(ref int value, int defaultValue, ref string valueString, string text, int min = int.MinValue, int max = int.MaxValue, Action onChanged = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text);
        GUILayout.Space(4f);
        valueString = GUILayout.TextField(valueString ?? value.ToString());
        int resultInt;
        try {
            resultInt = valueString.IsNullOrEmpty() ? defaultValue : int.Parse(valueString);
            if(resultInt < min) {
                resultInt = min;
                valueString = min.ToString();
            } else if(resultInt > max) {
                resultInt = max;
                valueString = max.ToString();
            }
        } catch (FormatException) {
            resultInt = defaultValue;
            valueString = defaultValue.ToString();
        }
        if(resultInt != value) {
            value = resultInt;
            onChanged?.Invoke();
            mod.ModSetting.Save();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    
    public void AddSettingFloat(ref float value, float defaultValue, ref string valueString, string text, float min = float.MinValue, float max = float.MaxValue, Action onChanged = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text);
        GUILayout.Space(4f);
        valueString = GUILayout.TextField(valueString ?? value.ToString());
        float resultFloat;
        try {
            resultFloat = valueString.IsNullOrEmpty() ? defaultValue : float.Parse(valueString);
            if(resultFloat < min) {
                resultFloat = min;
                valueString = min.ToString();
            } else if(resultFloat > max) {
                resultFloat = max;
                valueString = max.ToString();
            }
        } catch (FormatException) {
            resultFloat = defaultValue;
            valueString = defaultValue.ToString();
        }
        if(resultFloat != value) {
            value = resultFloat;
            onChanged?.Invoke();
            mod.ModSetting.Save();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    
    public void AddSettingLong(ref long value, long defaultValue, ref string valueString, string text, long min = long.MinValue, long max = long.MaxValue, Action onChanged = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text);
        GUILayout.Space(4f);
        valueString = GUILayout.TextField(valueString ?? value.ToString());
        long resultLong;
        try {
            resultLong = valueString.IsNullOrEmpty() ? defaultValue : long.Parse(valueString);
            if(resultLong < min) {
                resultLong = min;
                valueString = min.ToString();
            } else if(resultLong > max) {
                resultLong = max;
                valueString = max.ToString();
            }
        } catch (FormatException) {
            resultLong = defaultValue;
            valueString = defaultValue.ToString();
        }
        if(resultLong != value) {
            value = resultLong;
            onChanged?.Invoke();
            mod.ModSetting.Save();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    
    public void AddSettingDouble(ref double value, double defaultValue, ref string valueString, string text, double min = double.MinValue, double max = double.MaxValue, Action onChanged = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text);
        GUILayout.Space(4f);
        valueString = GUILayout.TextField(valueString ?? value.ToString());
        double resultDouble;
        try {
            resultDouble = valueString.IsNullOrEmpty() ? defaultValue : double.Parse(valueString);
            if(resultDouble < min) {
                resultDouble = min;
                valueString = min.ToString();
            } else if(resultDouble > max) {
                resultDouble = max;
                valueString = max.ToString();
            }
        } catch (FormatException) {
            resultDouble = defaultValue;
            valueString = defaultValue.ToString();
        }
        if(resultDouble != value) {
            value = resultDouble;
            onChanged?.Invoke();
            mod.ModSetting.Save();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    
    public void AddSettingString(ref string value, string defaultValue, string text, Action onChanged = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text);
        GUILayout.Space(4f);
        string result = GUILayout.TextField(value ?? defaultValue);
        if(value == result) return;
        value = result;
        onChanged?.Invoke();
        mod.ModSetting.Save();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    public void AddSettingEnum<T>(ref T value, string text, T[] values = null, Action onChanged = null) where T : Enum {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text);
        GUILayout.Space(4f);
        values ??= (T[]) Enum.GetValues(typeof(T));
        foreach(T current in values)
            AddEnumButton(ref value, current.ToString(), current, onChanged);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void AddEnumButton<T>(ref T value, string text, T current, Action onChanged) {
        text = value.Equals(current) ? $"<b>{text}</b>" : text;
        if(!GUILayout.Button(text)) return;
        value = current;
        onChanged?.Invoke();
        mod.ModSetting.Save();
    }
    
    public void AddSettingSliderFloat(ref float value, float defaultValue, ref string valueString, string text, float min, float max, Action onChanged = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text);
        GUILayout.Space(4f);
        float result = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(300));
        if(result != value) {
           value = result;
           valueString = $"{value:0.00}";
           onChanged?.Invoke();
           mod.ModSetting.Save();
        }
        valueString = GUILayout.TextField(valueString ?? $"{value:0.00}", GUILayout.Width(50));
        try {
           result = valueString.IsNullOrEmpty() ? defaultValue : float.Parse(valueString);
        } catch (FormatException) {
           result = defaultValue;
           valueString = $"{result:0.00}";
        }
        if(result != value) {
           value = result;
           onChanged?.Invoke();
           mod.ModSetting.Save();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    
    public void AddSettingSliderInt(ref int value, int defaultValue, ref string valueString, string text, int min, int max, Action onChanged = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text);
        GUILayout.Space(4f);
        int result = (int) Math.Round(GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(300)));
        if(result != value) {
           value = result;
           valueString = value.ToString();
           onChanged?.Invoke();
           mod.ModSetting.Save();
        }
        valueString = GUILayout.TextField(valueString ?? value.ToString(), GUILayout.Width(50));
        try {
           result = valueString.IsNullOrEmpty() ? defaultValue : int.Parse(valueString);
        } catch (FormatException) {
           result = defaultValue;
           valueString = result.ToString();
        }
        if(result != value) {
           value = result;
           onChanged?.Invoke();
           mod.ModSetting.Save();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
}