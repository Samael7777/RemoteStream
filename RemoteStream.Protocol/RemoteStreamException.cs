// ReSharper disable ConvertToPrimaryConstructor
namespace RemoteStream.Protocol;

public class RemoteStreamException : Exception
{
    public RemoteStreamException(ExceptionObject eo) 
        : this(eo.Message, eo)
    { }

    public RemoteStreamException(string? message, ExceptionObject? eo) 
        : base(message)
    {
        ExceptionInfo = eo;
    }

    public ExceptionObject? ExceptionInfo { get; }
}