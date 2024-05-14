using System;
using System.Threading;
using System.Threading.Tasks;
using JALib.Core;

namespace JALib.Tools;

public class JATask {
    public static Task Run(JAMod mod, Action action) {
        return Task.Run(() => {
            try {
                action.Invoke();
            } catch (Exception e) {
                mod.Error("An error occurred while running a task.");
                mod.LogException(e);
                ErrorUtils.ShowError(mod, e);
            }
        });
    }

    public static Task Run(JAction action) {
        return Task.Run(action.Invoke);
    }

    public static Task Run(JAMod mod, Action action, CancellationToken cancellationToken) {
        return Task.Run(() => {
            try {
                action.Invoke();
            } catch (Exception e) {
                mod.Error("An error occurred while running a task.");
                mod.LogException(e);
                ErrorUtils.ShowError(mod, e);
            }
        }, cancellationToken);
    }
    
    public static Task Run(JAction action, CancellationToken cancellationToken) {
        return Task.Run(action.Invoke, cancellationToken);
    }
    
    public static Task Run(JAMod mod, Func<Task> action) {
        return Task.Run(async () => {
            try {
                await action.Invoke();
            } catch (Exception e) {
                mod.Error("An error occurred while running a task.");
                mod.LogException(e);
                ErrorUtils.ShowError(mod, e);
            }
        });
    }
    
    public static Task Run(Func<Task> action) {
        return Task.Run(action.Invoke);
    }
    
    public static Task Run(JAMod mod, Func<Task> action, CancellationToken cancellationToken) {
        return Task.Run(async () => {
            try {
                await action.Invoke();
            } catch (Exception e) {
                mod.Error("An error occurred while running a task.");
                mod.LogException(e);
                ErrorUtils.ShowError(mod, e);
            }
        }, cancellationToken);
    }
    
    public static Task<TResult> Run<TResult>(JAMod mod, Func<TResult> action) {
        return Task.Run(() => {
            try {
                return action.Invoke();
            } catch (Exception e) {
                mod.Error("An error occurred while running a task.");
                mod.LogException(e);
                ErrorUtils.ShowError(mod, e);
                return default;
            }
        });
    }
    
    public static Task<TResult> Run<TResult>(JAMod mod, Func<TResult> action, CancellationToken cancellationToken) {
        return Task.Run(() => {
            try {
                return action.Invoke();
            } catch (Exception e) {
                mod.Error("An error occurred while running a task.");
                mod.LogException(e);
                ErrorUtils.ShowError(mod, e);
                return default;
            }
        }, cancellationToken);
    }
}