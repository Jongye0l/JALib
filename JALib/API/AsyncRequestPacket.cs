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
        tcs.TrySetResult(true);
    }

    internal void FailResponse() {
        tcs.TrySetResult(false);
    }
}