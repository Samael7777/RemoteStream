using System.Reflection;
using System.Runtime.CompilerServices;
using ProtoBuf.Meta;
using RemoteStream.Protocol.Surrogates;

namespace RemoteStream.Protocol;

file static class ProtocolInitializer
{
    private static readonly RuntimeTypeModel _runtimeTypeModel = RuntimeTypeModel.Default;

#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Configure()
    {
        ConfigureSurrogates();
    }

    private static void ConfigureSurrogates()
    {
        var currentAssembly = typeof(ProtocolInitializer).Assembly;

        var surrogates = currentAssembly.GetTypes()
            .Where(t => t.GetCustomAttributes<SurrogateAttribute>().Any())
            .SelectMany(t => t.GetCustomAttributes<SurrogateAttribute>(), (t, sa)
                => (SurrogateType: t, sa.TargetType));

        foreach (var surrogate in surrogates)
        {
            AddSurrogate(surrogate.TargetType, surrogate.SurrogateType);
        }
    }

    private static void AddSurrogate(Type type, Type surrogate)
    {
        _runtimeTypeModel.Add(type, false).SetSurrogate(surrogate);
    }

    //todo: maybe needs for generics
    
    //private static void PopulateTypes(Type? t)
    //{
    //    if (t == null || IsPopulated(t)) return;

    //    var inheritanceTreeList = new LinkedList<Type>();
    //    do
    //    {
    //        inheritanceTreeList.AddLast(t);
    //        t = t.BaseType;
    //    } while (t is not null && t != typeof(object));

    //    var inheritanceTree = inheritanceTreeList.ToArray();

    //    if (!inheritanceTree.Any(gt => gt.IsGenericType))
    //        return;

    //    var baseFieldNum = 100;
    //    for (var i = 0; i < inheritanceTree.Length - 1; i++)
    //    {
    //        var type = inheritanceTree[i];
    //        var metaType = _runtimeTypeModel.Add(type, true);
    //        metaType.AddSubType(baseFieldNum++, inheritanceTree[i + 1]);
    //    }
    //}


    //private static bool IsPopulated(Type t)
    //{
    //    foreach(var mt in _runtimeTypeModel.GetTypes())
    //    {
    //        if (mt is not MetaType metaType) continue;
    //        if (metaType.Type == t)
    //            return true;
    //    }

    //    return false;
    //}
}