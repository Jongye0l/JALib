using System.Threading.Tasks;

namespace JALib.API;

internal abstract class AsyncRequestPacket : RequestPacket {
    private TaskCompletionSource<bool> tcs;
    
    public async Task WaitResponse() {
        tcs ??= new TaskCompletionSource<bool>();
        await tcs.Task;
    }

    internal void CompleteResponse() {
        tcs.TrySetResult(true);
    }
}