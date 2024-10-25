using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using JALib.Tools.ByteTool;
using JetBrains.Annotations;

namespace JALib.Tools;

public class JATcpClient : TcpClient {
    private NetworkStream stream;
    private JAction read;
    private Thread thread;
    private JAction onClose;
    private JAction onConnect;
    private readonly bool autoConnect;

    public JATcpClient([NotNull] IPEndPoint localEP, JAction read = null, bool autoConnect = true) : base(localEP) {
        stream = GetStream();
        this.read = read;
        this.autoConnect = autoConnect;
        if(this.read is null && onClose is null) return;
        Read();
    }

    public JATcpClient(JAction read = null, bool autoConnect = true) {
        this.read = read;
        this.autoConnect = autoConnect;
    }

    public JATcpClient(AddressFamily family, JAction read = null, bool autoConnect = true) : base(family) {
        this.read = read;
        this.autoConnect = autoConnect;
    }

    public JATcpClient([NotNull] string hostname, int port, JAction read = null, bool autoConnect = true) {
        this.read = read;
        this.autoConnect = autoConnect;
        Connect(hostname, port);
    }

    public JATcpClient([NotNull] string hostname, string service, JAction read = null, bool autoConnect = true) {
        this.read = read;
        this.autoConnect = autoConnect;
        Connect(hostname, service);
    }

    public JATcpClient([NotNull] string hostname, int port, string service, bool onlyThisPort = false, JAction read = null, bool autoConnect = true) {
        this.read = read;
        this.autoConnect = autoConnect;
        Connect(hostname, port, service, onlyThisPort);
    }

    public new void Connect(string host, int port) {
        while(true) {
            try {
                base.Connect(host, port);
                stream = GetStream();
                onConnect?.Invoke();
                if(read is not null || onClose is not null) Read();
                return;
            } catch (Exception) {
                if(!autoConnect) throw;
                if(MainThread.IsMainThread()) throw new InvalidOperationException("Main thread cannot AutoConnect");
                Thread.Sleep(60000);
            }
        }
    }

    public void Connect(string host, string service) {
        Connect(host, -1, service);
    }

    public void Connect(string host, int port, string service, bool onlyThisPort = false) {
        try {
            string domain = $"_{service}._tcp.{host}";
            host = (onlyThisPort ? GetSrvRecord(domain, port) : GetSrvRecord(domain, ref port)) ?? host;
            if(host[^1] == '.') host = host[..^1];
        } catch (Exception e) {
            if(port == -1) throw;
            JALib.Instance.LogException(e);
        }
        Connect(host, port);
    }

    private static string GetSrvRecord(string domain, ref int port) {
        LookupClient client = new();
        IDnsQueryResponse response = client.Query(domain, QueryType.SRV);
        foreach(SrvRecord record in response.Answers.SrvRecords()) {
            port = record.Port;
            return record.Target.Value;
        }
        return null;
    }

    private static string GetSrvRecord(string domain, int port) {
        LookupClient client = new();
        IDnsQueryResponse response = client.Query(domain, QueryType.SRV);
        return (from record in response.Answers.SrvRecords() where record.Port == port select record.Target.Value).FirstOrDefault();
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
        stream ??= GetStream();
    }

    public byte ReadByte() {
        CheckConnect();
        return (byte) stream.ReadByte();
    }

    public short ReadShort() => ReadBytes(2).ToShort();
    public int ReadInt() => ReadBytes(4).ToInt();
    public long ReadLong() => ReadBytes(8).ToLong();
    public float ReadFloat() => ReadBytes(4).ToFloat();
    public double ReadDouble() => ReadBytes(8).ToDouble();
    public bool ReadBoolean() => ReadByte() != 0;
    public byte[] ReadBytesAndCount() => ReadBytes(ReadInt());
    public string ReadUTF() => Encoding.UTF8.GetString(ReadBytesAndCount());

    public byte[] ReadBytes(int count, bool force = true) {
        CheckConnect();
        byte[] buffer = new byte[count];
        if(count == 0) return buffer;
        if(force) {
            int offset = 0;
            while(offset < count) offset += stream.Read(buffer, offset, count - offset);
        } else if(stream.Read(buffer, 0, count) != count) throw new InvalidOperationException("Failed to read bytes");
        return buffer;
    }

    public Task<byte> ReadAsyncByte() => ReadAsyncBytes(1).ContinueWith(task => task.Result[0]);
    public Task<short> ReadAsyncShort() => ReadAsyncBytes(2).ContinueWith(task => task.Result.ToShort());
    public Task<int> ReadAsyncInt() => ReadAsyncBytes(4).ContinueWith(task => task.Result.ToInt());
    public Task<long> ReadAsyncLong() => ReadAsyncBytes(8).ContinueWith(task => task.Result.ToLong());
    public Task<float> ReadAsyncFloat() => ReadAsyncBytes(4).ContinueWith(task => task.Result.ToFloat());
    public Task<double> ReadAsyncDouble() => ReadAsyncBytes(8).ContinueWith(task => task.Result.ToDouble());
    public Task<bool> ReadAsyncBoolean() => ReadAsyncBytes(1).ContinueWith(task => task.Result[0] != 0);
    public Task<byte[]> ReadAsyncBytesAndCount() => ReadAsyncInt().ContinueWith(task => ReadAsyncBytes(task.Result)).Unwrap();
    public Task<string> ReadAsyncUTF() => ReadAsyncBytesAndCount().ContinueWith(task => Encoding.UTF8.GetString(task.Result));

    public async Task<byte[]> ReadAsyncBytes(int count, bool force = true) {
        CheckConnect();
        byte[] buffer = new byte[count];
        if(count == 0) return buffer;
        if(force) {
            int offset = 0;
            while(offset < count) offset += await stream.ReadAsync(buffer, offset, count - offset);
        } else if(stream.Read(buffer, 0, count) != count) throw new InvalidOperationException("Failed to read bytes");
        return buffer;
    }

    public void WriteBytes(byte[] data) {
        CheckConnect();
        stream.Write(data);
    }

    public void WriteBytesAndCount(byte[] data) {
        WriteInt(data.Length);
        WriteBytes(data);
    }

    public void WriteByte(byte value) {
        CheckConnect();
        stream.WriteByte(value);
    }

    public void WriteShort(short value) => WriteBytes(value.ToBytes());
    public void WriteInt(int value) => WriteBytes(value.ToBytes());
    public void WriteLong(long value) => WriteBytes(value.ToBytes());
    public void WriteFloat(float value) => WriteBytes(value.ToBytes());
    public void WriteDouble(double value) => WriteBytes(value.ToBytes());
    public void WriteBoolean(bool value) => WriteByte((byte) (value ? 1 : 0));
    public void WriteUTF(string value) => WriteBytesAndCount(Encoding.UTF8.GetBytes(value));

    public Task WriteAsyncBytes(byte[] data) {
        CheckConnect();
        return stream.WriteAsync(data).AsTask();
    }

    public Task WriteAsyncBytesAndCount(byte[] data) => WriteAsyncBytes(data.Length.ToBytes().Concat(data).ToArray());
    public Task WriteAsyncByte(byte value) => WriteAsyncBytes([value]);
    public Task WriteAsyncShort(short value) => WriteAsyncBytes(value.ToBytes());
    public Task WriteAsyncInt(int value) => WriteAsyncBytes(value.ToBytes());
    public Task WriteAsyncLong(long value) => WriteAsyncBytes(value.ToBytes());
    public Task WriteAsyncFloat(float value) => WriteAsyncBytes(value.ToBytes());
    public Task WriteAsyncDouble(double value) => WriteAsyncBytes(value.ToBytes());
    public Task WriteAsyncBoolean(bool value) => WriteAsyncByte((byte) (value ? 1 : 0));
    public Task WriteAsyncUTF(string value) => WriteAsyncBytesAndCount(Encoding.UTF8.GetBytes(value));

    public new void Close() {
        Dispose();
    }
}