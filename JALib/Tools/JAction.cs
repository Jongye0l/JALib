using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using JALib.Core;

namespace JALib.Tools;

public class JAction {
    private readonly Action action;
    private readonly JAMod mod;
    public MethodInfo Method => action.Method;
    public object Target => action.Target;
    
    public JAction(JAMod mod, Action action) {
        this.mod = mod;
        this.action = action;
    }

    public void Invoke() {
        try {
            action.Invoke();
        } catch (Exception e) {
            mod.Error("An error occurred while invoking an action.");
            mod.LogException(e);
            ErrorUtils.ShowError(mod, e);
        }
    }
    
    public IAsyncResult BeginInvoke(AsyncCallback callback, object obj) {
        return action.BeginInvoke(callback, obj);
    }

    public void EndInvoke(IAsyncResult result) {
        action.EndInvoke(result);
    }

    public object DynamicInvoke(params object[] args) {
        return action.DynamicInvoke(args);
    }
    
    public Delegate[] GetInvocationList() {
        return action.GetInvocationList();
    }
    
    public void GetObjectData(SerializationInfo info, StreamingContext context) {
        action.GetObjectData(info, context);
    }
    
    public override bool Equals(object obj) {
        return this.action.Equals(obj is JAction action ? action.action : obj);
    }
    
    public override int GetHashCode() {
        return action.GetHashCode();
    }
    
    public override string ToString() {
        return action.ToString();
    }
    
    public static bool operator ==(JAction a, JAction b) {
        return a != null && b != null && a.action == b.action;
    }
    
    public static bool operator !=(JAction a, JAction b) {
        return !(a == b);
    }
}