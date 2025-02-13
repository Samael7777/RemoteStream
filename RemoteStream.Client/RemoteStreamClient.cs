using System.Buffers;
using System.Runtime.InteropServices;
using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using RemoteStream.Protocol;
using RemoteStream.Protocol.Interfaces;

namespace RemoteStream.Client;

public class RemoteStreamClient : Stream
{
    protected const int MessageHeaderSize = 10; //todo: make auto measuring

    private readonly IRemoteStream _client;

    protected readonly GrpcChannel _channel;
    protected readonly SemaphoreSlim _semaphore = new(1, 1);
    protected readonly GrpcChannelOptions _channelOptions;
    protected readonly int _maxWriteBufferSize;
    
    public override bool CanRead { get; }
    public override bool CanSeek { get; }
    public override bool CanWrite { get; }
    public override bool CanTimeout { get; }

    public override long Length =>
        _client.GetLengthAsync().WaitAsync(CancellationToken.None).Result.Value;

    public override long Position
    {
        get
        {   
            _semaphore.Wait();
            try
            {
                return _client.GetPositionAsync().WaitAsync(CancellationToken.None).Result.Value;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        set
        {
            _semaphore.Wait();
            try
            {
                _client.SetPositionAsync(ValueRequest<long>.FromValue(value)).WaitAsync(CancellationToken.None)
                    .Result.ThrowExceptionWhenError();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public RemoteStreamClient(Uri connection, GrpcChannelOptions options)
    {
        _channelOptions = options;
        _maxWriteBufferSize = (options.MaxReceiveMessageSize ?? 4 * 1024 * 1024) - MessageHeaderSize;

        _channel = GrpcChannel.ForAddress(connection, options);
        
        _client = _channel.CreateGrpcService<IRemoteStream>();

        var streamInfo = _client.GetStreamInfo().Value
                         ?? throw new ApplicationException("Get stream info error");

        CanRead = streamInfo.CanRead;
        CanSeek = streamInfo.CanSeek;
        CanTimeout = streamInfo.CanTimeout;
        CanWrite = streamInfo.CanWrite;
    }
    
    public override void Flush() => FlushAsync(CancellationToken.None).Wait();

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var context = BuildCallContext(cancellationToken);
            (await _client.FlushAsync(context).ConfigureAwait(false)).ThrowExceptionWhenError();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer, offset, count, CancellationToken.None)
            .WaitAsync(CancellationToken.None).Result;

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var context = BuildCallContext(cancellationToken);
            var response = await _client.ReadAsync(ValueRequest<int>.FromValue(count), context)
                .ConfigureAwait(false);

            var resultBuffer = response.Value;
            resultBuffer.CopyTo(buffer.AsMemory(offset, count));

            return resultBuffer.Length;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
    {
        //Based on original Stream
        if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
        {
#pragma warning disable CA1835
            return await ReadAsync(array.Array!, array.Offset, array.Count, cancellationToken)
#pragma warning restore CA1835
                .ConfigureAwait(false);
        }

        var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
#pragma warning disable CA1835
            var result = await ReadAsync(sharedBuffer, 0, buffer.Length, cancellationToken)
#pragma warning restore CA1835
                .ConfigureAwait(false);
            new ReadOnlySpan<byte>(sharedBuffer, 0, result).CopyTo(buffer.Span);
            
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        _semaphore.Wait();
        try
        {
            return _client.SeekAsync(ValueRequest<long>.FromValue(offset), ValueRequest<SeekOrigin>.FromValue(origin))
                .WaitAsync(CancellationToken.None).Result.Value;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public override void SetLength(long value) =>
        _client.SetLengthAsync(ValueRequest<long>.FromValue(value)).WaitAsync(CancellationToken.None).Result.ThrowExceptionWhenError();


    public override void Write(byte[] buffer, int offset, int count) =>
        WriteAsync(buffer, offset, count, CancellationToken.None).WaitAsync(CancellationToken.None);

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var context = BuildCallContext(cancellationToken);

            var memoryBuffer = new Memory<byte>(buffer, offset, count);
            var start = 0;

            while (start < buffer.Length)
            {
                var left = buffer.Length - start;
                var blockSize = Math.Min(left, _maxWriteBufferSize);

                var request = ValueRequest<ReadOnlyMemory<byte>>.FromValue(memoryBuffer.Slice(start, blockSize));
                var response = await _client.WriteAsync(request, context).ConfigureAwait(false);
                response.ThrowExceptionWhenError();

                start += blockSize;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        //Based on original Stream
        if (MemoryMarshal.TryGetArray(buffer, out var array) && array.Array != null)
        {
#pragma warning disable CA1835
            await WriteAsync(array.Array, array.Offset, array.Count, cancellationToken)
#pragma warning restore CA1835
                .ConfigureAwait(false);
        }
        else
        {
            var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            buffer.Span.CopyTo(sharedBuffer);
            
            try
            {
#pragma warning disable CA1835
                await WriteAsync(sharedBuffer, 0, buffer.Length, cancellationToken)
#pragma warning restore CA1835
                    .ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }
    }

    private static CallContext BuildCallContext(CancellationToken token)
    {
        var options = new CallOptions(cancellationToken: token);
        return new CallContext(options);
    }
    
    #region Not supported
    /// <summary>
    /// Not supported
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Obsolete("Not supported, use async method instead")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Not supported
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Obsolete("Not supported, use async method instead")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Not supported
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Obsolete("Not supported, use async method instead")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override int EndRead(IAsyncResult asyncResult)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Not supported
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Obsolete("Not supported, use async method instead")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override void EndWrite(IAsyncResult asyncResult)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    {
        throw new InvalidOperationException();
    }

    #endregion

    #region Dispose

    private bool _disposed;

    ~RemoteStreamClient()
    {
        Dispose(false);
    }
    
    protected override void Dispose(bool disposing)
    {
        DisposeAsync(disposing).AsTask().Wait(CancellationToken.None);
    }
    
    public override async ValueTask DisposeAsync()
    {
        await DisposeAsync(true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            //dispose managed state (managed objects)
            await _client.CloseAsync().ConfigureAwait(false);
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }
        //free unmanaged resources (unmanaged objects) and override finalizer
        //set large fields to null

        _disposed = true;
    }

    #endregion
}