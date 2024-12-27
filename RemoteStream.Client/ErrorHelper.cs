using RemoteStream.Protocol;

namespace RemoteStream.Client;

internal static class ErrorHelper
{
    public static void CheckResultThrowError(StatusResponse response)
    {
        if (response.HasErrorJson)
        {
            ThrowRemoteStreamException(response.ErrorJson);
        }
    }

    public static void CheckResultThrowError(LengthResponse response)
    {
        if (response.HasErrorJson)
        {
            ThrowRemoteStreamException(response.ErrorJson);
        }
    }

    public static void CheckResultThrowError(PositionResponse response)
    {
        if (response.HasErrorJson)
        {
            ThrowRemoteStreamException(response.ErrorJson);
        }
    }

    public static void CheckResultThrowError(ReadResponse response)
    {
        if (response.HasErrorJson)
        {
            ThrowRemoteStreamException(response.ErrorJson);
        }
    }

    private static void ThrowRemoteStreamException(string exceptionObjectJson)
    {
        var eo = ExceptionObject.FromJson(exceptionObjectJson);
        throw new RemoteStreamException(eo);
    }
}