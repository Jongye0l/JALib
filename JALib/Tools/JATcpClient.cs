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

    public bool ReadBoolean() => ReadByte() != 0;

    public byte[] ReadBytesAndCount() => ReadBytes(ReadInt());

    public string ReadUTF() => Encoding.UTF8.GetString(ReadBytesAndCount());

    public async Task<byte> ReadAsyncByte() {
        CheckConnect();
        byte[] buffer = new byte[1];
        await stream.ReadAsync(buffer, 0, 1);
        return buffer[0];
    }

    public async Task<short> ReadAsyncShort() => (await ReadAsyncBytes(2)).ToShort();

    public async Task<int> ReadAsyncInt() => (await ReadAsyncBytes(4)).ToInt();

    public async Task<long> ReadAsyncLong() => (await ReadAsyncBytes(8)).ToLong();

    public async Task<float> ReadAsyncFloat() => (await ReadAsyncBytes(4)).ToFloat();

    public async Task<double> ReadAsyncDouble() => (await ReadAsyncBytes(8)).ToDouble();

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

    public async Task<bool> ReadAsyncBoolean() => await ReadAsyncByte() != 0;

    public async Task<byte[]> ReadAsyncBytesAndCount() => await ReadAsyncBytes(await ReadAsyncInt());

    public async Task<string> ReadAsyncUTF() => Encoding.UTF8.GetString(await ReadAsyncBytes(await ReadAsyncInt()));

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

    public void WriteShort(short value) {
        WriteBytes(value.ToBytes());
    }

    public void WriteInt(int value) {
        WriteBytes(value.ToBytes());
    }

    public void WriteLong(long value) {
        WriteBytes(value.ToBytes());
    }

    public void WriteFloat(float value) {
        WriteBytes(value.ToBytes());
    }

    public void WriteDouble(double value) {
        WriteBytes(value.ToBytes());
    }

    public void WriteBoolean(bool value) {
        WriteByte((byte) (value ? 1 : 0));
    }

    public void WriteUTF(string value) {
        WriteBytesAndCount(Encoding.UTF8.GetBytes(value));
    }

    public async Task WriteAsyncBytes(byte[] data) {
        CheckConnect();
        await stream.WriteAsync(data);
    }

    public async Task WriteAsyncBytesAndCount(byte[] data) {
        await WriteAsyncInt(data.Length);
        await WriteAsyncBytes(data);
    }

    public async Task WriteAsyncByte(byte value) {
        await WriteAsyncBytes(new[] { value });
    }

    public async Task WriteAsyncShort(short value) {
        await WriteAsyncBytes(value.ToBytes());
    }

    public async Task WriteAsyncInt(int value) {
        await WriteAsyncBytes(value.ToBytes());
    }

    public async Task WriteAsyncLong(long value) {
        await WriteAsyncBytes(value.ToBytes());
    }

    public async Task WriteAsyncFloat(float value) {
        await WriteAsyncBytes(value.ToBytes());
    }

    public async Task WriteAsyncDouble(double value) {
        await WriteAsyncBytes(value.ToBytes());
    }

    public async Task WriteAsyncBoolean(bool value) {
        await WriteAsyncByte((byte) (value ? 1 : 0));
    }

    public async Task WriteAsyncUTF(string value) {
        await WriteAsyncBytesAndCount(Encoding.UTF8.GetBytes(value));
    }

    public new void Close() {
        Dispose();
    }
}