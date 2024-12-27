namespace RemoteStream.Protocol;

public static class ExceptionEx
{
    public static string ToExceptionObjectJson(this Exception ex)
    {
        return new ExceptionObject(ex).ToJson();
    }
}