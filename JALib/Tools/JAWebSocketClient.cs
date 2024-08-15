using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JALib.Tools.ByteTool;

namespace JALib.Tools;

public class JAWebSocketClient : IDisposable {
    private ClientWebSocket socket;
    private JAction read;
    private Thread thread;
    private JAction onClose;
    private JAction onConnect;
    private readonly bool autoConnect;
    public bool Connected => socket.State == WebSocketState.Open;

    public JAWebSocketClient(JAction read = null, bool autoConnect = true) {
        socket = new ClientWebSocket();
        this.read = read;
        this.autoConnect = autoConnect;
    }

    public JAWebSocketClient(Uri uri, JAction read = null, bool autoConnect = true) : this(read, autoConnect) {
        Connect(uri);
    }

    public JAWebSocketClient(string uri, JAction read = null, bool autoConnect = true) : this(read, autoConnect) {
        Connect(uri);
    }

    public void Connect(Uri uri, CancellationToken token = default) {
        while(true) {
            socket.ConnectAsync(uri, token).Wait();
            if(!Connected) {
                if(!autoConnect) return;
                if(MainThread.IsMainThread()) throw new InvalidOperationException("Main thread cannot AutoConnect");
                Thread.Sleep(60000);
            }
            onConnect?.Invoke();
            if(read is not null || onClose is not null) Read();
            return;
        }
    }

    public void Connect(string uri, CancellationToken token = default) {
        Connect(new Uri(uri), token);
    }

    public async Task ConnectAsync(Uri uri, CancellationToken token = default) {
        while(true) {
            await socket.ConnectAsync(uri, token);
            if(!Connected) {
                if(!autoConnect) return;
                await Task.Delay(60000, token);
            }
            onConnect?.Invoke();
            if(read is not null || onClose is not null) Read();
            return;
        }
    }

    public Task ConnectAsync(string uri, CancellationToken token = default) {
        return ConnectAsync(new Uri(uri), token);
    }

    private void Read() {
        thread = new Thread(() => {
            while(Connected) {
                if(read is not null) read.Invoke();
                else Task.Yield();
            }
            onClose?.Invoke();
            thread = null;
        });
    }

    public void SetConnectAction(JAction action) {
        onConnect = action;
    }
    
    public void SetCloseAction(JAction action) {
        onClose = action;
        if(Connected && thread is null) Read();
    }

    private void CheckConnect() {
        if(!Connected) throw new InvalidOperationException(nameof(Socket) + " is not connected");
    }

    public byte ReadByte() {
        return ReadBytes(1)[0];
    }
    
    public short ReadShort() {
        return ReadBytes(2).ToShort();
    }
    
    public int ReadInt() {
        return ReadBytes(4).ToInt();
    }
    
    public long ReadLong() {
        return ReadBytes(8).ToLong();
    }
    
    public float ReadFloat() {
        return ReadBytes(4).ToFloat();
    }
    
    public double ReadDouble() {
        return ReadBytes(8).ToDouble();
    }
    
    public byte[] ReadBytes(int count, bool force = true) {
        CheckConnect();
        byte[] buffer = new byte[count];
        if(count == 0) return buffer;
        WebSocketReceiveResult result = socket.ReceiveAsync(buffer, CancellationToken.None).Result;
        if(result.Count != count) throw new InvalidOperationException("Failed to read bytes");
        return buffer;
    }
    
    public bool ReadBoolean() {
        return ReadByte() != 0;
    }

    public byte[] ReadBytesAndCount() {
        return ReadBytes(ReadInt());
    }
    
    public string ReadUTF() {
        return Encoding.UTF8.GetString(ReadBytesAndCount());
    }
    
    public async Task<byte> ReadAsyncByte() {
        return (await ReadAsyncBytes(1))[0];
    }
    
    public async Task<short> ReadAsyncShort() {
        return (await ReadAsyncBytes(2)).ToShort();
    }
    
    public async Task<int> ReadAsyncInt() {
        return (await ReadAsyncBytes(4)).ToInt();
    }
    
    public async Task<long> ReadAsyncLong() {
        return (await ReadAsyncBytes(8)).ToLong();
    }
    
    public async Task<float> ReadAsyncFloat() {
        return (await ReadAsyncBytes(4)).ToFloat();
    }
    
    public async Task<double> ReadAsyncDouble() {
        return (await ReadAsyncBytes(8)).ToDouble();
    }

    public async Task<byte[]> ReadAsyncBytes(int count, bool force = true) {
        CheckConnect();
        byte[] buffer = new byte[count];
        if(count == 0) return buffer;
        WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        if(result.Count != count) throw new InvalidOperationException("Failed to read bytes");
        return buffer;
    }
    
    public async Task<bool> ReadAsyncBoolean() {
        return await ReadAsyncByte() != 0;
    }

    public async Task<byte[]> ReadAsyncBytesAndCount() {
        return await ReadAsyncBytes(await ReadAsyncInt());
    }
    
    public async Task<string> ReadAsyncUTF() {
        return Encoding.UTF8.GetString(await ReadAsyncBytes(await ReadAsyncInt()));
    }
    
    public void WriteBytes(byte[] data, bool endOfMessage = true) {
        CheckConnect();
        socket.SendAsync(data, WebSocketMessageType.Binary, endOfMessage, CancellationToken.None).Wait();
    }

    public void WriteBytesAndCount(byte[] data, bool endOfMessage = true) {
        WriteInt(data.Length, false);
        WriteBytes(data, endOfMessage);
    }
    
    public void WriteByte(byte value, bool endOfMessage = true) {
        WriteBytes(new[] { value }, endOfMessage);
    }
    
    public void WriteShort(short value, bool endOfMessage = true) {
        WriteBytes(value.ToBytes(), endOfMessage);
    }
    
    public void WriteInt(int value, bool endOfMessage = true) {
        WriteBytes(value.ToBytes(), endOfMessage);
    }
    
    public void WriteLong(long value, bool endOfMessage = true) {
        WriteBytes(value.ToBytes(), endOfMessage);
    }
    
    public void WriteFloat(float value, bool endOfMessage = true) {
        WriteBytes(value.ToBytes(), endOfMessage);
    }
    
    public void WriteDouble(double value, bool endOfMessage = true) {
        WriteBytes(value.ToBytes(), endOfMessage);
    }
    
    public void WriteBoolean(bool value, bool endOfMessage = true) {
        WriteByte((byte) (value ? 1 : 0), endOfMessage);
    }
    
    public void WriteUTF(string value, bool endOfMessage = true) {
        WriteBytesAndCount(Encoding.UTF8.GetBytes(value), endOfMessage);
    }
    
    public async Task WriteAsyncBytes(byte[] data, bool endOfMessage = true) {
        CheckConnect();
        await socket.SendAsync(data, WebSocketMessageType.Binary, endOfMessage, CancellationToken.None);
    }
    
    public async Task WriteAsyncBytesAndCount(byte[] data, bool endOfMessage = true) {
        await WriteAsyncInt(data.Length, false);
        await WriteAsyncBytes(data, endOfMessage);
    }
    
    public async Task WriteAsyncByte(byte value, bool endOfMessage = true) {
        await WriteAsyncBytes(new[] { value }, endOfMessage);
    }
    
    public async Task WriteAsyncShort(short value, bool endOfMessage = true) {
        await WriteAsyncBytes(value.ToBytes(), endOfMessage);
    }
    
    public async Task WriteAsyncInt(int value, bool endOfMessage = true) {
        await WriteAsyncBytes(value.ToBytes(), endOfMessage);
    }
    
    public async Task WriteAsyncLong(long value, bool endOfMessage = true) {
        await WriteAsyncBytes(value.ToBytes(), endOfMessage);
    }
    
    public async Task WriteAsyncFloat(float value, bool endOfMessage = true) {
        await WriteAsyncBytes(value.ToBytes(), endOfMessage);
    }
    
    public async Task WriteAsyncDouble(double value, bool endOfMessage = true) {
        await WriteAsyncBytes(value.ToBytes(), endOfMessage);
    }
    
    public async Task WriteAsyncBoolean(bool value, bool endOfMessage = true) {
        await WriteAsyncByte((byte) (value ? 1 : 0), endOfMessage);
    }
    
    public async Task WriteAsyncUTF(string value, bool endOfMessage = true) {
        await WriteAsyncBytesAndCount(Encoding.UTF8.GetBytes(value), endOfMessage);
    }

    public void Dispose() {
        socket.Dispose();
        GC.SuppressFinalize(this);
    }
}