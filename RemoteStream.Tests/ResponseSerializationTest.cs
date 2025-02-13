using ProtoBuf;
using RemoteStream.Protocol;

namespace RemoteStream.Tests;

[TestFixture]
public class ResponseSerializationTest
{
    [Test]
    public void ResponseSuccessTest()
    {
        var response = Response.FromSuccess();
        var restored = SerializationPipeline(response);

        Assert.That(restored.IsSuccess, Is.True);
    }

    [Test]
    public void ResponseExceptionTest()
    {
        Response response;
        try
        {
            throw new OperationCanceledException("Test exception");
        }
        catch (OperationCanceledException e)
        {
            response = Response.FromException(e);
        }

        var restored = SerializationPipeline(response);
        Assert.Multiple(() =>
        {
            Assert.That(restored.IsSuccess, Is.False);
            Assert.That(restored.Exception, Is.Not.Null);
            if (restored.Exception == null)
            {
                Assert.Fail("Incorrect exception serialization");
                return;
            }
            
            Assert.That(restored.Exception.GetType(), Is.EqualTo(typeof(OperationCanceledException)));
            Assert.That(restored.Exception.Message, Is.EqualTo("Test exception"));
        });
    }

    [Test]
    public void ResponseInt64Test()
    {
        var response = Response<long>.FromValue(123456L);
        var restored = SerializationPipeline(response);

        Assert.That(response.Value, Is.EqualTo(restored.Value));
    }

    [Test]
    public void ResponseBufferTest()
    {
        var rand = new Random();
        var buffer = new byte[1024];
        rand.NextBytes(buffer);
        var memory = new ReadOnlyMemory<byte>(buffer);

        var response = Response<ReadOnlyMemory<byte>>.FromValue(memory);
        var restored = SerializationPipeline(response);

        Assert.That(restored.Value.ToArray(), Is.EquivalentTo(memory.ToArray()));
    }

    private static T SerializationPipeline<T>(T value)
    {
        using var serializationBuffer = new MemoryStream();
        Serializer.Serialize(serializationBuffer, value);
        serializationBuffer.Position = 0;
        var restored = Serializer.Deserialize<T>(serializationBuffer);

        return restored;
    }
}