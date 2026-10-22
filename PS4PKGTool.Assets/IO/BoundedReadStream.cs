namespace PS4PKGTool.Assets.IO;

/// <summary>
/// A read-only stream view over a bounded range of a parent stream. Reads past
/// the bound return 0 (EOF). Used so slices never allocate or touch bytes
/// outside their range.
/// </summary>
public sealed class BoundedReadStream : Stream
{
    private readonly Stream _inner;
    private readonly long _length;
    private readonly bool _leaveOpen;
    private long _position;

    public BoundedReadStream(Stream inner, long length, bool leaveOpen = true)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _length = Math.Max(0, length);
        _leaveOpen = leaveOpen;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _position = value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int remaining = (int)Math.Min(count, _length - _position);
        if (remaining <= 0) return 0;
        int read = _inner.Read(buffer, offset, remaining);
        _position += read;
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        int remaining = (int)Math.Min(buffer.Length, _length - _position);
        if (remaining <= 0) return 0;
        int read = _inner.Read(buffer[..remaining]);
        _position += read;
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int remaining = (int)Math.Min(buffer.Length, _length - _position);
        if (remaining <= 0) return 0;
        int read = await _inner.ReadAsync(buffer[..remaining], cancellationToken);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        if (target < 0) throw new IOException("Cannot seek before the start of the slice.");
        _position = target;
        return _position;
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen)
            _inner.Dispose();
        base.Dispose(disposing);
    }
}
