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
        private char[]? chars;
        private int charsWritten;
        private int charsTranscoded;

        public DbUtf8Reader(DbDataReader reader)
        {
            this.reader = reader;
            this.chars = ArrayPool<char>.Shared.Rent(DefaultBufferSize);
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
            ObjectDisposedException.ThrowIf(this.chars is null, this);

            charsRead = 0;
            var actualLength = Math.Min(this.chars.Length - this.charsWritten, length / TranscodingFactor);
            if (actualLength > 0)
            {
                charsRead = (int)this.reader.GetChars(ordinal, dataOffset, this.chars, this.charsWritten, actualLength);
                if (charsRead > 0)
                {
                    this.charsWritten += charsRead;
                }
                else if (this.charsTranscoded == this.charsWritten)
                {
                    bytesWritten = 0;
                    return false;
                }
            }

            var status = Utf8.FromUtf16(this.chars.AsSpan(this.charsTranscoded, this.charsWritten - this.charsTranscoded),
                buffer.AsSpan(bufferOffset, length), out var charsTranscodedNow, out bytesWritten);

            if (status != OperationStatus.InvalidData)
            {
                this.charsTranscoded += charsTranscodedNow;
                if (this.charsTranscoded == this.charsWritten)
                {
                    this.charsWritten = 0;
                    this.charsTranscoded = 0;
                }

                return true;
            }

            bytesWritten = 0;
            return false;
        }

        private void Free()
        {
            var array = this.chars;
            if (array is not null)
            {
                ArrayPool<char>.Shared.Return(array);
                this.chars = null;
            }
        }
    }
}
