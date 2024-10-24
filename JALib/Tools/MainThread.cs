﻿using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using JALib.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JALib.Tools;

public static class MainThread {
    public static Thread Thread { get; private set; }
    private static ConcurrentQueue<JAction> queue = new();
    private static StaticCoroutine staticCoroutine;

    internal static void Initialize() {
        queue ??= new ConcurrentQueue<JAction>();
        queue.Enqueue(new JAction(JALib.Instance, () => {
            Thread = Thread.CurrentThread;
            staticCoroutine = new GameObject("StaticCoroutine").AddComponent<StaticCoroutine>();
            Object.DontDestroyOnLoad(staticCoroutine.gameObject);
        }));
    }

    internal static void Dispose() {
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
        if(IsMainThread()) {
            action.Invoke();
            return;
        }
        queue.Enqueue(action);
    }

    public static void Run(JAMod mod, Action action) => Run(new JAction(mod, action));
    public static bool IsMainThread() => Thread.CurrentThread == Thread;
    public static Coroutine StartCoroutine(IEnumerator routine) => staticCoroutine.StartCoroutine(routine);
    public static void StopCoroutine(Coroutine routine) => staticCoroutine.StopCoroutine(routine);
    public static void StopCoroutine(IEnumerator routine) => staticCoroutine.StopCoroutine(routine);

    private class StaticCoroutine : MonoBehaviour;
}