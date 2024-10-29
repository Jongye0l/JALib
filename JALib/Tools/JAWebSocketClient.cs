using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JALib.Tools.ByteTool;

namespace JALib.Tools;

public class JAWebSocketClient(JAction read = null, bool autoConnect = true) : IDisposable {
    private readonly ClientWebSocket socket = new();
    private Thread thread;
    private JAction onClose;
    private JAction onConnect;
    private bool autoConnect = autoConnect;
    public bool Connected => socket.State == WebSocketState.Open;

    public JAWebSocketClient(Uri uri, JAction read = null, bool autoConnect = true) : this(read, autoConnect) => Connect(uri);

    public JAWebSocketClient(string uri, JAction read = null, bool autoConnect = true) : this(read, autoConnect) => Connect(uri);

    public void Connect(string uri, CancellationToken token = default) => Connect(new Uri(uri), token);
    public void Connect(Uri uri, CancellationToken token = default) => ConnectAsync(uri, token).Wait(token);
    public Task ConnectAsync(string uri, CancellationToken token = default) => ConnectAsync(new Uri(uri), token);
    public Task ConnectAsync(Uri uri, CancellationToken token = default) => new AsyncConnect(this, uri, token).tcs.Task;

    private class AsyncConnect {
        private readonly JAWebSocketClient client;
        private readonly Uri uri;
        private readonly CancellationToken token;
        internal readonly TaskCompletionSource<bool> tcs = new();
        private Task task;

        internal AsyncConnect(JAWebSocketClient client, Uri uri, CancellationToken token) {
            this.client = client;
            this.uri = uri;
            this.token = token;
            MoveNext();
        }

        public void MoveNext() {
            try {
                task ??= client.socket.ConnectAsync(uri, token);
                if(!task.IsCompleted) {
                    task.GetAwaiter().UnsafeOnCompleted(MoveNext);
                    return;
                }
                if(client.Connected) {
                    client.onConnect?.Invoke();
                    client.Read();
                    tcs.SetResult(true);
                } else if(client.autoConnect) {
                    task = null;
                    Task.Delay(60000, token).GetAwaiter().OnCompleted(MoveNext);
                }
            } catch (Exception e) {
                tcs.SetException(e);
            }
        }
    }

    private void Read() {
        if(read == null && onClose == null) return;
        thread = new Thread(() => {
            try {
                while(Connected) {
                    if(read is not null) read.Invoke();
                    else Task.Yield();
                }
                onClose?.Invoke();
                thread = null;
            } catch (ThreadAbortException) {
            }
        });
        thread.Start();
    }

    public void SetConnectAction(JAction action) => onConnect = action;

    public void SetCloseAction(JAction action) {
        onClose = action;
        if(Connected && thread is null) Read();
    }

    private void CheckConnect() {
        if(!Connected) throw new InvalidOperationException(nameof(Socket) + " is not connected");
    }

    public byte ReadByte() => ReadBytes(1)[0];
    public short ReadShort() => ReadBytes(2).ToShort();
    public int ReadInt() => ReadBytes(4).ToInt();
    public long ReadLong() => ReadBytes(8).ToLong();
    public float ReadFloat() => ReadBytes(4).ToFloat();
    public double ReadDouble() => ReadBytes(8).ToDouble();

    public byte[] ReadBytes(int count, bool force = true) {
        CheckConnect();
        byte[] buffer = new byte[count];
        if(count == 0) return buffer;
        WebSocketReceiveResult result = socket.ReceiveAsync(buffer, CancellationToken.None).Result;
        if(result.Count != count && force) throw new InvalidOperationException("Failed to read bytes");
        return buffer;
    }

    public byte[] ReadBytes() {
        using MemoryStream stream = ReadStream();
        return stream.ToArray();
    }

    public MemoryStream ReadStream() {
        CheckConnect();
        byte[] buffer = new byte[256];
        MemoryStream stream = new();
        while(true) {
            WebSocketReceiveResult result = socket.ReceiveAsync(buffer, CancellationToken.None).Result;
            stream.Write(buffer, 0, result.Count);
            if(result.EndOfMessage) break;
        }
        stream.Position = 0;
        return stream;
    }

    public bool ReadBoolean() => ReadByte() != 0;
    public byte[] ReadBytesAndCount() => ReadBytes(ReadInt());
    public string ReadUTF() => Encoding.UTF8.GetString(ReadBytesAndCount());
    public Task<byte> ReadAsyncByte() => ReadAsyncBytes(1).ContinueWith(task => task.Result[0]);
    public Task<short> ReadAsyncShort() => ReadAsyncBytes(2).ContinueWith(task => task.Result.ToShort());
    public Task<int> ReadAsyncInt() => ReadAsyncBytes(4).ContinueWith(task => task.Result.ToInt());
    public Task<long> ReadAsyncLong() => ReadAsyncBytes(8).ContinueWith(task => task.Result.ToLong());
    public Task<float> ReadAsyncFloat() => ReadAsyncBytes(4).ContinueWith(task => task.Result.ToFloat());
    public Task<double> ReadAsyncDouble() => ReadAsyncBytes(8).ContinueWith(task => task.Result.ToDouble());

    public Task<byte[]> ReadAsyncBytes(int count, bool force = true) {
        try {
            CheckConnect();
            byte[] buffer = new byte[count];
            if(count == 0) return Task.FromResult(buffer);
            AsyncReadByte asyncReadByte = new(buffer);
            socket.ReceiveAsync(buffer, CancellationToken.None).ContinueWith(asyncReadByte.Run);
            return asyncReadByte.tcs.Task;
        } catch (Exception e) {
            return Task.FromException<byte[]>(e);
        }
    }

    private class AsyncReadByte(byte[] buffer) {
        private byte[] buffer = buffer;
        public TaskCompletionSource<byte[]> tcs = new();

        public void Run(Task<WebSocketReceiveResult> result) {
            try {
                if(result.Result.Count != buffer.Length) throw new InvalidOperationException("Failed to read bytes");
                tcs.SetResult(buffer);
            } catch (Exception e) {
                tcs.SetException(e);
            }
        }
    }

    public Task<bool> ReadAsyncBoolean() => ReadAsyncBytes(1).ContinueWith(task => task.Result[0] != 0);
    public Task<byte[]> ReadAsyncBytesAndCount() => ReadAsyncInt().ContinueWith(t => ReadAsyncBytes(t.Result)).Unwrap();
    public Task<string> ReadAsyncUTF() => ReadAsyncBytesAndCount().ContinueWith(t => Encoding.UTF8.GetString(t.Result));

    public void WriteBytes(byte[] data, bool endOfMessage = true) {
        CheckConnect();
        socket.SendAsync(data, WebSocketMessageType.Binary, endOfMessage, CancellationToken.None).Wait();
    }

    public void WriteBytesAndCount(byte[] data, bool endOfMessage = true) => WriteBytes(data.Length.ToBytes().Concat(data).ToArray(), endOfMessage);
    public void WriteByte(byte value, bool endOfMessage = true) => WriteBytes([value], endOfMessage);
    public void WriteShort(short value, bool endOfMessage = true) => WriteBytes(value.ToBytes(), endOfMessage);
    public void WriteInt(int value, bool endOfMessage = true) => WriteBytes(value.ToBytes(), endOfMessage);
    public void WriteLong(long value, bool endOfMessage = true) => WriteBytes(value.ToBytes(), endOfMessage);
    public void WriteFloat(float value, bool endOfMessage = true) => WriteBytes(value.ToBytes(), endOfMessage);
    public void WriteDouble(double value, bool endOfMessage = true) => WriteBytes(value.ToBytes(), endOfMessage);
    public void WriteBoolean(bool value, bool endOfMessage = true) => WriteByte((byte) (value ? 1 : 0), endOfMessage);
    public void WriteUTF(string value, bool endOfMessage = true) => WriteBytesAndCount(Encoding.UTF8.GetBytes(value), endOfMessage);

    public Task WriteAsyncBytes(byte[] data, bool endOfMessage = true) {
        CheckConnect();
        return socket.SendAsync(data, WebSocketMessageType.Binary, endOfMessage, CancellationToken.None);
    }

    public Task WriteAsyncBytesAndCount(byte[] data, bool endOfMessage = true) => WriteAsyncBytes(data.Length.ToBytes().Concat(data).ToArray(), endOfMessage);
    public Task WriteAsyncByte(byte value, bool endOfMessage = true) => WriteAsyncBytes([value], endOfMessage);
    public Task WriteAsyncShort(short value, bool endOfMessage = true) => WriteAsyncBytes(value.ToBytes(), endOfMessage);
    public Task WriteAsyncInt(int value, bool endOfMessage = true) => WriteAsyncBytes(value.ToBytes(), endOfMessage);
    public Task WriteAsyncLong(long value, bool endOfMessage = true) => WriteAsyncBytes(value.ToBytes(), endOfMessage);
    public Task WriteAsyncFloat(float value, bool endOfMessage = true) => WriteAsyncBytes(value.ToBytes(), endOfMessage);
    public Task WriteAsyncDouble(double value, bool endOfMessage = true) => WriteAsyncBytes(value.ToBytes(), endOfMessage);
    public Task WriteAsyncBoolean(bool value, bool endOfMessage = true) => WriteAsyncByte((byte) (value ? 1 : 0), endOfMessage);
    public Task WriteAsyncUTF(string value, bool endOfMessage = true) => WriteAsyncBytesAndCount(Encoding.UTF8.GetBytes(value), endOfMessage);

    public void Dispose() {
        socket.Dispose();
        GC.SuppressFinalize(this);
    }
}