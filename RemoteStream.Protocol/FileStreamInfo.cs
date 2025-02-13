using ProtoBuf;

namespace RemoteStream.Protocol;

[ProtoContract]
public class FileStreamInfo
{
    [ProtoMember(1)]
    public bool IsAsync { get; set; }

    [ProtoMember(2)] 
    public string Name { get; set; } = "";
}