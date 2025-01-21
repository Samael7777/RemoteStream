using ProtoBuf;

namespace RemoteStream.Protocol;

[ProtoContract]
public class StreamInfo
{
    [ProtoMember(1)]
    public bool CanRead { get; set; }
    [ProtoMember(2)]
    public bool CanWrite { get; set; }
    [ProtoMember(3)]
    public bool CanSeek { get; set; }
    [ProtoMember(4)]
    public bool CanTimeout { get; set; }
}