using System.Net;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using RemoteStream.Protocol;

namespace RemoteStream.Client;

public class RemoteStreamClient : Stream
{
    private const int GrpcMaxReceiveMessageSize = 4 * 1024 * 1024; //4 Mb
    private const int MaxWriteBufferSize = GrpcMaxReceiveMessageSize - 5; //Max gRpc message size - header size

    private readonly Empty _emptyRequest = new();
    private readonly RemoteStreamRpcService.RemoteStreamRpcServiceClient _client;
    
    public override bool CanRead { get; }
    public override bool CanSeek { get; }
    public override bool CanWrite { get; }
    public override bool CanTimeout { get; }
    public override long Length => _client.GetLength(_emptyRequest).Value;
    public override long Position
    {
        get => _client.GetPosition(_emptyRequest).Value;
        set => _client.SetPosition(new PositionRequest { Value = value });
    }
    public IPEndPoint ServerEndPoint { get; }

    public RemoteStreamClient(IPEndPoint serverEndPoint, ConnectionType connectionType)
    {
        ServerEndPoint = serverEndPoint;
        var serverUri = BuildServerUri(connectionType);
        
        _client = BuildGrpcClient(serverUri);
        var streamInfo = _client.GetStreamInfo(_emptyRequest);

        CanTimeout = streamInfo.CanTimeout;
        CanRead = streamInfo.CanRead;
        CanSeek = streamInfo.CanSeek;
        CanWrite = streamInfo.CanWrite;
    }

    private Uri BuildServerUri(ConnectionType connectionType)
    {
        return connectionType switch
        {
            ConnectionType.Http => ServerEndPoint.ToHttpUri(),
            ConnectionType.Https => ServerEndPoint.ToHttpsUri(),
            _ => throw new ArgumentOutOfRangeException(nameof(connectionType), connectionType, null)
        };
    }

    public override void Flush()
    {
        var result = _client.Flush(_emptyRequest);
        ErrorHelper.CheckResultThrowError(result);
    }

    public override async Task FlushAsync(CancellationToken ct)
    {
        var result = await _client.FlushAsync(_emptyRequest, cancellationToken: ct)
            .ConfigureAwait(false);
        ErrorHelper.CheckResultThrowError(result);
    }

    public override int Read(byte[] buffer, int offset, int count) 
        => ReadAsync(buffer, offset, count, CancellationToken.None).Result;

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = new())
    {
        
        var result = await _client.ReadAsync(new ReadRequest { BytesToRead = buffer.Length }, cancellationToken: ct)
            .ConfigureAwait(false);
        ErrorHelper.CheckResultThrowError(result);

        result.Data.Span.CopyTo(buffer.Span);
        
        return result.Data.Length;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => await ReadAsync(buffer.AsMemory(offset, count), ct).ConfigureAwait(false);
    
    public override long Seek(long offset, SeekOrigin origin)
    {
        var result = _client.Seek(new SeekRequest { Offset = offset, OriginValue = (int)origin });
        ErrorHelper.CheckResultThrowError(result);

        return result.Value;
    }

    public override void SetLength(long value)
    {
        var result = _client.SetLength(new LengthRequest { Value = value });
        ErrorHelper.CheckResultThrowError(result);
    }

    public override void Write(byte[] buffer, int offset, int count)
        => WriteAsync(buffer, offset, count).Wait();

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => await WriteAsync(buffer.AsMemory(offset, count), ct).ConfigureAwait(false);    

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = new())
    {
        if (buffer.IsEmpty) return;
        var options = new CallOptions().WithCancellationToken(ct);
        
        var offset = 0;

        while (offset < buffer.Length)
        {
            var left = buffer.Length - offset;
            var blockSize = Math.Min(left, MaxWriteBufferSize);

            var request = new WriteRequest { Data = ByteString.CopyFrom(buffer.Span.Slice(offset, blockSize)) };
            var result = await _client.WriteAsync(request, options).ConfigureAwait(false);
            ErrorHelper.CheckResultThrowError(result);

            offset += blockSize;
        }
    }

    #region Not implemented
    /// <summary>
    /// Not implemented
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Not implemented
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Not implemented
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public override int EndRead(IAsyncResult asyncResult)
    { 
        throw new NotImplementedException();
    }

    /// <summary>
    /// Not implemented
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public override void EndWrite(IAsyncResult asyncResult)
    {
        throw new NotImplementedException();
    }
    
    #endregion
    
    private static RemoteStreamRpcService.RemoteStreamRpcServiceClient BuildGrpcClient(Uri serverUri)
    {
        var httpClientHandler = new HttpClientHandler
        {
            // заглушка для самоподписанного сертификата
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
        };

        var httpClient = new HttpClient(httpClientHandler);
        var options = new GrpcChannelOptions
        {
            HttpClient = httpClient,
            MaxReceiveMessageSize = GrpcMaxReceiveMessageSize
        };

        var channel = GrpcChannel.ForAddress(serverUri, options);
        
        return new RemoteStreamRpcService.RemoteStreamRpcServiceClient(channel);
    }
}