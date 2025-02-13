using Grpc.Core;
using Grpc.Net.Client;
using RemoteStream.Client;
using RemoteStream.Server;
using System.Security.Cryptography;

namespace RemoteStream.Tests;

[TestFixture]
public class RemoteStreamGeneralTests
{
    private const int BufferSize = 100 * 1024 * 1024; //100 Mb
    private const string Host = "localhost";
    private const int Port = 7777;
    
    private byte[] _srcBuffer = new byte[BufferSize];
    private byte[] _dstBuffer = new byte[BufferSize];
    private MemoryStream _srcStream;
    private MemoryStream _dstStream;
    private Uri _connection;
    private GrpcChannelOptions _channelOptions;

    [OneTimeSetUp]
    public void Init()
    {
        var ub = new UriBuilder("http")
        {
            Host = Host,
            Port = Port,
        };
        _connection = ub.Uri;
        
        _channelOptions = new GrpcChannelOptions();
        _channelOptions.WithAcceptAnyCertificateHttpsClient();
        _channelOptions.Credentials = ChannelCredentials.Insecure;

        FillBufferWithRandom(_srcBuffer);
        
        _srcStream = new MemoryStream(_srcBuffer);
        _dstStream = new MemoryStream(_dstBuffer);
    }

    [OneTimeTearDown]
    public void Finish()
    {
        _srcStream.Dispose();
        _dstStream.Dispose();
        _srcBuffer = [];
        _dstBuffer = [];

        GC.Collect();
    }

    [TearDown]
    public void TearDown()
    {
        Array.Clear(_dstBuffer);

        _srcStream.Position = 0;
        _dstStream.Position = 0;
    }

    [Test]
    public async Task ReadDataTest()
    {
        await using var server = new RemoteStreamServer<Stream>(_srcStream, Host, Port, false);
        await using var client = new RemoteStreamClient(_connection, _channelOptions);

        await client.CopyToAsync(_dstStream);

        Assert.That(IsBuffersEquivalent(), Is.EqualTo(true));
    }

    [Test]
    public async Task WriteDataTest()
    {
        await using var server = new RemoteStreamServer<Stream>(_dstStream, Host, Port, false);
        await using var client = new RemoteStreamClient(_connection, _channelOptions);

        await _srcStream.CopyToAsync(client);

        Assert.That(IsBuffersEquivalent(), Is.EqualTo(true));
    }

    private static void FillBufferWithRandom(byte[] buffer)
    {
        var rnd = new Random();
        rnd.NextBytes(buffer);
    }

    private bool IsBuffersEquivalent()
    {
        var srcMd5Task = Task.Run(() => MD5.HashData(_srcBuffer));
        var dstMd5Task = Task.Run(() => MD5.HashData(_dstBuffer));
        Task.WaitAll(srcMd5Task, dstMd5Task);
        
        var srcMd5 = srcMd5Task.Result;
        var dstMd5 = dstMd5Task.Result;

        return srcMd5.SequenceEqual(dstMd5);
    }
}