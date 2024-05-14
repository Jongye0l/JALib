using System;
using System.Text;
using System.Threading.Tasks;
using JALib.Core;
using JALib.Tools;

namespace JALib.Stream;

public class ByteArrayDataInput : IDisposable, IAsyncDisposable {
    private byte[] data;
    private int cur;
    private JAMod mod;

    public ByteArrayDataInput(byte[] data, JAMod mod = null) {
        this.data = data;
        this.mod = mod;
    }

    public void Dispose() {
        data = null;
        mod = null;
    }

    public async ValueTask DisposeAsync() {
        if(mod == null) await Task.Run(Dispose);
        else await JATask.Run(mod, Dispose);
    }
    
    public string ReadUTF() {
        return Encoding.UTF8.GetString(ReadBytes());
    }

    public int ReadInt() {
        return (data[cur++] << 24) + (data[cur++] << 16) + (data[cur++] << 8) + (data[cur++] << 0);
    }

    public long ReadLong() {
        return ((long) data[cur++] << 56) + 
               ((long) (data[cur++]&255) << 48) + 
               ((long) (data[cur++]&255) << 40) + 
               ((long) (data[cur++]&255) << 32) + 
               ((long) (data[cur++]&255) << 24) + 
               (data[cur++]&255 << 16) + 
               (data[cur++]&255 << 8) + 
               (data[cur++]&255 << 0);
    }

    public bool ReadBoolean() {
        return data[cur++] != 0;
    }

    public float ReadFloat() {
        Array.Reverse(data, cur, 4);
        float f = BitConverter.ToSingle(data, cur);
        cur += 4;
        return f;
    }

    public double ReadDouble() {
        Array.Reverse(data, cur, 8);
        double d = BitConverter.ToDouble(data, cur);
        cur += 8;
        return d;
    }

    public byte ReadByte() {
        return data[cur++];
    }

    public short ReadShort() {
        return (short) ((data[cur++] << 8) + data[cur++]);
    }

    public byte[] ReadBytes() {
        byte[] buffer = new byte[ReadInt()];
        for(int i = 0; i < buffer.Length; i++) buffer[i] = data[cur++];
        return buffer;
    }
}