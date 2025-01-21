using System.ServiceModel;

namespace RemoteStream.Protocol;

[ServiceContract]
public interface IStream
{
    [OperationContract] 
    Response<StreamInfo> GetStreamInfo();

    [OperationContract] 
    Response<long> GetLength();

    [OperationContract]
    Response<Empty> SetLength(long length);

    [OperationContract]
    Response<long> GetPosition();

    [OperationContract]
    Response<Empty> SetPosition(long position);
}

//service RemoteStreamRpcService {
//rpc GetStreamInfo(google.protobuf.Empty) returns (StreamInfo);
//rpc GetLength(google.protobuf.Empty) returns (LengthResponse);
//rpc SetLength(LengthRequest) returns (StatusResponse);
//rpc GetPosition(google.protobuf.Empty) returns (PositionResponse);
//rpc SetPosition(PositionRequest) returns (StatusResponse);

//rpc Seek(SeekRequest) returns (PositionResponse);
//rpc Write(WriteRequest) returns (StatusResponse);
//rpc Read(ReadRequest) returns (ReadResponse);
//rpc Flush(google.protobuf.Empty) returns (StatusResponse);
//rpc Close(google.protobuf.Empty) returns (StatusResponse);
//}