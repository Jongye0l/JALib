using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JALib.Core;

namespace JALib.Tools;

public class JATask {
    public static Task Run(JAMod mod, Action action) => Task.Run(new JATask(mod, action).Run);
    public static Task Run(JAction action) => Task.Run(action.Invoke);
    public static Task Run(JAMod mod, Action action, CancellationToken cancellationToken) => Task.Run(new JATask(mod, action).Run, cancellationToken);
    public static Task Run(JAction action, CancellationToken cancellationToken) => Task.Run(action.Invoke, cancellationToken);
    public static Task Run(JAMod mod, Func<Task> action) => Task.Run(new JATask2(mod, action).Run);
    public static Task Run(JAMod mod, Func<Task> action, CancellationToken cancellationToken) => Task.Run(new JATask2(mod, action).Run, cancellationToken);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<TResult> action) => Task.Run(new JATask3<TResult>(mod, action).Run);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<TResult> action, CancellationToken cancellationToken) => Task.Run(new JATask3<TResult>(mod, action).Run, cancellationToken);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<Task<TResult>> action) => Task.Run(new JATask4<TResult>(mod, action).Run);
    public static Task<TResult> Run<TResult>(JAMod mod, Func<Task<TResult>> action, CancellationToken cancellationToken) => Task.Run(new JATask4<TResult>(mod, action).Run, cancellationToken);

    private JAMod mod;
    private Action action;

    private JATask(JAMod mod, Action action) {
        this.mod = mod;
        this.action = action;
    }

    private void Run() {
        try {
            action();
        } catch (Exception e) {
            string key = "An error occurred while running a task.";
            mod.LogException(key, e);
            mod.ReportException(key, e);
        }
    }

    private class JATask2 : IAsyncStateMachine {
        private JAMod mod;
        private Func<Task> action;
        private Task task;
        private AsyncTaskMethodBuilder builder;

        internal JATask2(JAMod mod, Func<Task> action) {
            this.mod = mod;
            this.action = action;
        }

        internal Task Run() {
            builder = AsyncTaskMethodBuilder.Create();
            task = action();
            JATask2 stateMachine = this;
            builder.Start(ref stateMachine);
            return builder.Task;
        }

        public void MoveNext() {
            if(!task.IsCompleted) {
                task.GetAwaiter().UnsafeOnCompleted(MoveNext);
                return;
            }
            try {
                task.GetAwaiter().GetResult();
            } catch (Exception e) {
                string key = "An error occurred while running a task.";
                mod.LogException(key, e);
                mod.ReportException(key, e);
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
                string key = "An error occurred while running a task.";
                mod.LogException(key, e);
                mod.ReportException(key, e);
                return default;
            }
        }
    }

    private class JATask4<T> : IAsyncStateMachine {
        private JAMod mod;
        private Func<Task<T>> action;
        private Task<T> task;
        private AsyncTaskMethodBuilder<T> builder;

        internal JATask4(JAMod mod, Func<Task<T>> action) {
            this.mod = mod;
            this.action = action;
        }

        internal Task<T> Run() {
            task = action();
            builder = AsyncTaskMethodBuilder<T>.Create();
            JATask4<T> stateMachine = this;
            builder.Start(ref stateMachine);
            return builder.Task;
        }

        public void MoveNext() {
            if(!task.IsCompleted) {
                task.GetAwaiter().UnsafeOnCompleted(MoveNext);
                return;
            }
            try {
                builder.SetResult(task.GetAwaiter().GetResult());
            } catch (Exception e) {
                string key = "An error occurred while running a task.";
                mod.LogException(key, e);
                mod.ReportException(key, e);
            }
            builder.SetResult(default);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) {
        }
    }
}