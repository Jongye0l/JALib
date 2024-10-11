using System.Threading.Tasks;

namespace JALib.API;

abstract class AsyncRequestPacket : RequestPacket {
    private TaskCompletionSource<bool> tcs;

    public bool Success => tcs.Task.Result;

    public async Task WaitResponse() {
        tcs ??= new TaskCompletionSource<bool>();
        await tcs.Task;
    }

    internal void CompleteResponse() {
        tcs.TrySetResult(true);
    }

    internal void FailResponse() {
        tcs.TrySetResult(false);
    }
}