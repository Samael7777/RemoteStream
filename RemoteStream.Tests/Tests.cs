using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using RemoteStream.Client;
using RemoteStream.Server;

namespace RemoteStream.Tests;

[TestFixture]
public class Tests
{
    private const int BufferSize = 10485760; //10Mb
    private const int ServerPort = 7777;

    private string _cert;
    private string _key;
    private byte[] _srcBuffer;
    private byte[] _dstBuffer;
    private Stream _srcStream;
    private Stream _dstStream;
    private IPEndPoint _serverEp;

    [OneTimeSetUp]
    public void SetupFixture()
    {
        _srcBuffer = new byte[BufferSize];
        _dstBuffer = new byte[BufferSize];
        _serverEp = new IPEndPoint(IPAddress.Loopback, ServerPort);

        var assembly = Assembly.GetExecutingAssembly();
        _cert = assembly.GetTextFromResource("RemoteStream.Tests.Certificate.server.crt");
        _key = assembly.GetTextFromResource("RemoteStream.Tests.Certificate.server.key");
    }

    [SetUp]
    public void Setup()
    {   
        Array.Clear(_srcBuffer);
        Array.Clear(_dstBuffer);
        RandomNumberGenerator.Fill(_srcBuffer);

        _srcStream = new MemoryStream(_srcBuffer);
        _dstStream = new MemoryStream(_dstBuffer);
    }

    [TearDown]
    public void Clean()
    {
        _srcStream.Dispose();
        _dstStream.Dispose();
    }

    [Test]
    public void TestReadWithHttpConnection()
    {
        using var streamServer = new RemoteStreamServer(_srcStream, _serverEp);
        using var streamClient = new RemoteStreamClient(_serverEp, ConnectionType.Http);

        streamClient.CopyTo(_dstStream);
        Assert.That(IsBuffersSame, Is.True);
    }

    [Test]
    public void TestReadWithHttpsConnection()
    {
        using var streamServer = new RemoteStreamServer(_srcStream, _serverEp, _cert, _key);
        using var streamClient = new RemoteStreamClient(_serverEp, ConnectionType.Https);

        streamClient.CopyTo(_dstStream);
        Assert.That(IsBuffersSame, Is.True);
    }
    
    [Test]
    public void TestWriteWithHttpConnection()
    {
        using var streamServer = new RemoteStreamServer(_dstStream, _serverEp);
        using var streamClient = new RemoteStreamClient(_serverEp, ConnectionType.Http);

        _srcStream.CopyTo(streamClient);
        Assert.That(IsBuffersSame, Is.True);
    }

    [Test]
    public void TestWriteWithHttpsConnection()
    {
        using var streamServer = new RemoteStreamServer(_dstStream, _serverEp, _cert, _key);
        using var streamClient = new RemoteStreamClient(_serverEp, ConnectionType.Https);

        _srcStream.CopyTo(streamClient);
        Assert.That(IsBuffersSame, Is.True);
    }

    private bool IsBuffersSame()
    {
        var srcMd5Task = Task.Run(() => MD5.HashData(_srcBuffer));
        var dstMd5Task = Task.Run(() => MD5.HashData(_dstBuffer));
        Task.WaitAll(srcMd5Task, dstMd5Task);
        
        var srcMd5 = srcMd5Task.Result;
        var dstMd5 = dstMd5Task.Result;

        return srcMd5.SequenceEqual(dstMd5);
    }
}