using System.ServiceModel;
using ProtoBuf.Grpc;

namespace RemoteStream.Protocol.Interfaces;

[ServiceContract]
public interface IRemoteFileStream : IRemoteStream
{
    [OperationContract]
    Task<Response<FileStreamInfo>> GetFileStreamInfoAsync();

    [OperationContract]
    Task<Response> FlushAsync(ValueRequest<bool> flushToDisk, CallContext context = default);
    
    [OperationContract]
    Task<Response> Lock(ValueRequest<long> position, ValueRequest<long> length);

    [OperationContract]
    Task<Response> Unlock(ValueRequest<long> position, ValueRequest<long> length);
}