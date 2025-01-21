using System.ServiceModel;

namespace RemoteStream.Protocol;

[ServiceContract]
public interface IStream
{
    [OperationContract] 
    Task<Response<StreamInfo>> GetStreamInfo();

    [OperationContract] 
    Task<Response<long>> GetLength();

    [OperationContract]
    Task<Response<Empty>> SetLength(long length);

    [OperationContract]
    Task<Response<long>> GetPosition();

    [OperationContract]
    Task<Response<Empty>> SetPosition(long position);

    [OperationContract]
    Task<Response<long>> Seek(long offset, SeekOrigin origin);

    [OperationContract]
    Task<Response<Empty>> Write(ReadOnlySpan<byte> data);

    [OperationContract]
    Task<Response<byte[]>> Read(int len);

    [OperationContract]
    Task<Response<Empty>> Flush();
    
    [OperationContract]
    Task<Response<Empty>> Close();
}