using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JALib.Tools;

public static class MainThread {
    public static Thread Thread { get; private set; }
    private static ConcurrentQueue<JAction> queue = new();
    private static StaticCoroutine staticCoroutine;

    internal static void Initialize() {
        Thread = Thread.CurrentThread;
        queue ??= new ConcurrentQueue<JAction>();
        Run(new JAction(JALib.Instance, () => {
            staticCoroutine = new GameObject("StaticCoroutine").AddComponent<StaticCoroutine>();
            Object.DontDestroyOnLoad(staticCoroutine.gameObject);
        }));
    }

    internal static void Dispose() {
        Thread = null;
        GC.SuppressFinalize(queue);
        if(!staticCoroutine) return;
        staticCoroutine.StopAllCoroutines();
        Object.Destroy(staticCoroutine.gameObject);
        queue.Clear();
        GC.SuppressFinalize(queue);
        GC.SuppressFinalize(staticCoroutine);
        queue = null;
        staticCoroutine = null;
    }

    internal static void OnUpdate() {
        while(queue.TryDequeue(out JAction action)) action.Invoke();
    }

    public static void Run(JAction action) {
        if(IsMainThread() || Thread == null) {
            action.Invoke();
            return;
        }
        queue.Enqueue(action);
    }

    public static bool IsMainThread() => Thread.CurrentThread == Thread;
    public static Coroutine StartCoroutine(IEnumerator routine) => staticCoroutine.StartCoroutine(routine);
    public static void StopCoroutine(Coroutine routine) => staticCoroutine.StopCoroutine(routine);
    public static void StopCoroutine(IEnumerator routine) => staticCoroutine.StopCoroutine(routine);

    private class StaticCoroutine : MonoBehaviour;
}