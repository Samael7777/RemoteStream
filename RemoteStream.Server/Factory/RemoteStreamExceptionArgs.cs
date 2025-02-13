namespace RemoteStream.Server.Factory;

public class RemoteStreamExceptionArgs(Exception ex) : EventArgs
{
    public Exception Exception { get; } = ex;
}