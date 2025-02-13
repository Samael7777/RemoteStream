using System.Reflection;
using RemoteStream.Server.Implementations;

namespace RemoteStream.Server.Factory;


internal static class RemoteStreamImplFactory
{
    private static readonly Dictionary<Type, ImplementationInfo> _implCache = [];

    static RemoteStreamImplFactory()
    {
        BuildImplementationsCache();
    }

    //todo
    public static RemoteStreamImpl GetRemoteStreamImpl<T>(T baseStream, bool disposeStream)
    where T : Stream
    {
        if (!_implCache.ContainsKey(typeof(T)))
            throw new ArgumentException($"Service {typeof(T)} is not implemented.");

        var implType = _implCache[typeof(T)].Implementation;

        var ctorParams = new object[] { baseStream, disposeStream };

        if (Activator.CreateInstance(implType, ctorParams) is not RemoteStreamImpl implInstance)
            throw new ApplicationException($"Can't create implementation instance for {typeof(T)}");

        return implInstance;
    }

    private static void BuildImplementationsCache()
    {
        var thisAssembly = typeof(RemoteStreamImplFactory).Assembly;
        var implementations = thisAssembly.GetTypes()
            .Where(t => t.GetCustomAttribute<StreamImplementationAttribute>() != null)
            .Select(t => (Implementation: t, Attribute: t.GetCustomAttribute<StreamImplementationAttribute>()!));

        foreach (var item in implementations)
        {
            _implCache.Add(item.Attribute.Target, new ImplementationInfo(item.Implementation, item.Attribute.GrpcInterface));
        }
    }
}