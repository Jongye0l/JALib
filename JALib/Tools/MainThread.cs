using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JALib.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JALib.Tools;

public static class MainThread {
    public static Thread Thread { get; private set; }
    private static bool _isRunningOnMainThreadUpdate;
    public static bool IsRunningOnMainThreadUpdate => _isRunningOnMainThreadUpdate && IsMainThread();
    public static bool IsQueueEmpty => queue.IsEmpty;
    private static ConcurrentQueue<JAction> queue = new();
    private static StaticCoroutine staticCoroutine;
    private static TaskCompletionSource<bool> completeLoadTask;

    internal static void Initialize() {
        queue ??= new ConcurrentQueue<JAction>();
        queue.Enqueue(new JAction(JALib.Instance, Setup));
    }

    private static void Setup() {
        Thread = Thread.CurrentThread;
        staticCoroutine = new GameObject("StaticCoroutine").AddComponent<StaticCoroutine>();
        Object.DontDestroyOnLoad(staticCoroutine.gameObject);
    }

    public static Task WaitForMainThread() {
        completeLoadTask ??= new TaskCompletionSource<bool>();
        return completeLoadTask.Task ?? Task.FromResult(true);
    }

    internal static void Dispose() {
        if(completeLoadTask != null) {
            completeLoadTask.SetException(new ObjectDisposedException("JALib is disposed."));
            completeLoadTask = null;
        }
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
        _isRunningOnMainThreadUpdate = true;
        try {
            if(completeLoadTask != null) {
                completeLoadTask.TrySetResult(true);
                completeLoadTask = null;
            }
            while(queue.TryDequeue(out JAction action)) action.Invoke();
        } finally {
            _isRunningOnMainThreadUpdate = false;
        }
    }

    public static void Run(JAction action) {
        if(IsMainThread()) {
            action.Invoke();
            return;
        }
        queue.Enqueue(action);
    }

    public static void Run(JAMod mod, Action action) => Run(new JAction(mod, action));
    public static void ForceQueue(JAction action) => queue.Enqueue(action);
    public static void ForceQueue(JAMod mod, Action action) => ForceQueue(new JAction(mod, action));
    public static bool IsMainThread() => Thread.CurrentThread == Thread;
    public static Coroutine StartCoroutine(IEnumerator routine) => staticCoroutine.StartCoroutine(routine);
    public static void StopCoroutine(Coroutine routine) => staticCoroutine.StopCoroutine(routine);
    public static void StopCoroutine(IEnumerator routine) => staticCoroutine.StopCoroutine(routine);

    private class StaticCoroutine : MonoBehaviour;
}