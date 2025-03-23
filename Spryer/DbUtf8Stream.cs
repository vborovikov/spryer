namespace Spryer;

using System;
using System.Buffers;
using System.Data.Common;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides a <see cref="Stream"/> implementation for reading UTF-8 bytes from a <see cref="DbDataReader"/>.
/// </summary>
sealed class DbUtf8Stream : Stream
{
    private readonly DbUtf8Reader reader;
    private long dataOffset;

    public DbUtf8Stream(DbDataReader reader)
    {
        this.reader = new(reader);
        this.dataOffset = -1L;
    }

    public override bool CanRead => !this.reader.IsClosed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.reader.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await this.reader.DisposeAsync();
        await base.DisposeAsync();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var total = 0;

        while (total < count)
        {
            if (this.dataOffset < 0L)
            {
                if (!this.reader.Read())
                {
                    break;
                }
                this.dataOffset = 0L;
            }

            if (this.reader.TryGetBytes(0, this.dataOffset, out var dataRead, buffer, offset + total, count - total, out var read))
            {
                total += (int)read;
                this.dataOffset += dataRead;
            }
            else
            {
                this.dataOffset = -1L;
            }
        }

        return total;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var total = 0;

        while (total < count)
        {
            if (this.dataOffset < 0L)
            {
                if (!await this.reader.ReadAsync(cancellationToken))
                {
                    break;
                }

                this.dataOffset = 0L;
            }

            if (this.reader.TryGetBytes(0, this.dataOffset, out var charsRead, buffer, offset + total, count - total, out var bytesWritten))
            {
                total += bytesWritten;
                this.dataOffset += charsRead;
            }
            else
            {
                this.dataOffset = -1L;
            }
        }

        return total;
    }

    public override void Flush() => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <summary>
    /// Decodes UTF-16 characters into UTF-8 bytes.
    /// </summary>
    struct DbUtf8Reader : IDisposable, IAsyncDisposable
    {
        private const int TranscodingFactor = 3;
        private const int DefaultBufferSize = 1024 * 1024 * TranscodingFactor;

        private readonly DbDataReader reader;
        private char[]? buffer;
        private int written;
        private int transcoded;

        public DbUtf8Reader(DbDataReader reader)
        {
            this.reader = reader;
            this.buffer = ArrayPool<char>.Shared.Rent(DefaultBufferSize);
        }

        public readonly bool IsClosed => this.reader.IsClosed;

        public void Dispose()
        {
            Free();
            this.reader.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Free();
            return this.reader.DisposeAsync();
        }

        public readonly bool Read() => this.reader.Read();

        public readonly Task<bool> ReadAsync(CancellationToken cancellationToken) => this.reader.ReadAsync(cancellationToken);

        public bool TryGetBytes(int ordinal, long dataOffset, out int charsRead, byte[]? buffer, int bufferOffset, int length, out int bytesWritten)
        {
            ObjectDisposedException.ThrowIf(this.buffer is null, this);

            charsRead = 0;
            var actualLength = Math.Min(this.buffer.Length - this.written, length / TranscodingFactor);
            if (actualLength > 0)
            {
                charsRead = (int)this.reader.GetChars(ordinal, dataOffset, this.buffer, this.written, actualLength);
                if (charsRead > 0)
                {
                    MarkWritten(charsRead);
                }
                else if (this.transcoded == this.written)
                {
                    bytesWritten = 0;
                    return false;
                }
            }

            var status = Utf8.FromUtf16(this.buffer.AsSpan(this.transcoded, this.written - this.transcoded),
                buffer.AsSpan(bufferOffset, length), out var charsTranscoded, out bytesWritten);
            MarkTranscoded(charsTranscoded);

            return status != OperationStatus.InvalidData;
        }

        private void MarkWritten(int more)
        {
            ObjectDisposedException.ThrowIf(this.buffer is null, this);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(more);

            var writtenMore = this.written + more;
            ArgumentOutOfRangeException.ThrowIfGreaterThan(writtenMore, this.buffer.Length, nameof(more));
            this.written = writtenMore;
        }

        private void MarkTranscoded(int more)
        {
            ObjectDisposedException.ThrowIf(this.buffer is null, this);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(more);

            var transcodedMore = this.transcoded + more;
            ArgumentOutOfRangeException.ThrowIfGreaterThan(transcodedMore, this.written, nameof(more));
            this.transcoded = transcodedMore;

            if (this.transcoded == this.written)
            {
                this.written = 0;
                this.transcoded = 0;
            }
        }

        private void Free()
        {
            var array = this.buffer;
            if (array is not null)
            {
                ArrayPool<char>.Shared.Return(array);
                this.buffer = null;
            }
        }
    }
}
