using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using RemoteStream.Protocol;


namespace RemoteStream.Server;

internal class RemoteStreamServerRpcImpl : RemoteStreamRpcService.RemoteStreamRpcServiceBase, 
    IDisposable
{
    
    private readonly Stream _baseStream;
   
    public event EventHandler? StreamClosed;
    public event EventHandler<Exception>? ExceptionThrew;
    
    // ReSharper disable once ConvertToPrimaryConstructor
    public RemoteStreamServerRpcImpl(Stream baseStream)
    {
        _baseStream = baseStream;
    }
    
    public override Task<StatusResponse> Close(Empty request, ServerCallContext context)
    {
        try
        {
            _baseStream.Close();
            StreamClosed?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(new StatusResponse { IsOk = true });
        }
        catch (Exception e)
        {
            RaiseExceptionEvent(e);
            return Task.FromResult(new StatusResponse { ErrorJson = e.ToExceptionObjectJson()});
        }
    }
    
    public override async Task<StatusResponse> Flush(Empty request, ServerCallContext context)
    {
        try
        {
            await _baseStream.FlushAsync().ConfigureAwait(false);
            return new StatusResponse { IsOk = true };
        }
        catch (Exception e)
        {
            RaiseExceptionEvent(e);
            return new StatusResponse { ErrorJson = e.ToExceptionObjectJson() };
        }
    }

    public override Task<LengthResponse> GetLength(Empty request, ServerCallContext context)
    {
        try
        {
            var len = _baseStream.Length;
            return Task.FromResult(new LengthResponse { Value = len });
        }
        catch (Exception e)
        {
            RaiseExceptionEvent(e);
            return Task.FromResult(new LengthResponse { ErrorJson = e.ToExceptionObjectJson() });
        }
    }

    public override Task<PositionResponse> GetPosition(Empty request, ServerCallContext context)
    {
        try
        {
            var pos = _baseStream.Position;
            return Task.FromResult(new PositionResponse { Value = pos });
        }
        catch (Exception e)
        {
            RaiseExceptionEvent(e);
            return Task.FromResult(new PositionResponse { ErrorJson = e.ToExceptionObjectJson() });
        }
    }

    public override Task<StreamInfo> GetStreamInfo(Empty request, ServerCallContext context)
    {
        var streamInfo = new StreamInfo
        {
            CanRead = _baseStream.CanRead,
            CanSeek = _baseStream.CanSeek,
            CanTimeout = _baseStream.CanTimeout,
            CanWrite = _baseStream.CanWrite,
        };

        return Task.FromResult(streamInfo);
    }

    public override async Task<ReadResponse> Read(ReadRequest request, ServerCallContext context)
    {
        var bytesToRead = request.BytesToRead;
        try
        {
            var buffer = new Memory<byte>(new byte[bytesToRead]);
            var read = await _baseStream.ReadAsync(buffer).ConfigureAwait(false);

            return new ReadResponse { Data = ByteString.CopyFrom(buffer.Span[..read]) };
        }
        catch (Exception e)
        {
            RaiseExceptionEvent(e);
            return new ReadResponse { ErrorJson = e.ToExceptionObjectJson() };
        }
    }

    public override Task<PositionResponse> Seek(SeekRequest request, ServerCallContext context)
    {
        try
        {
            var pos = _baseStream.Seek(request.Offset, (SeekOrigin)request.OriginValue);
            return Task.FromResult(new PositionResponse { Value = pos });
        }
        catch (Exception e)
        {
            RaiseExceptionEvent(e);
            return Task.FromResult(new PositionResponse { ErrorJson = e.ToExceptionObjectJson() });
        }
    }

    public override Task<StatusResponse> SetLength(LengthRequest request, ServerCallContext context)
    {
        try
        {
            _baseStream.SetLength(request.Value);
            return Task.FromResult(new StatusResponse { IsOk = true});
        }
        catch (Exception e)
        {
            RaiseExceptionEvent(e);
            return Task.FromResult(new StatusResponse { ErrorJson = e.ToExceptionObjectJson() });
        }
    }

    public override Task<StatusResponse> SetPosition(PositionRequest request, ServerCallContext context)
    {
        try
        {
            _baseStream.Position = request.Value;
            return Task.FromResult(new StatusResponse { IsOk = true});
        }
        catch (Exception e)
        {
            RaiseExceptionEvent(e);
            return Task.FromResult(new StatusResponse { ErrorJson = e.ToExceptionObjectJson() });
        }
    }

    public override async Task<StatusResponse> Write(WriteRequest request, ServerCallContext context)
    {
        try
        {
            var data = request.Data;
            if (data != null)
            {
                await _baseStream.WriteAsync(data.Memory).ConfigureAwait(false);
            }

            return new StatusResponse { IsOk = true };
        }
        catch (Exception e)
        {
            RaiseExceptionEvent(e);
            return new StatusResponse { ErrorJson = e.ToExceptionObjectJson() };
        }
    }
    
    private void RaiseExceptionEvent(Exception ex)
    {
        ExceptionThrew?.Invoke(this, ex);
    }

    #region Dispose

    private bool _disposed;

    ~RemoteStreamServerRpcImpl()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            //dispose managed state (managed objects)
            _baseStream.Dispose();
            StreamClosed?.Invoke(this, EventArgs.Empty);
        }
        //free unmanaged resources (unmanaged objects) and override finalizer
        //set large fields to null
        _disposed = true;
    }

    #endregion
}