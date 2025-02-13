using ProtoBuf.Grpc;
using RemoteStream.Protocol;
using RemoteStream.Protocol.Interfaces;
using RemoteStream.Server.Factory;

// ReSharper disable ConvertToPrimaryConstructor

namespace RemoteStream.Server.Implementations;

[StreamImplementation(typeof(Stream), typeof(IRemoteStream))]
public class RemoteStreamImpl : IRemoteStream, IDisposable, IAsyncDisposable
{
    private readonly bool _disposeStream;
    
    protected readonly Stream _baseStream;

    public event EventHandler? StreamClosed;
    public event EventHandler<RemoteStreamExceptionArgs>? ExceptionThrew;

    public RemoteStreamImpl(Stream stream, bool disposeStream)
    {
        _baseStream = stream;
        _disposeStream = disposeStream;
    }

    public Response<StreamInfo> GetStreamInfo()
    {
        var info = new StreamInfo
        {
            CanRead = _baseStream.CanRead,
            CanSeek = _baseStream.CanSeek,
            CanTimeout = _baseStream.CanTimeout,
            CanWrite = _baseStream.CanWrite,
        };

        return Response<StreamInfo>.FromValue(info);
    }

    public Task<Response<long>> GetLengthAsync()
    {
        try
        {
            var response = Response<long>.FromValue(_baseStream.Length);
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            var response = Response<long>.FromException(ex);
            OnExceptionThrew(ex);

            return Task.FromResult(response);
        }
    }

    public Task<Response> SetLengthAsync(ValueRequest<long> length)
    {
        try
        {
            _baseStream.SetLength(length.Value);

            var response = Response.FromSuccess();
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            OnExceptionThrew(ex);

            return Response.TaskFromException(ex);
        }
    }

    public Task<Response<long>> GetPositionAsync()
    {
        try
        {
            var pos = _baseStream.Position;
            return Response<long>.TaskFromValue(pos);
        }
        catch (Exception ex)
        {
            OnExceptionThrew(ex);

            return Response<long>.TaskFromException(ex);
        }
    }

    public Task<Response> SetPositionAsync(ValueRequest<long> position)
    {
        try
        {
            _baseStream.Position = position.Value;

            return Response.TaskFromSuccess();
        }
        catch (Exception ex)
        {
            OnExceptionThrew(ex);

            return Response.TaskFromException(ex);
        }
    }

    public Task<Response<long>> SeekAsync(ValueRequest<long> offset, ValueRequest<SeekOrigin> origin)
    {
        try
        {
            var pos = _baseStream.Seek(offset.Value, origin.Value);

            return Response<long>.TaskFromValue(pos);
        }
        catch (Exception ex)
        {
            OnExceptionThrew(ex);

            return Response<long>.TaskFromException(ex);
        }
    }

    public async Task<Response> WriteAsync(ValueRequest<ReadOnlyMemory<byte>> data, CallContext context = default)
    {
        try
        {
            await _baseStream.WriteAsync(data.Value, context.CancellationToken)
                .ConfigureAwait(false);

            return Response.FromSuccess();
        }
        catch (Exception ex)
        {
            OnExceptionThrew(ex);

            return Response.FromException(ex);
        }
    }

    public async Task<Response<ReadOnlyMemory<byte>>> ReadAsync(ValueRequest<int> len, CallContext context = default)
    {
        try
        {
            var buffer = new byte[len.Value];
            var read = await _baseStream.ReadAsync(buffer, context.CancellationToken)
                .ConfigureAwait(false);

            var data = new ReadOnlyMemory<byte>(buffer[..read].ToArray());

            return Response<ReadOnlyMemory<byte>>.FromValue(data);
        }
        catch (Exception ex)
        {
            OnExceptionThrew(ex);

            return Response<ReadOnlyMemory<byte>>.FromException(ex);
        }
    }

    public async Task<Response> FlushAsync(CallContext context = default)
    {
        try
        {
            await _baseStream.FlushAsync(context.CancellationToken).ConfigureAwait(false);

            return Response.FromSuccess();
        }
        catch (Exception ex)
        {
            OnExceptionThrew(ex);

            return Response.FromException(ex);
        }
    }

    public async Task<Response> CloseAsync()
    {
        try
        {
            await DisposeAsync().ConfigureAwait(false);
            OnStreamClosed();

            return Response.FromSuccess();
        }
        catch (Exception ex)
        {
            OnExceptionThrew(ex);

            return Response.FromException(ex);
        }
    }

    protected void OnExceptionThrew(Exception ex)
    {
        ExceptionThrew?.Invoke(this, new RemoteStreamExceptionArgs(ex));
    }

    protected void OnStreamClosed()
    {
        StreamClosed?.Invoke(this, EventArgs.Empty);
    }

    #region Dispose

    private bool _disposed;

    ~RemoteStreamImpl()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if(_disposeStream) 
                _baseStream.Dispose();

            OnStreamClosed();
        }

        _disposed = true;
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        if(_disposeStream) 
            await _baseStream.DisposeAsync().ConfigureAwait(false);
        
        await Task.Run(OnStreamClosed).ConfigureAwait(false);
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    #endregion
}