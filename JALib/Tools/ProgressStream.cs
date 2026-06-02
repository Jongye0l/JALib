using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JALib.Tools;

public class ProgressStream(Stream baseStream, long length) : Stream {
    private long _position;
    private long _lastCheckedPosition;

    public bool NeedUpdate(out double value) {
        if(Length == -1) {
            value = 0;
            return false;
        }
        long currentPosition = _position;
        if(currentPosition == _lastCheckedPosition) {
            value = 0;
            return false;
        }
        value = (double) currentPosition / Length;
        _lastCheckedPosition = currentPosition;
        return true;
    }

    public override void Flush() => baseStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    
    public override int Read(byte[] buffer, int offset, int count) {
        int read = baseStream.Read(buffer, offset, count);
        _position += read;
        return read;
    }

    public override int ReadByte() {
        int read = baseStream.ReadByte();
        if(read != -1) _position++;
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        int read = await baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        _position += read;
        return read;
    }

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length { get; } = length;

    public override long Position {
        get => _position;
        set => throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing) {
        if(!disposing) return;
        baseStream.Dispose();
    }

    public override ValueTask DisposeAsync() => baseStream.DisposeAsync();
}