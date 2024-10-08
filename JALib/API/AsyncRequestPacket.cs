using System;
using System.Threading.Tasks;

namespace JALib.API;

abstract class AsyncRequestPacket : RequestPacket {
    private TaskCompletionSource<bool> tcs;

    public async Task WaitResponse() {
        tcs ??= new TaskCompletionSource<bool>();
        await tcs.Task;
    }

    public bool Success => tcs.Task.Result;

    internal void CompleteResponse() {
        tcs ??= new TaskCompletionSource<bool>();
        tcs.TrySetResult(true);
    }

    internal void FailResponse(Exception e) {
        tcs ??= new TaskCompletionSource<bool>();
        tcs.TrySetException(e);
    }
}