using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JALib.Core;

namespace JALib.Tools;

public static class JATask {
    public static Task Run(JAMod mod, Action action) => Task.Run(new A(mod, action).Run);
    public static Task Run(JAction action) => Task.Run(action.Invoke);
    public static Task Run(JAMod mod, Action action, CancellationToken cancellationToken) => Task.Run(new A(mod, action).Run, cancellationToken);
    public static Task Run(JAction action, CancellationToken cancellationToken) => Task.Run(action.Invoke, cancellationToken);
    public static Task Run(JAMod mod, Func<Task> action) => Task.Run(new B(mod, action).Run);
    public static Task Run(JAMod mod, Func<Task> action, CancellationToken cancellationToken) => Task.Run(new B(mod, action).Run, cancellationToken);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<TResult> action) => Task.Run(new C<TResult>(mod, action).Run);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<TResult> action, CancellationToken cancellationToken) => Task.Run(new C<TResult>(mod, action).Run, cancellationToken);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<Task<TResult>> action) => Task.Run(new D<TResult>(mod, action).Run);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<Task<TResult>> action, CancellationToken cancellationToken) => Task.Run(new D<TResult>(mod, action).Run, cancellationToken);

    public static void CatchException(this Task task, JAMod mod) => Task.Run(new B(mod, task).Run);
    public static void CatchException<TResult>(this Task<TResult> task, JAMod mod) => Task.Run(new D<TResult>(mod, task).Run);
    public static void CatchExceptionSync(this Task task, JAMod mod) => new B(mod, task).Run();
    public static void CatchExceptionSync<TResult>(this Task<TResult> task, JAMod mod) => new D<TResult>(mod, task).Run();
    public static void OnCompleted(this Task task, JAMod mod, Action<Task> action, CompleteFlag flag = CompleteFlag.All) => new E(mod, task, action, flag).Run();
    public static void OnCompleted(this Task task, Action<Task> action) => new E(null, task, action, CompleteFlag.None).Run();
    public static void OnCompleted<TResult>(this Task<TResult> task, JAMod mod, Action<Task<TResult>> action, CompleteFlag flag = CompleteFlag.All) => new F<TResult>(mod, task, action, flag).Run();
    public static void OnCompleted<TResult>(this Task<TResult> task, Action<Task<TResult>> action) => new F<TResult>(null, task, action, CompleteFlag.None).Run();
    public static void OnCompletedAsync(this Task task, JAMod mod, Action<Task> action, CompleteFlag flag = CompleteFlag.All) => Task.Run(new E(mod, task, action, flag).Run);
    public static void OnCompletedAsync(this Task task, Action<Task> action) => Task.Run(new E(null, task, action, CompleteFlag.None).Run);
    public static void OnCompletedAsync<TResult>(this Task<TResult> task, JAMod mod, Action<Task<TResult>> action, CompleteFlag flag = CompleteFlag.All) => Task.Run(new F<TResult>(mod, task, action, flag).Run);
    public static void OnCompletedAsync<TResult>(this Task<TResult> task, Action<Task<TResult>> action) => Task.Run(new F<TResult>(null, task, action, CompleteFlag.None).Run);
    public static void OnCompleted(this Task task, JAMod mod, Action action) => new G(mod, task, action).Run();
    public static void OnCompleted(this Task task, Action action) => new H(task, action).Run();
    public static void OnCompletedAsync(this Task task, JAMod mod, Action action) => Task.Run(new G(mod, task, action).Run);
    public static void OnCompletedAsync(this Task task, Action action) => Task.Run(new H(task, action).Run);
    public static void OnCompleted(this YieldAwaitable awaitable, Action action) => awaitable.GetAwaiter().OnCompleted(action);
    
    private static void SendErrorMessage(JAMod mod, Exception e) => mod.LogReportException("An error occurred while running a task", e, 1);

    private class A {
        private JAMod mod;
        private Action action;
        
        internal A(JAMod mod, Action action) {
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

    private struct B : IAsyncStateMachine {
        private JAMod mod;
        private Func<Task> action;
        private Task task;
        private AsyncTaskMethodBuilder builder;

        internal B(JAMod mod, Func<Task> action) {
            this.mod = mod;
            this.action = action;
        }

        internal B(JAMod mod, Task task) {
            this.mod = mod;
            this.task = task;
        }

        internal Task Run() {
            task ??= action();
            builder = AsyncTaskMethodBuilder.Create();
            builder.Start(ref this);
            return builder.Task;
        }

        public void MoveNext() {
            if(!task.IsCompleted) {
                TaskAwaiter awaiter = task.GetAwaiter();
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

    private class C<T> {
        private JAMod mod;
        private Func<T> action;

        internal C(JAMod mod, Func<T> action) {
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

    private struct D<T> : IAsyncStateMachine {
        private JAMod mod;
        private Func<Task<T>> action;
        private Task<T> task;
        private AsyncTaskMethodBuilder<T> builder;

        internal D(JAMod mod, Func<Task<T>> action) {
            this.mod = mod;
            this.action = action;
        }

        internal D(JAMod mod, Task<T> task) {
            this.mod = mod;
            this.task = task;
        }

        internal Task<T> Run() {
            task ??= action();
            builder = AsyncTaskMethodBuilder<T>.Create();
            builder.Start(ref this);
            return builder.Task;
        }

        public void MoveNext() {
            TaskAwaiter<T> awaiter = task.GetAwaiter();
            if(!task.IsCompleted) {
                builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
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

    private class E {
        private JAMod mod;
        private Task task;
        private Action<Task> action;
        private CompleteFlag flag;
        
        internal E(JAMod mod, Task task, Action<Task> action, CompleteFlag flag) {
            this.mod = mod;
            this.task = task;
            this.action = action;
            this.flag = flag;
        }
        
        internal void Run() {
            if(!task.IsCompleted) {
                task.GetAwaiter().UnsafeOnCompleted(Run);
                return;
            }
            bool isFaulted = task.IsFaulted;
            if((flag & CompleteFlag.TryCatchTask) != 0 && isFaulted) {
                ReadOnlyCollection<Exception> exceptions = task.Exception!.InnerExceptions;
                SendErrorMessage(mod, exceptions.Count == 1 ? exceptions[0] : task.Exception);
            }
            if((flag & CompleteFlag.CompleteOnly) != 0 && isFaulted) return;
            if((flag & CompleteFlag.TryCatchAction) != 0) {
                try {
                    action(task);
                } catch (Exception e) {
                    SendErrorMessage(mod, e);
                }
            } else action(task);
        }
    }

    private class F<T> {
        private JAMod mod;
        private Task<T> task;
        private Action<Task<T>> action;
        private CompleteFlag flag;
        
        internal F(JAMod mod, Task<T> task, Action<Task<T>> action, CompleteFlag flag) {
            this.mod = mod;
            this.task = task;
            this.action = action;
            this.flag = flag;
        }

        internal void Run() {
            if(!task.IsCompleted) {
                task.GetAwaiter().UnsafeOnCompleted(Run);
                return;
            }
            bool isFaulted = task.IsFaulted;
            if(isFaulted && (flag & CompleteFlag.TryCatchTask) != 0) {
                ReadOnlyCollection<Exception> exceptions = task.Exception!.InnerExceptions;
                SendErrorMessage(mod, exceptions.Count == 1 ? exceptions[0] : task.Exception);
            }
            if(isFaulted && (flag & CompleteFlag.CompleteOnly) != 0) return;
            if((flag & CompleteFlag.TryCatchAction) != 0) {
                try {
                    action(task);
                } catch (Exception e) {
                    SendErrorMessage(mod, e);
                }
            } else action(task);
        }
    }

    private struct G {
        private JAMod mod;
        private Task task;
        private Action action;
        
        internal G(JAMod mod, Task task, Action action) {
            this.mod = mod;
            this.task = task;
            this.action = action;
        }

        internal void Run() {
            task.CatchException(mod);
            task.GetAwaiter().OnCompleted(new A(mod, action).Run);
        }
    }

    private struct H {
        private Task task;
        private Action action;
        
        internal H(Task task, Action action) {
            this.task = task;
            this.action = action;
        }
        
        internal void Run() {
            task.GetAwaiter().OnCompleted(action);
        }
    }

    [Flags]
    public enum CompleteFlag : byte {
        None = 0,
        TryCatchTask = 0x1,
        CompleteOnly = 0x2,
        TryCatchAction = 0x4,
        All = 255
    }
}