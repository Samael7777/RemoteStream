using Grpc.Net.Client;

namespace RemoteStream.Client;

public static class GrpcChannelOptionsEx
{
    public static GrpcChannelOptions WithAcceptAnyCertificateHttpsClient(this GrpcChannelOptions options)
    {
        var httpHandler = new HttpClientHandler
        {
            // заглушка для само-подписанного сертификата
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        var httpClient = new HttpClient(httpHandler);

        options.HttpClient = httpClient;
        options.DisposeHttpClient = true;
        
        return options;
    }
}