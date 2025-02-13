using System.ServiceModel;
using ProtoBuf.Grpc;

namespace RemoteStream.Protocol.Interfaces;

[ServiceContract]
public interface IRemoteStream
{
    [OperationContract]
    Response<StreamInfo> GetStreamInfo();

    [OperationContract]
    Task<Response<long>> GetLengthAsync();

    [OperationContract]
    Task<Response> SetLengthAsync(ValueRequest<long> length);

    [OperationContract]
    Task<Response<long>> GetPositionAsync();

    [OperationContract]
    Task<Response> SetPositionAsync(ValueRequest<long> position);

    [OperationContract]
    Task<Response<long>> SeekAsync(ValueRequest<long> offset, ValueRequest<SeekOrigin> origin);

    [OperationContract]
    Task<Response> WriteAsync(ValueRequest<ReadOnlyMemory<byte>> data, CallContext context = default);

    [OperationContract]
    Task<Response<ReadOnlyMemory<byte>>> ReadAsync(ValueRequest<int> len, CallContext context = default);

    [OperationContract]
    Task<Response> FlushAsync(CallContext context = default);

    [OperationContract]
    Task<Response> CloseAsync();
}