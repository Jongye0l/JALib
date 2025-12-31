using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JALib.Core;

namespace JALib.Tools;

public static class JATask {
    public static Task Run(JAMod mod, Action action) => Task.Run(new JATask1(mod, action).Run);
    public static Task Run(JAction action) => Task.Run(action.Invoke);
    public static Task Run(JAMod mod, Action action, CancellationToken cancellationToken) => Task.Run(new JATask1(mod, action).Run, cancellationToken);
    public static Task Run(JAction action, CancellationToken cancellationToken) => Task.Run(action.Invoke, cancellationToken);
    public static Task Run(JAMod mod, Func<Task> action) => Task.Run(new JATask2(mod, action).Run);
    public static Task Run(JAMod mod, Func<Task> action, CancellationToken cancellationToken) => Task.Run(new JATask2(mod, action).Run, cancellationToken);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<TResult> action) => Task.Run(new JATask3<TResult>(mod, action).Run);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<TResult> action, CancellationToken cancellationToken) => Task.Run(new JATask3<TResult>(mod, action).Run, cancellationToken);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<Task<TResult>> action) => Task.Run(new JATask4<TResult>(mod, action).Run);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<Task<TResult>> action, CancellationToken cancellationToken) => Task.Run(new JATask4<TResult>(mod, action).Run, cancellationToken);

    public static void CatchException(this Task task, JAMod mod) => Task.Run(new JATask2(mod, task).Run);
    public static void CatchException<TResult>(this Task<TResult> task, JAMod mod) => Task.Run(new JATask4<TResult>(mod, task).Run);
    public static void CatchExceptionSync(this Task task, JAMod mod) => new JATask2(mod, task).Run();
    public static void CatchExceptionSync<TResult>(this Task<TResult> task, JAMod mod) => new JATask4<TResult>(mod, task).Run();
    
    private static void SendErrorMessage(JAMod mod, Exception e) => mod.LogReportException("An error occurred while running a task", e, 1);

    private class JATask1 {
        private JAMod mod;
        private Action action;
        
        internal JATask1(JAMod mod, Action action) {
            this.mod = mod;
            this.action = action;
        }

        internal void Run() {
            try {
                action();
            } catch (Exception e) {
                SendErrorMessage(mod, e);
            }
        }
    }

    private struct JATask2 : IAsyncStateMachine {
        private JAMod mod;
        private Func<Task> action;
        private Task task;
        private TaskAwaiter awaiter;
        private AsyncTaskMethodBuilder builder;

        internal JATask2(JAMod mod, Func<Task> action) {
            this.mod = mod;
            this.action = action;
        }

        internal JATask2(JAMod mod, Task task) {
            this.mod = mod;
            this.task = task;
        }

        internal Task Run() {
            task ??= action();
            awaiter = task.GetAwaiter();
            builder = AsyncTaskMethodBuilder.Create();
            builder.Start(ref this);
            return builder.Task;
        }

        public void MoveNext() {
            if(!task.IsCompleted) {
                builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                return;
            }
            if(task.IsFaulted) {
                ReadOnlyCollection<Exception> exceptions = task.Exception!.InnerExceptions;
                SendErrorMessage(mod, exceptions.Count == 1 ? exceptions[0] : task.Exception);
            }
            builder.SetResult();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) {
        }
    }

    private class JATask3<T> {
        private JAMod mod;
        private Func<T> action;

        internal JATask3(JAMod mod, Func<T> action) {
            this.mod = mod;
            this.action = action;
        }

        internal T Run() {
            try {
                return action();
            } catch (Exception e) {
                SendErrorMessage(mod, e);
                return default;
            }
        }
    }

    private struct JATask4<T> : IAsyncStateMachine {
        private JAMod mod;
        private Func<Task<T>> action;
        private Task<T> task;
        private TaskAwaiter<T> awaiter;
        private AsyncTaskMethodBuilder<T> builder;

        internal JATask4(JAMod mod, Func<Task<T>> action) {
            this.mod = mod;
            this.action = action;
        }

        internal JATask4(JAMod mod, Task<T> task) {
            this.mod = mod;
            this.task = task;
        }

        internal Task<T> Run() {
            task ??= action();
            awaiter = task.GetAwaiter();
            builder = AsyncTaskMethodBuilder<T>.Create();
            builder.Start(ref this);
            return builder.Task;
        }

        public void MoveNext() {
            if(!task.IsCompleted) {
                builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                awaiter.UnsafeOnCompleted(MoveNext);
                return;
            }
            if(task.IsFaulted) {
                ReadOnlyCollection<Exception> exceptions = task.Exception!.InnerExceptions;
                SendErrorMessage(mod, exceptions.Count == 1 ? exceptions[0] : task.Exception);
                builder.SetResult(default);
            } else builder.SetResult(awaiter.GetResult());
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) {
        }
    }
}