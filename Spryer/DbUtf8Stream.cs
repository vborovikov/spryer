namespace Spryer;

using System;
using System.Buffers;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides a <see cref="Stream"/> implementation for reading UTF-8 bytes from a <see cref="DbDataReader"/>.
/// </summary>
sealed class DbUtf8Stream : Stream
{
    private const int TranscodingFactor = 3;

    private readonly DbDataReader reader;
    private readonly DataBuffer<char> chars;
    private readonly DataBuffer<byte> bytes;
    private long dataOffset;

    public DbUtf8Stream(DbDataReader reader)
    {
        this.reader = reader;
        this.chars = new();
        this.bytes = new(TranscodingFactor);
        this.dataOffset = -1L;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.bytes.Dispose();
            this.chars.Dispose();
            this.reader.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await this.bytes.DisposeAsync();
        await this.chars.DisposeAsync();
        await this.reader.DisposeAsync();
        await base.DisposeAsync();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var doneReading = this.reader.IsClosed;

        // read more data
        if (!doneReading)
        {
            while (this.chars.SpanLength > 0)
            {
                if (this.dataOffset < 0L)
                {
                    if (!TryReadRow())
                    {
                        doneReading = true;
                        break;
                    }
                    this.dataOffset = 0L;
                }

                var charsRead = (int)this.reader.GetChars(0, this.dataOffset, this.chars, this.chars.SpanOffset, this.chars.SpanLength);
                if (charsRead > 0)
                {
                    this.chars.MarkUsed(charsRead);
                    this.dataOffset += charsRead;
                }
                else
                {
                    this.dataOffset = -1L;
                }
            }

            if (doneReading)
            {
                this.reader.Close();
            }
        }

        // transcode more data
        if (this.chars.DataLength > 0 && this.bytes.SpanLength > 0)
        {
            var status = Utf8.FromUtf16(this.chars, this.bytes, out var charsTranscoded, out var bytesWritten, isFinalBlock: doneReading);
            if (status != OperationStatus.InvalidData)
            {
                this.chars.MarkFree(charsTranscoded);
                this.bytes.MarkUsed(bytesWritten);
            }
        }

        // move requested length
        return this.bytes.MoveTo(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var doneReading = this.reader.IsClosed;

        // read more data
        if (!doneReading)
        {
            while (this.chars.SpanLength > 0)
            {
                if (this.dataOffset < 0L)
                {
                    if (!await TryReadRowAsync(cancellationToken).ConfigureAwait(false))
                    {
                        doneReading = true;
                        break;
                    }
                    this.dataOffset = 0L;
                }

                var charsRead = (int)this.reader.GetChars(0, this.dataOffset, this.chars, this.chars.SpanOffset, this.chars.SpanLength);
                if (charsRead > 0)
                {
                    this.chars.MarkUsed(charsRead);
                    this.dataOffset += charsRead;
                }
                else
                {
                    this.dataOffset = -1L;
                }
            }

            if (doneReading)
            {
                await this.reader.CloseAsync().ConfigureAwait(false);
            }
        }

        // transcode more data
        if (this.chars.DataLength > 0 && this.bytes.SpanLength > 0)
        {
            var status = Utf8.FromUtf16(this.chars, this.bytes, out var charsTranscoded, out var bytesWritten, isFinalBlock: doneReading);
            if (status != OperationStatus.InvalidData)
            {
                this.chars.MarkFree(charsTranscoded);
                this.bytes.MarkUsed(bytesWritten);
            }
        }

        // move requested length
        return this.bytes.MoveTo(buffer.Span);
    }

    private async Task<bool> TryReadRowAsync(CancellationToken cancellationToken)
    {
        do
        {
            if (await this.reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        } while (await this.reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

        // no more rows
        return false;
    }

    private bool TryReadRow()
    {
        do
        {
            if (this.reader.Read())
            {
                return true;
            }
        } while (this.reader.NextResult());

        // no more rows
        return false;
    }

    public override void Flush() => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    [DebuggerDisplay("Used: {SpanOffset} Read: {DataOffset}")]
    sealed class DataBuffer<T> : IDisposable, IAsyncDisposable
        where T : struct
    {
        private const int DefaultSize = 16 * 1024;

        private T[] array;
        private int used;
        private int free;

        public DataBuffer(int factor = 1)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor);
            this.array = ArrayPool<T>.Shared.Rent(DefaultSize * factor);
        }

        public int SpanOffset => this.used;
        public int SpanLength => this.array.Length - this.used;

        public int DataOffset => this.free;
        public int DataLength => this.used - this.free;

        public static implicit operator T[](DataBuffer<T> buffer) => buffer.array;
        public static implicit operator Span<T>(DataBuffer<T> buffer) =>
            buffer.array.AsSpan(buffer.SpanOffset, buffer.SpanLength);
        public static implicit operator ReadOnlySpan<T>(DataBuffer<T> buffer) =>
            buffer.array.AsSpan(buffer.DataOffset, buffer.DataLength);

        public void MarkUsed(int count)
        {
            this.used += count;
        }

        public void MarkFree(int count)
        {
            this.free += count;
            if (this.free == this.used)
            {
                this.used = this.free = 0;
            }
        }

        public int MoveTo(T[] buffer, int offset, int length) =>
            MoveTo(buffer.AsSpan(offset, length));

        public int MoveTo(Span<T> buffer)
        {
            var length = Math.Min(buffer.Length, this.DataLength);
            if (length == 0) return 0;

            if (this.array.AsSpan(this.DataOffset, length).TryCopyTo(buffer))
            {
                MarkFree(length);
                return length;
            }

            return 0;
        }

        public void Dispose()
        {
            Release();
        }

        public ValueTask DisposeAsync()
        {
            Release();
            return ValueTask.CompletedTask;
        }

        private void Release()
        {
            var rented = this.array;
            if (rented.Length > 0)
            {
                ArrayPool<T>.Shared.Return(rented);
                this.array = [];
            }
        }
    }
}
