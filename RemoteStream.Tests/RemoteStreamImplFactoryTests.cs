using RemoteStream.Protocol.Interfaces;
using RemoteStream.Server.Factory;

namespace RemoteStream.Tests;

[TestFixture]
public class RemoteStreamImplFactoryTests
{
    [Test]
    public void GetRemoteStreamImplTest_IRemoteStream_Correct()
    {
        using var baseStream = new MemoryStream();
        using var impl = RemoteStreamImplFactory.GetRemoteStreamImpl<Stream>(baseStream, true);
        Assert.Multiple(() =>
        {
            Assert.That(impl, Is.Not.Null);
            Assert.That(impl.GetType().GetInterface(nameof(IRemoteStream)),Is.Not.Null);
        });
    }

    [Test]
    public void GetRemoteStreamImplTest_Exception()
    {
        using var baseStream = new MemoryStream();
        Assert.Throws<ArgumentException>(() =>
        {
            _ = RemoteStreamImplFactory.GetRemoteStreamImpl(baseStream, true);
        });
    }
}