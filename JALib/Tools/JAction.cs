using System.Reflection;
using System.Runtime.Serialization;
using JALib.Core;

namespace JALib.Tools;

public class JAction(JAMod mod, Action action) {
    private readonly Action action = action;
    public MethodInfo Method => action.Method;
    public object Target => action.Target;

    public void Invoke() {
        try {
            action();
        } catch (Exception e) {
            string key = "An error occurred while invoking an action " + action.Method.Name;
            JAMod mod1 = mod ?? JALib.Instance;
            mod1.LogException(key, e);
            mod1.ReportException(key, e);
        }
    }

    public IAsyncResult BeginInvoke(AsyncCallback callback, object obj) => action.BeginInvoke(callback, obj);
    public void EndInvoke(IAsyncResult result) => action.EndInvoke(result);
    public object DynamicInvoke(params object[] args) => action.DynamicInvoke(args);
    public Delegate[] GetInvocationList() => action.GetInvocationList();
    public void GetObjectData(SerializationInfo info, StreamingContext context) => action.GetObjectData(info, context);
    public override bool Equals(object obj) => this.action.Equals(obj is JAction ac ? ac.action : obj);
    public override int GetHashCode() => action.GetHashCode();
    public override string ToString() => action.ToString();
    public static bool operator ==(JAction a, JAction b) => a != null && b != null && a.action == b.action;
    public static bool operator !=(JAction a, JAction b) => !(a == b);
    public static implicit operator JAction(Action action) => new(null, action);
}